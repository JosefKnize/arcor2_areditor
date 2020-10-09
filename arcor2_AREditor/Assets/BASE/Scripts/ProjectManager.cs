using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System;

using IO.Swagger.Model;
using System.Linq;

namespace Base {
    /// <summary>
    /// Takes care of currently opened project. Provides methods for manipuation with project.
    /// </summary>
    public class ProjectManager : Base.Singleton<ProjectManager> {
        /// <summary>
        /// Opened project metadata
        /// </summary>
        public IO.Swagger.Model.Project ProjectMeta = null;
        /// <summary>
        /// All action points in scene
        /// </summary>
        public Dictionary<string, ActionPoint> ActionPoints = new Dictionary<string, ActionPoint>();
        /// <summary>
        /// All logic items (i.e. connections of actions) in project
        /// </summary>
        public Dictionary<string, LogicItem> LogicItems = new Dictionary<string, LogicItem>();
        /// <summary>
        /// Spawn point for global action points
        /// </summary>
        public GameObject ActionPointsOrigin;
        /// <summary>
        /// Prefab for project elements
        /// </summary>
        public GameObject ConnectionPrefab, ActionPointPrefab, PuckPrefab, StartPrefab, EndPrefab;
        /// <summary>
        /// Action representing start of program
        /// </summary>
        public StartAction StartAction;
        /// <summary>
        /// Action representing end of program
        /// </summary>
        public EndAction EndAction;
        /// <summary>
        /// ??? Dan?
        /// </summary>
        private bool projectActive = true;
        /// <summary>
        /// Indicates if action point orientations should be visible for given project
        /// </summary>
        public bool APOrientationsVisible;
        /// <summary>
        /// Holds current diameter of action points
        /// </summary>
        public float APSize = 0.2f;
        /// <summary>
        /// Indicates if project is loaded
        /// </summary>
        public bool Valid = false;
        /// <summary>
        /// Indicates if editation of project is allowed.
        /// </summary>
        public bool AllowEdit = false;
        /// <summary>
        /// Indicates if project was changed since last save
        /// </summary>
        private bool projectChanged;
        /// <summary>
        /// Public setter for project changed property. Invokes OnProjectChanged event with each change and
        /// OnProjectSavedSatusChanged when projectChanged value differs from original value (i.e. when project
        /// was not changed and now it is and vice versa) 
        /// </summary>
        public bool ProjectChanged {
            get => projectChanged;
            set {
                bool origVal = projectChanged;
                projectChanged = value;
                if (!Valid)
                    return;
                OnProjectChanged?.Invoke(this, EventArgs.Empty);
                if (origVal != value) {
                    OnProjectSavedSatusChanged?.Invoke(this, EventArgs.Empty);
                }
            } 
        }

        /// <summary>
        /// Invoked when some of the action point weas updated. Contains action point description
        /// </summary>
        //public event AREditorEventArgs.ActionPointUpdatedEventHandler OnActionPointUpdated; 
        /// <summary>
        /// Invoked when project loaded
        /// </summary>
        public event EventHandler OnLoadProject;
        /// <summary>
        /// Invoked when project changed
        /// </summary>
        public event EventHandler OnProjectChanged;
        /// <summary>
        /// Invoked when project saved
        /// </summary>
        public event EventHandler OnProjectSaved;
        /// <summary>
        /// Invoked when projectChanged value differs from original value (i.e. when project
        /// was not changed and now it is and vice versa) 
        /// </summary>
        public event EventHandler OnProjectSavedSatusChanged;

        public event AREditorEventArgs.ActionPointEventHandler OnActionPointAddedToScene;

        public event AREditorEventArgs.ActionPointOrientationEventHandler OnActionPointOrientationAdded;
        public event AREditorEventArgs.ActionPointOrientationEventHandler OnActionPointOrientationUpdated;
        public event AREditorEventArgs.ActionPointOrientationEventHandler OnActionPointOrientationBaseUpdated;
        public event AREditorEventArgs.StringEventHandler OnActionPointOrientationRemoved;

        /// <summary>
        /// Initialization of projet manager
        /// </summary>
        private void Start() {
            WebsocketManager.Instance.OnLogicItemAdded += OnLogicItemAdded;
            WebsocketManager.Instance.OnLogicItemRemoved += OnLogicItemRemoved;
            WebsocketManager.Instance.OnLogicItemUpdated += OnLogicItemUpdated;
            WebsocketManager.Instance.OnProjectBaseUpdated += OnProjectBaseUpdated;

            WebsocketManager.Instance.OnActionPointAdded += OnActionPointAdded;
            WebsocketManager.Instance.OnActionPointRemoved += OnActionPointRemoved;
            WebsocketManager.Instance.OnActionPointUpdated += OnActionPointUpdated;
            WebsocketManager.Instance.OnActionPointBaseUpdated += OnActionPointBaseUpdated;

            WebsocketManager.Instance.OnActionPointOrientationAdded += OnActionPointOrientationAddedCallback;
            WebsocketManager.Instance.OnActionPointOrientationUpdated += OnActionPointOrientationUpdatedCallback;
            WebsocketManager.Instance.OnActionPointOrientationBaseUpdated += OnActionPointOrientationBaseUpdatedCallback;
            WebsocketManager.Instance.OnActionPointOrientationRemoved += OnActionPointOrientationRemovedCallback;

            WebsocketManager.Instance.OnActionPointJointsAdded += OnActionPointJointsAdded;
            WebsocketManager.Instance.OnActionPointJointsUpdated += OnActionPointJointsUpdated;
            WebsocketManager.Instance.OnActionPointJointsBaseUpdated += OnActionPointJointsBaseUpdated;
            WebsocketManager.Instance.OnActionPointJointsRemoved += OnActionPointJointsRemoved;
        }

