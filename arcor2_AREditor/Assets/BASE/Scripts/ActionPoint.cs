using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Base {
    public abstract class ActionPoint : Clickable {

        // Key string is set to IO.Swagger.Model.ActionPoint Data.Uuid
        public Dictionary<string, Action> Actions = new Dictionary<string, Action>();
        public GameObject ActionsSpawn;

        public ActionObject ActionObject;
        protected Vector3 offset;
        [System.NonSerialized]
        public int PuckCounter = 0;
        // TODO: rename (Connection to action object)
        public Connection ConnectionToIO;

        [System.NonSerialized]
        public IO.Swagger.Model.ProjectActionPoint Data = new IO.Swagger.Model.ProjectActionPoint(id: "", robotJoints: new List<IO.Swagger.Model.ProjectRobotJoints>(), orientations: new List<IO.Swagger.Model.NamedOrientation>(), position: new IO.Swagger.Model.Position(), actions: new List<IO.Swagger.Model.Action>(), userId: "");
        protected ActionPointMenu actionPointMenu;

        
        public bool Locked {
            get {
                return GameManager.Instance.LoadBool("project/" + GameManager.Instance.CurrentProject.Id + "/AP/" + Data.Id + "/locked", false);
            }

            set {
                Debug.Assert(GameManager.Instance.CurrentProject != null);
                GameManager.Instance.SaveBool("project/" + GameManager.Instance.CurrentProject.Id + "/AP/" + Data.Id + "/locked", value);
            }
        }

        protected virtual void Start() {
            actionPointMenu = MenuManager.Instance.ActionPointMenu.gameObject.GetComponent<ActionPointMenu>();
            
        }

        protected virtual void Update() {
            if (gameObject.transform.hasChanged) {
                SetScenePosition(transform.localPosition);
                //SetSceneOrientation(transform.localRotation);
                transform.hasChanged = false;
            }
        }

        public void ActionPointUpdate(IO.Swagger.Model.ProjectActionPoint apData = null) {
            if (apData != null)
                Data = apData;
            // update position and rotation based on received data from swagger
            transform.localPosition = GetScenePosition();
            //TODO: ActionPoint has multiple rotations of end-effectors, for visualization, render end-effectors individually
            //transform.localRotation = GetSceneOrientation();
        }
        
        public virtual void UpdateId(string newId, bool updateProject = true) {
            Data.Id = newId;

            if (updateProject)
                GameManager.Instance.UpdateProject();
        }

        public void InitAP(ActionObject actionObject, IO.Swagger.Model.ProjectActionPoint apData = null) {
            SetActionObject(actionObject);
            if (apData != null) {
                Data = apData;
            } else {
                Data.Id = Guid.NewGuid().ToString();
            }
               
            if (Data.Orientations.Count == 0)
                Data.Orientations.Add(new IO.Swagger.Model.NamedOrientation(id: "default", orientation: new IO.Swagger.Model.Orientation()));

        }

        public void SetActionObject(ActionObject actionObject) {
            ActionObject = actionObject;
            Data.Id = actionObject.Data.Id +  "-AP" + ActionObject.CounterAP++.ToString();
        }

        public abstract void UpdatePositionsOfPucks();

        public Dictionary<string, IO.Swagger.Model.Pose> GetPoses() {
            Dictionary<string, IO.Swagger.Model.Pose> poses = new Dictionary<string, IO.Swagger.Model.Pose>();
            foreach (IO.Swagger.Model.NamedOrientation orientation in Data.Orientations) {
                poses.Add(orientation.Id, new IO.Swagger.Model.Pose(orientation.Orientation, Data.Position));
            }
            return poses;
        }

        public IO.Swagger.Model.Pose GetDefaultPose() {
            foreach (IO.Swagger.Model.NamedOrientation orientation in Data.Orientations) {
                if (orientation.Id == "default")
                    return new IO.Swagger.Model.Pose(position: Data.Position, orientation: orientation.Orientation);
            }
            throw new ItemNotFoundException();            
        }

        public IO.Swagger.Model.ProjectRobotJoints GetFirstJoints(string robot_id = null, bool valid_only = false) {
            foreach (IO.Swagger.Model.ProjectRobotJoints robotJoint in Data.RobotJoints) {
                if ((robot_id != null && robot_id != robotJoint.RobotId) ||
                        (valid_only && !robotJoint.IsValid))
                    continue;
                return robotJoint;
            }
            return null;    
        }

        public Dictionary<string, IO.Swagger.Model.ProjectRobotJoints> GetJoints(bool uniqueOnly = false, string robot_id = null, bool valid_only = false) {
            Dictionary<string, IO.Swagger.Model.ProjectRobotJoints> joints = new Dictionary<string, IO.Swagger.Model.ProjectRobotJoints>();
            Dictionary<string, IO.Swagger.Model.Pose> poses = new Dictionary<string, IO.Swagger.Model.Pose>();
            if (uniqueOnly) {
                poses = GetPoses();
            }
            foreach (IO.Swagger.Model.ProjectRobotJoints robotJoint in Data.RobotJoints) {
                if ((uniqueOnly && poses.ContainsKey(robotJoint.Id)) ||
                    (robot_id != null && robot_id != robotJoint.RobotId) ||
                    (valid_only && !robotJoint.IsValid)) {
                    continue;
                }                
                joints.Add(robotJoint.Id, robotJoint);
            }
            return joints;
        }
        


        public void DeleteAP(bool updateProject = true) {
            // Remove all actions of this action point
            RemoveActions(false);

            // TODO: remove connections to action objects
            if (ConnectionToIO != null && ConnectionToIO.gameObject != null) {
                Destroy(ConnectionToIO.gameObject);
            }

            // Remove this ActionPoint reference from parent ActionObject list
            ActionObject.ActionPoints.Remove(this.Data.Id);

            Destroy(gameObject);

            if (updateProject)
                GameManager.Instance.UpdateProject();
        }

        public virtual bool ProjectInteractable() {
            return GameManager.Instance.GetGameState() == GameManager.GameStateEnum.ProjectEditor;
        }

        public abstract Vector3 GetScenePosition();
        public abstract void SetScenePosition(Vector3 position);
        public abstract Quaternion GetSceneOrientation();
        public abstract void SetSceneOrientation(Quaternion orientation);

        public void RemoveActions(bool updateProject) {
            // Remove all actions of this action point
            foreach (string actionUUID in Actions.Keys.ToList<string>()) {
                RemoveAction(actionUUID, updateProject);
            }
            Actions.Clear();
        }

        public void RemoveAction(string uuid, bool updateProject) {
            Actions[uuid].DeleteAction(updateProject);
        }

        public void ShowMenu() {
            actionPointMenu.CurrentActionPoint = this;
            actionPointMenu.UpdateMenu();
            MenuManager.Instance.ShowMenu(MenuManager.Instance.ActionPointMenu);            
        }

        public virtual void ActivateForGizmo(string layer) {
            gameObject.layer = LayerMask.NameToLayer(layer);
        }
    }

}