        private void OnActionPointJointsRemoved(object sender, StringEventArgs args) {
            try {
                ActionPoint actionPoint = GetActionPointWithJoints(args.Data);
                actionPoint.RemoveJoints(args.Data);
                ProjectChanged = true;
                OnProjectChanged?.Invoke(this, EventArgs.Empty);
            } catch (KeyNotFoundException ex) {
                Debug.LogError(ex);
                Notifications.Instance.ShowNotification("Failed to remove action point joints", ex.Message);
                return;
            }
        }

        private void OnActionPointJointsBaseUpdated(object sender, RobotJointsEventArgs args) {
            try {
                ActionPoint actionPoint = GetActionPointWithJoints(args.Data.Id);
                actionPoint.BaseUpdateJoints(args.Data);
                ProjectChanged = true;
                OnProjectChanged?.Invoke(this, EventArgs.Empty);
            } catch (KeyNotFoundException ex) {
                Debug.LogError(ex);
                Notifications.Instance.ShowNotification("Failed to update action point joints", ex.Message);
                return;
            }
        }

        private void OnActionPointJointsUpdated(object sender, RobotJointsEventArgs args) {
            try {
                ActionPoint actionPoint = GetActionPointWithJoints(args.Data.Id);
                actionPoint.UpdateJoints(args.Data);
                ProjectChanged = true;
            } catch (KeyNotFoundException ex) {
                Debug.LogError(ex);
                Notifications.Instance.ShowNotification("Failed to update action point joints", ex.Message);
                return;
            }
        }

        private void OnActionPointJointsAdded(object sender, RobotJointsEventArgs args) {
            try {
                ActionPoint actionPoint = GetActionPoint(args.ActionPointId);
                actionPoint.AddJoints(args.Data);
                ProjectChanged = true;
            } catch (KeyNotFoundException ex) {
                Debug.LogError(ex);
                Notifications.Instance.ShowNotification("Failed to add action point joints", ex.Message);
                return;
            }
        }

        private void OnActionPointBaseUpdated(object sender, BareActionPointEventArgs args) {
            try {
                ActionPoint actionPoint = GetActionPoint(args.ActionPoint.Id);
                actionPoint.ActionPointBaseUpdate(args.ActionPoint);
                ProjectChanged = true;
            } catch (KeyNotFoundException ex) {
                Debug.Log("Action point " + args.ActionPoint.Id + " not found!");
                Notifications.Instance.ShowNotification("", "Action point " + args.ActionPoint.Id + " not found!");
                return;
            }
        }

        private void OnActionPointOrientationBaseUpdatedCallback(object sender, ActionPointOrientationEventArgs args) {
            try {
                ActionPoint actionPoint = ProjectManager.Instance.GetActionPointWithOrientation(args.Data.Id);
                actionPoint.BaseUpdateOrientation(args.Data);
                ProjectChanged = true;
                OnActionPointOrientationBaseUpdated?.Invoke(this, args);
            } catch (KeyNotFoundException ex) {
                Debug.LogError(ex);
                Notifications.Instance.ShowNotification("Failed to update action point orientation", ex.Message);
                return;
            }
        }

        private void OnActionPointOrientationRemovedCallback(object sender, StringEventArgs args) {
            try {
                ActionPoint actionPoint = GetActionPointWithOrientation(args.Data);
                actionPoint.RemoveOrientation(args.Data);
                ProjectChanged = true;
                OnActionPointOrientationRemoved?.Invoke(this, args);
            } catch (KeyNotFoundException ex) {
                Debug.LogError(ex);
                Notifications.Instance.ShowNotification("Failed to remove action point orientation", ex.Message);
                return;
            }
        }

        private void OnActionPointOrientationUpdatedCallback(object sender, ActionPointOrientationEventArgs args) {
            try {
                ActionPoint actionPoint = ProjectManager.Instance.GetActionPointWithOrientation(args.Data.Id);
                actionPoint.UpdateOrientation(args.Data);
                ProjectChanged = true;
                OnActionPointOrientationUpdated?.Invoke(this, args);
            } catch (KeyNotFoundException ex) {
                Debug.LogError(ex);
                Notifications.Instance.ShowNotification("Failed to update action point orientation", ex.Message);
                return;
            }
        }

        private void OnActionPointOrientationAddedCallback(object sender, ActionPointOrientationEventArgs args) {
            try {
                ActionPoint actionPoint = GetActionPoint(args.ActionPointId);
                actionPoint.AddOrientation(args.Data);
                ProjectChanged = true;
                OnActionPointOrientationAdded?.Invoke(this, args);
            } catch (KeyNotFoundException ex) {
                Debug.LogError(ex);
                Notifications.Instance.ShowNotification("Failed to add action point orientation", ex.Message);
                return;
            }
        }

        private void OnActionPointRemoved(object sender, StringEventArgs args) {
            RemoveActionPoint(args.Data);
            ProjectChanged = true;
        }

        private void OnActionPointUpdated(object sender, ProjectActionPointEventArgs args) {
            try {
                ActionPoint actionPoint = GetActionPoint(args.ActionPoint.Id);
                actionPoint.UpdateActionPoint(args.ActionPoint);
                ProjectChanged = true;
                // TODO - update orientations, joints etc.
            } catch (KeyNotFoundException ex) {
                Debug.LogError("Action point " + args.ActionPoint.Id + " not found!");
                Notifications.Instance.ShowNotification("", "Action point " + args.ActionPoint.Id + " not found!");
                return;
            }
        }
        

        private void OnActionPointAdded(object sender, ProjectActionPointEventArgs data) {
            if (data.ActionPoint.Parent == null || data.ActionPoint.Parent == "") {
                SpawnActionPoint(data.ActionPoint, null);
            } else {
                try {
                    IActionPointParent actionPointParent = GetActionPointParent(data.ActionPoint.Parent);
                    ActionPoint ap = SpawnActionPoint(data.ActionPoint, actionPointParent);
                } catch (KeyNotFoundException ex) {
                    Debug.LogError(ex);
                }

            }
            ProjectChanged = true;
        }

        private void OnProjectBaseUpdated(object sender, BareProjectEventArgs args) {
            if (GameManager.Instance.GetGameState() == GameManager.GameStateEnum.ProjectEditor) {
                SetProjectMeta(args.Project);
                ProjectChanged = true;
            } 
        }

        /// <summary>
        /// Creates project from given json
        /// </summary>
        /// <param name="project">Project descriptoin in json</param>
        /// <param name="allowEdit">Sets if project is editable</param>
        /// <returns>True if project sucessfully created</returns>
        public async Task<bool> CreateProject(IO.Swagger.Model.Project project, bool allowEdit) {
            Debug.Assert(ActionsManager.Instance.ActionsReady);
            if (ProjectMeta != null)
                return false;

            SetProjectMeta(DataHelper.ProjectToBareProject(project));
            AllowEdit = allowEdit;
            LoadSettings();

            StartAction = Instantiate(StartPrefab,  SceneManager.Instance.SceneOrigin.transform).GetComponent<StartAction>();
            EndAction = Instantiate(EndPrefab, SceneManager.Instance.SceneOrigin.transform).GetComponent<EndAction>();

            UpdateActionPoints(project);
            if (project.HasLogic)
                UpdateLogicItems(project.Logic);

            projectChanged = project.Modified == DateTime.MinValue;
            OnLoadProject?.Invoke(this, EventArgs.Empty);
            
            Valid = true;
            (bool successClose, _) = await GameManager.Instance.CloseProject(false, true);
            ProjectChanged = !successClose;
            return true;
        }


        /// <summary>
        /// Destroys current project
        /// </summary>
        /// <returns>True if project successfully destroyed</returns>
        public bool DestroyProject() {
            Valid = false;
            ProjectMeta = null;
            foreach (ActionPoint ap in ActionPoints.Values) {
                ap.DeleteAP(false);
            }
            if (StartAction != null) {
                Destroy(StartAction.gameObject);
                StartAction = null;
            }               
            if (EndAction != null) {
                Destroy(EndAction.gameObject);
                EndAction = null;
            }
            ActionPoints.Clear();
            return true;
        }

        /// <summary>
        /// Updates logic items
        /// </summary>
        /// <param name="logic">List of logic items</param>
        private void UpdateLogicItems(List<IO.Swagger.Model.LogicItem> logic) {
            foreach (IO.Swagger.Model.LogicItem projectLogicItem in logic) {
                if (!LogicItems.TryGetValue(projectLogicItem.Id, out LogicItem logicItem)) {
                    logicItem = new LogicItem(projectLogicItem);
                    LogicItems.Add(logicItem.Data.Id, logicItem);
                }
                logicItem.UpdateConnection(projectLogicItem);

            }
        }

        /// <summary>
        /// Updates logic item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnLogicItemUpdated(object sender, LogicItemChangedEventArgs args) {
            if (LogicItems.TryGetValue(args.Data.Id, out LogicItem logicItem)) {
                logicItem.Data = args.Data;
                logicItem.UpdateConnection(args.Data);
            } else {
                Debug.LogError("Server tries to update logic item that does not exists (id: " + args.Data.Id + ")");
            }
        }

        /// <summary>
        /// Removes logic item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnLogicItemRemoved(object sender, StringEventArgs args) {
            if (LogicItems.TryGetValue(args.Data, out LogicItem logicItem)) {
                logicItem.Remove();
                LogicItems.Remove(args.Data);
            } else {
                Debug.LogError("Server tries to remove logic item that does not exists (id: " + args.Data + ")");
            }
        }

        /// <summary>
        /// Adds logic item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnLogicItemAdded(object sender, LogicItemChangedEventArgs args) {
            LogicItem logicItem = new LogicItem(args.Data);
            LogicItems.Add(args.Data.Id, logicItem);
        }

        /// <summary>
        /// Sets project metadata
        /// </summary>
        /// <param name="project"></param>
        public void SetProjectMeta(BareProject project) {
            if (ProjectMeta == null) {
                ProjectMeta = new Project(sceneId: "", id: "", name: "");
            }
            ProjectMeta.Id = project.Id;
            ProjectMeta.SceneId = project.SceneId;
            ProjectMeta.HasLogic = project.HasLogic;
            ProjectMeta.Desc = project.Desc;
            ProjectMeta.IntModified = project.IntModified;
            ProjectMeta.Modified = project.Modified;
            ProjectMeta.Name = project.Name;
            
        }

        /// <summary>
        /// Gets json describing project
        /// </summary>
        /// <returns></returns>
        public Project GetProject() {
            if (ProjectMeta == null)
                return null;
            Project project = ProjectMeta;
            project.ActionPoints = new List<IO.Swagger.Model.ActionPoint>();
            foreach (ActionPoint ap in ActionPoints.Values) {
                IO.Swagger.Model.ActionPoint projectActionPoint = ap.Data;
                foreach (Action action in ap.Actions.Values) {
                    IO.Swagger.Model.Action projectAction = new IO.Swagger.Model.Action(id: action.Data.Id,
                        name: action.Data.Name, type: action.Data.Type) {
                        Parameters = new List<IO.Swagger.Model.ActionParameter>()                        
                    };
                    foreach (Parameter param in action.Parameters.Values) {
                        projectAction.Parameters.Add(DataHelper.ParameterToActionParameter(param));
                    }
                }
                project.ActionPoints.Add(projectActionPoint);
            }
            return project;
        }


        private void Update() {
            if (GameManager.Instance.GetGameState() == GameManager.GameStateEnum.ProjectEditor &&
                GameManager.Instance.SceneInteractable &&
                GameManager.Instance.GetEditorState() == GameManager.EditorStateEnum.Normal) {
                if (!projectActive && (ControlBoxManager.Instance.UseGizmoMove)) {
                    ActivateActionPointsForGizmo(true);
                    projectActive = true;
                } else if (projectActive && !(ControlBoxManager.Instance.UseGizmoMove)) {
                    ActivateActionPointsForGizmo(false);
                    projectActive = false;
                }
            } else {
                if (projectActive) {
                    ActivateActionPointsForGizmo(false);
                    projectActive = false;
                }
            }
        }

        /// <summary>
        /// Deactivates or activates all action points in scene for gizmo interaction.
        /// </summary>
        /// <param name="activate"></param>
        private void ActivateActionPointsForGizmo(bool activate) {
            if (activate) {
                //TODO: this should probably be Scene
                gameObject.layer = LayerMask.NameToLayer("GizmoRuntime");
                foreach (ActionPoint actionPoint in GetAllActionPoints()) {
                    actionPoint.ActivateForGizmo("GizmoRuntime");
                }
            } else {
                gameObject.layer = LayerMask.NameToLayer("Default");
                foreach (ActionPoint actionPoint in GetAllActionPoints()) {
                    actionPoint.ActivateForGizmo("Default");
                }
            }
        }

        /// <summary>
        /// Loads project settings from persistant storage
        /// </summary>
        public void LoadSettings() {
            APOrientationsVisible = PlayerPrefsHelper.LoadBool("project/" + ProjectMeta.Id + "/APOrientationsVisibility", true);
            APSize = PlayerPrefsHelper.LoadFloat("project/" + ProjectMeta.Id + "/APSize", 0.2f);
        }

        /// <summary>
        /// Spawn action point into the project
        /// </summary>
        /// <param name="apData">Json describing action point</param>
        /// <param name="actionPointParent">Parent of action point. If null, AP is spawned as global.</param>
        /// <returns></returns>
        public ActionPoint SpawnActionPoint(IO.Swagger.Model.ActionPoint apData, IActionPointParent actionPointParent) {
            Debug.Assert(apData != null);
            GameObject AP;
            if (actionPointParent == null) {               
                AP = Instantiate(ActionPointPrefab, ActionPointsOrigin.transform);
            } else {
                AP = Instantiate(ActionPointPrefab, actionPointParent.GetTransform());
            }

            AP.transform.localScale = new Vector3(1f, 1f, 1f);
            ActionPoint actionPoint = AP.GetComponent<ActionPoint>();
            actionPoint.InitAP(apData, APSize, actionPointParent);
            ActionPoints.Add(actionPoint.Data.Id, actionPoint);
            OnActionPointAddedToScene?.Invoke(this, new ActionPointEventArgs(actionPoint));
            return actionPoint;
        }

        /// <summary>
        /// Finds free AP name, based on given name (e.g. globalAP, globalAP_1, globalAP_2 etc.)
        /// </summary>
        /// <param name="apDefaultName">Name of parent or "globalAP"</param>
        /// <returns></returns>
        public string GetFreeAPName(string apDefaultName) {
            int i = 2;
            bool hasFreeName;
            string freeName = apDefaultName + "_ap";
            do {
                hasFreeName = true;
                if (ActionPointsContainsName(freeName)) {
                    hasFreeName = false;
                }
                if (!hasFreeName)
                    freeName = apDefaultName + "_ap_" + i++.ToString();
            } while (!hasFreeName);

            return freeName;
        }

        /// <summary>
        /// Checks if action point with given name exists
        /// </summary>
        /// <param name="name">Human readable action point name</param>
        /// <returns></returns>
        public bool ActionPointsContainsName(string name) {
            foreach (ActionPoint ap in GetAllActionPoints()) {
                if (ap.Data.Name == name)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Hides all arrows representing action point orientations
        /// </summary>
        internal void HideAPOrientations() {
            APOrientationsVisible = false;
            foreach (ActionPoint actionPoint in GetAllActionPoints()) {
                actionPoint.UpdateOrientationsVisuals(false);
            }
            PlayerPrefsHelper.SaveBool("scene/" + ProjectMeta.Id + "/APOrientationsVisibility", false);
        }

        /// <summary>
        /// Shows all arrows representing action point orientations
        /// </summary>
        internal void ShowAPOrientations() {
            APOrientationsVisible = true;
            foreach (ActionPoint actionPoint in GetAllActionPoints()) {
                actionPoint.UpdateOrientationsVisuals(true);
            }
            PlayerPrefsHelper.SaveBool("scene/" + ProjectMeta.Id + "/APOrientationsVisibility", true);
        }


        /// <summary>
        /// Updates action point GameObject in ActionObjects.ActionPoints dict based on the data present in IO.Swagger.Model.ActionPoint Data.
        /// </summary>
        /// <param name="project"></param>
        public void UpdateActionPoints(Project project) {
            List<string> currentAP = new List<string>();
            List<string> currentActions = new List<string>();
            // key = parentId, value = list of APs with given parent
            Dictionary<string, List<IO.Swagger.Model.ActionPoint>> actionPointsWithParents = new Dictionary<string, List<IO.Swagger.Model.ActionPoint>>();
            // ordered list of already processed parents. This ensure that global APs are processed first,
            // then APs with action objects as a parents and then APs with already processed AP parents
            List<string> processedParents = new List<string> {
                "global"
            };
            foreach (IO.Swagger.Model.ActionPoint projectActionPoint in project.ActionPoints) {
                string parent = projectActionPoint.Parent;
                if (string.IsNullOrEmpty(parent)) {
                    parent = "global";
                }
                if (actionPointsWithParents.TryGetValue(parent, out List<IO.Swagger.Model.ActionPoint> projectActionPoints)) {
                    projectActionPoints.Add(projectActionPoint);
                } else {
                    List<IO.Swagger.Model.ActionPoint> aps = new List<IO.Swagger.Model.ActionPoint> {
                        projectActionPoint
                    };
                    actionPointsWithParents[parent] = aps;
                }
                // if parent is action object, we dont need to process it
                if (SceneManager.Instance.ActionObjects.ContainsKey(parent)) {
                    processedParents.Add(parent);
                }
            }

            for (int i = 0; i < processedParents.Count; ++i) {
                if (actionPointsWithParents.TryGetValue(processedParents[i], out List<IO.Swagger.Model.ActionPoint> projectActionPoints)) {
                    foreach (IO.Swagger.Model.ActionPoint projectActionPoint in projectActionPoints) {
                        // if action point exist, just update it
                        if (ActionPoints.TryGetValue(projectActionPoint.Id, out ActionPoint actionPoint)) {
                            actionPoint.ActionPointBaseUpdate(DataHelper.ActionPointToBareActionPoint(projectActionPoint));
                        }
                        // if action point doesn't exist, create new one
                        else {
                            IActionPointParent parent = null;
                            if (!string.IsNullOrEmpty(projectActionPoint.Parent)) {
                                parent = ProjectManager.Instance.GetActionPointParent(projectActionPoint.Parent);
                            }
                            //TODO: update spawn action point to not need action object
                            actionPoint = SpawnActionPoint(projectActionPoint, parent);

                        }

                        // update actions in current action point 
                        (List<string>, Dictionary<string, string>) updateActionsResult = actionPoint.UpdateActionPoint(projectActionPoint);
                        currentActions.AddRange(updateActionsResult.Item1);
                        // merge dictionaries
                        //connections = connections.Concat(updateActionsResult.Item2).GroupBy(i => i.Key).ToDictionary(i => i.Key, i => i.First().Value);

                        actionPoint.UpdatePositionsOfPucks();

                        currentAP.Add(actionPoint.Data.Id);

                        processedParents.Add(projectActionPoint.Id);
                    }
                }
                
            }
            

            //UpdateActionConnections(project.ActionPoints, connections);

            // Remove deleted actions
            foreach (string actionId in GetAllActionsDict().Keys.ToList<string>()) {
                if (!currentActions.Contains(actionId)) {
                    RemoveAction(actionId);
                }
            }

            // Remove deleted action points
            foreach (string actionPointId in GetAllActionPointsDict().Keys.ToList<string>()) {
                if (!currentAP.Contains(actionPointId)) {
                    RemoveActionPoint(actionPointId);
                }
            }
        }

        /// <summary>
        /// Removes all action points in project
        /// </summary>
        public void RemoveActionPoints() {
            List<ActionPoint> actionPoints = ActionPoints.Values.ToList();
            foreach (ActionPoint actionPoint in actionPoints) {
                actionPoint.DeleteAP();
            }
        }

        /// <summary>
        /// Finds parent of action point based on its ID
        /// </summary>
        /// <param name="parentId">ID of parent object</param>
        /// <returns></returns>
        public IActionPointParent GetActionPointParent(string parentId) {
            if (parentId == null || parentId == "")
                throw new KeyNotFoundException("Action point parent " + parentId + " not found");
            if (SceneManager.Instance.ActionObjects.TryGetValue(parentId, out ActionObject actionObject)) {
                return actionObject;
            }
            if (ProjectManager.Instance.ActionPoints.TryGetValue(parentId, out ActionPoint actionPoint)) {
                return actionPoint;
            }

            throw new KeyNotFoundException("Action point parrent " + parentId + " not found");
        }

        /// <summary>
        /// Gets action points based on its human readable name
        /// </summary>
        /// <param name="name">Human readable name of action point</param>
        /// <returns></returns>
        public ActionPoint GetactionpointByName(string name) {
            foreach (ActionPoint ap in ActionPoints.Values) {
                if (ap.Data.Name == name)
                    return ap;
            }
            throw new KeyNotFoundException("Action point " + name + " not found");
        }



        /// <summary>
        /// Destroys and removes references to action point of given Id.
        /// </summary>
        /// <param name="Id"></param>
        public void RemoveActionPoint(string Id) {
            // Call function in corresponding action point that will delete it and properly remove all references and connections.
            // We don't want to update project, because we are calling this method only upon received update from server.
            ActionPoints[Id].DeleteAP();
        }

        /// <summary>
        /// Returns action point of given Id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionPoint GetActionPoint(string id) {
            if (ActionPoints.TryGetValue(id, out ActionPoint actionPoint)) {
                return actionPoint;
            }

            throw new KeyNotFoundException("ActionPoint \"" + id + "\" not found!");
        }


        /// <summary>
        /// Returns all action points in the scene in a list [ActionPoint_object]
        /// </summary>
        /// <returns></returns>
        public List<ActionPoint> GetAllActionPoints() {
            return ActionPoints.Values.ToList();
        }

        /// <summary>
        /// Returns all action points in the scene in a list [ActionPoint_object]
        /// </summary>
        /// <returns></returns>
        public List<ActionPoint> GetAllGlobalActionPoints() {
            return (from ap in ActionPoints
                    where ap.Value.Parent == null
                    select ap.Value).ToList();
        }

        /// <summary>
        /// Returns all action points in the scene in a dictionary [action_point_Id, ActionPoint_object]
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, ActionPoint> GetAllActionPointsDict() {
            return ActionPoints;
        }

        /// <summary>
        /// Returns joints with id or throws KeyNotFoundException
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IO.Swagger.Model.ProjectRobotJoints GetJoints(string id) {
            foreach (ActionPoint actionPoint in ActionPoints.Values) {
                try {
                    return actionPoint.GetJoints(id);
                } catch (KeyNotFoundException ex) { }
            }
            throw new KeyNotFoundException("Joints with id " + id + " not found");
        }

        /// <summary>
        /// Returns orientation with id or throws KeyNotFoundException
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IO.Swagger.Model.NamedOrientation GetNamedOrientation(string id) {
            foreach (ActionPoint actionPoint in ActionPoints.Values) {
                try {
                    return actionPoint.GetOrientation(id);
                } catch (KeyNotFoundException ex) { }
            }
            throw new KeyNotFoundException("Joints with id " + id + " not found");
        }

        /// <summary>
        /// Returns action point containing orientation with id or throws KeyNotFoundException
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionPoint GetActionPointWithOrientation(string id) {
            foreach (ActionPoint actionPoint in ActionPoints.Values) {
                try {
                    // if GetOrientation dont throw exception, correct action point was found
                    actionPoint.GetOrientation(id);
                    return actionPoint;
                } catch (KeyNotFoundException ex) { }
            }
            throw new KeyNotFoundException("Action point with orientation id " + id + " not found");
        }

        /// <summary>
        /// Returns action point containing joints with id or throws KeyNotFoundException
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionPoint GetActionPointWithJoints(string id) {
            foreach (ActionPoint actionPoint in ActionPoints.Values) {
                try {
                    // if GetJoints dont throw exception, correct action point was found
                    actionPoint.GetJoints(id);
                    return actionPoint;
                } catch (KeyNotFoundException ex) { }
            }
            throw new KeyNotFoundException("Action point with joints id " + id + " not found");
        }

        /// <summary>
        /// Change size of all action points
        /// </summary>
        /// <param name="size"><0; 1> From barely visible to quite big</param>
        public void SetAPSize(float size) {
            if (ProjectMeta != null)
                PlayerPrefsHelper.SaveFloat("project/" + ProjectMeta.Id + "/APSize", size);
            APSize = size;
            foreach (ActionPoint actionPoint in GetAllActionPoints()) {
                actionPoint.SetSize(size);
            }
        }

        /// <summary>
        /// Disables all action points
        /// </summary>
        public void DisableAllActionPoints() {
            foreach (ActionPoint ap in ActionPoints.Values) {
                ap.Disable();
            }
        }

        /// <summary>
        /// Enables all action points
        /// </summary>
        public void EnableAllActionPoints() {
            foreach (ActionPoint ap in ActionPoints.Values) {
                ap.Enable();
            }
        }

        /// <summary>
        /// Disables all actions
        /// </summary>
        public void DisableAllActions() {
            foreach (ActionPoint ap in ActionPoints.Values) {
                foreach (Action action in ap.Actions.Values)
                    action.Disable();
            }
            StartAction.Disable();
            EndAction.Disable();
        }

        /// <summary>
        /// Enables all actions
        /// </summary>
        public void EnableAllActions() {
            foreach (ActionPoint ap in ActionPoints.Values) {
                foreach (Action action in ap.Actions.Values)
                    action.Enable();
            }
            StartAction.Enable();
            EndAction.Enable();
        }

        /// <summary>
        /// Disables all action inputs
        /// </summary>
        public void DisableAllActionInputs() {
            foreach (ActionPoint ap in ActionPoints.Values) {
                foreach (Action action in ap.Actions.Values)
                    action.Input.Disable();
            }
            EndAction.Input.Disable();
        }

        /// <summary>
        /// Enables all action inputs
        /// </summary>
        public void EnableAllActionsInputs() {
            foreach (ActionPoint ap in ActionPoints.Values) {
                foreach (Action action in ap.Actions.Values)
                    action.Input.Enable();
            }
            EndAction.Input.Enable();
        }

        /// <summary>
        /// Disable all action outputs
        /// </summary>
        public void DisableAllActionOutputs() {
            foreach (ActionPoint ap in ActionPoints.Values) {
                foreach (Action action in ap.Actions.Values)
                    action.Output.Disable();
            }
            StartAction.Output.Disable();
        }

        /// <summary>
        /// Enables all action outputs
        /// </summary>
        public void EnableAllActionsOutputs() {
            foreach (ActionPoint ap in ActionPoints.Values) {
                foreach (Action action in ap.Actions.Values)
                    action.Output.Enable();
            }
            StartAction.Output.Enable();
        }

        #region ACTIONS

        public Action SpawnAction(IO.Swagger.Model.Action projectAction, ActionPoint ap) {
            //string action_id, string action_name, string action_type, 
            Debug.Assert(!ActionsContainsName(projectAction.Name));
            ActionMetadata actionMetadata;
            string providerName = projectAction.Type.Split('/').First();
            string actionType = projectAction.Type.Split('/').Last();
            IActionProvider actionProvider;
            try {
                actionProvider = SceneManager.Instance.GetActionObject(providerName);
            } catch (KeyNotFoundException ex) {
                throw new RequestFailedException("PROVIDER NOT FOUND EXCEPTION: " + providerName + " " + actionType);                
            }

            try {
                actionMetadata = actionProvider.GetActionMetadata(actionType);
            } catch (ItemNotFoundException ex) {
                Debug.LogError(ex);
                return null; //TODO: throw exception
            }

            if (actionMetadata == null) {
                Debug.LogError("Actions not ready");
                return null; //TODO: throw exception
            }
            GameObject puck = Instantiate(PuckPrefab, ap.ActionsSpawn.transform);
            puck.SetActive(false);

            puck.GetComponent<Action>().Init(projectAction, actionMetadata, ap, actionProvider);

            puck.transform.localScale = new Vector3(1f, 1f, 1f);

            Action action = puck.GetComponent<Action>();

            // Add new action into scene reference
            ActionPoints[ap.Data.Id].Actions.Add(action.Data.Id, action);

            ap.UpdatePositionsOfPucks();
            puck.SetActive(true);

            return action;
        }

        public string GetFreeActionName(string actionName) {
            int i = 2;
            bool hasFreeName;
            string freeName = actionName;
            do {
                hasFreeName = true;
                if (ActionsContainsName(freeName)) {
                    hasFreeName = false;
                }
                if (!hasFreeName)
                    freeName = actionName + "_" + i++.ToString();
            } while (!hasFreeName);

            return freeName;
        }


        /// <summary>
        /// Destroys and removes references to action of given Id.
        /// </summary>
        /// <param name="Id"></param>
        public void RemoveAction(string Id) {
            Action aToRemove = GetAction(Id);
            string apIdToRemove = aToRemove.ActionPoint.Data.Id;
            // Call function in corresponding action that will delete it and properly remove all references and connections.
            // We don't want to update project, because we are calling this method only upon received update from server.
            ActionPoints[apIdToRemove].Actions[Id].DeleteAction();
        }

        public bool ActionsContainsName(string name) {
            foreach (Action action in GetAllActions()) {
                if (action.Data.Name == name)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns action of given Id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Action GetAction(string id) {
            if (id == "START")
                return StartAction;
            else if (id == "END")
                return EndAction;
            foreach (ActionPoint actionPoint in ActionPoints.Values) {
                if (actionPoint.Actions.TryGetValue(id, out Action action)) {
                    return action;
                }
            }

            //Debug.LogError("Action " + Id + " not found!");
            throw new ItemNotFoundException("Action with ID " + id + " not found");
        }

        /// <summary>
        /// Returns action of given ID.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Action GetActionByName(string name) {
            foreach (ActionPoint actionPoint in ActionPoints.Values) {
                foreach (Action action in actionPoint.Actions.Values) {
                    if (action.Data.Name == name) {
                        return action;
                    }
                }
            }

            //Debug.LogError("Action " + id + " not found!");
            throw new ItemNotFoundException("Action with name " + name + " not found");
        }

        /// <summary>
        /// Returns all actions in the scene in a list [Action_object]
        /// </summary>
        /// <returns></returns>
        public List<Action> GetAllActions() {
            List<Action> actions = new List<Action>();
            foreach (ActionPoint actionPoint in ActionPoints.Values) {
                foreach (Action action in actionPoint.Actions.Values) {
                    actions.Add(action);
                }
            }

            return actions;
        }

        /// <summary>
        /// Returns all actions in the scene in a dictionary [action_Id, Action_object]
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, Action> GetAllActionsDict() {
            Dictionary<string, Action> actions = new Dictionary<string, Action>();
            foreach (ActionPoint actionPoint in ActionPoints.Values) {
                foreach (Action action in actionPoint.Actions.Values) {
                    actions.Add(action.Data.Id, action);
                }
            }

            return actions;
        }

        #endregion

        /// <summary>
        /// Updates action 
        /// </summary>
        /// <param name="projectAction">Action description</param>
        public void ActionUpdated(IO.Swagger.Model.Action projectAction) {
            Base.Action action = GetAction(projectAction.Id);
            if (action == null) {
                Debug.LogError("Trying to update non-existing action!");
                return;
            }
            action.ActionUpdate(projectAction, true);
            ProjectChanged = true;
        }

        /// <summary>
        /// Updates metadata of action
        /// </summary>
        /// <param name="projectAction">Action description</param>
        public void ActionBaseUpdated(IO.Swagger.Model.BareAction projectAction) {
            Base.Action action = GetAction(projectAction.Id);
            if (action == null) {
                Debug.LogError("Trying to update non-existing action!");
                return;
            }
            action.ActionUpdateBaseData(projectAction);
            ProjectChanged = true;
        }

        /// <summary>
        /// Adds action to the project
        /// </summary>
        /// <param name="projectAction">Action description</param>
        /// <param name="parentId">UUID of action point to which the action should be added</param>
        public void ActionAdded(IO.Swagger.Model.Action projectAction, string parentId) {
            ActionPoint actionPoint = GetActionPoint(parentId);
            try {
                Base.Action action = SpawnAction(projectAction, actionPoint);
                // updates name of the action
                action.ActionUpdateBaseData(DataHelper.ActionToBareAction(projectAction));
                // updates parameters of the action
                action.ActionUpdate(projectAction);
                ProjectChanged = true;
            } catch (RequestFailedException ex) {
                Debug.LogError(ex);
            }            
        }

        /// <summary>
        /// Removes actino from project
        /// </summary>
        /// <param name="action"></param>
        public void ActionRemoved(IO.Swagger.Model.BareAction action) {
            ProjectManager.Instance.RemoveAction(action.Id);
            ProjectChanged = true;
        }

        /// <summary>
        /// Called when project saved. Invokes OnProjectSaved event
        /// </summary>
        internal void ProjectSaved() {
            ProjectChanged = false;
            Notifications.Instance.ShowToastMessage("Project saved successfully.");
            OnProjectSaved?.Invoke(this, EventArgs.Empty);
        }

        public void HighlightOrientation(string orientationId, bool highlight) {
            ActionPoint ap = GetActionPointWithOrientation(orientationId);
            APOrientation orientation = ap.GetOrientationVisual(orientationId);
            orientation.HighlightOrientation(highlight);
        }


        

    }


}
