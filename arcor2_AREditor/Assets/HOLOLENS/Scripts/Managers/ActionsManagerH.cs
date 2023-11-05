using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using IO.Swagger.Model;
using System.Threading.Tasks;
using System.Collections;
using Newtonsoft.Json;
using UnityEngine.XR.ARSubsystems;
using Base;
using Hololens;

public class ActionsManagerH :  Singleton<ActionsManagerH>
{

    private Dictionary<string, ActionObjectMetadataH> actionObjectsMetadata = new Dictionary<string, ActionObjectMetadataH>();

    public Dictionary<string, RobotMeta> RobotsMeta = new Dictionary<string, RobotMeta>();
   
    public Dictionary<string, ActionObjectMetadataH> ActionObjectsMetadata {
        get => actionObjectsMetadata; set => actionObjectsMetadata = value;
    }

    public event EventHandler OnServiceMetadataUpdated, OnActionsLoaded;

 /*   public GameObject LinkableParameterInputPrefab, LinkableParameterDropdownPrefab, LinkableParameterDropdownPosesPrefab,
        ParameterDropdownJointsPrefab, ActionPointOrientationPrefab, ParameterRelPosePrefab,
        LinkableParameterBooleanPrefab, ParameterDropdownPrefab;*/

    public GameObject InteractiveObjects;

    [HideInInspector]
    public bool ActionsReady, ActionObjectsLoaded, AbstractOnlyObjects;

    public event AREditorEventArgs.StringListEventHandler OnObjectTypesAdded, OnObjectTypesRemoved, OnObjectTypesUpdated;


    private void Awake() {
        ActionsReady = false;
        ActionObjectsLoaded = false;
    }
    // Start is called before the first frame update
    void Start()
    {
      /*  Debug.Assert(LinkableParameterInputPrefab != null);
        Debug.Assert(LinkableParameterDropdownPrefab != null);
        Debug.Assert(LinkableParameterDropdownPosesPrefab != null);
        Debug.Assert(ParameterDropdownJointsPrefab != null);
        Debug.Assert(ParameterRelPosePrefab != null);*/
        Debug.Assert(InteractiveObjects != null);
        Init();
        WebSocketManagerH.Instance.OnDisconnectEvent += OnDisconnected;
        WebSocketManagerH.Instance.OnObjectTypeAdded += ObjectTypeAdded;
        WebSocketManagerH.Instance.OnObjectTypeRemoved += ObjectTypeRemoved;
        WebSocketManagerH.Instance.OnObjectTypeUpdated += ObjectTypeUpdated;
    }

    private void OnDisconnected(object sender, EventArgs args) {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
         if (!ActionsReady && ActionObjectsLoaded) {

            foreach (ActionObjectMetadataH ao in ActionObjectsMetadata.Values) {

                if (!ao.Disabled && !ao.ActionsLoaded) {
                    return;
                }
            }

            ActionsReady = true;
            OnActionsLoaded?.Invoke(this, EventArgs.Empty);
            enabled = false;

        }
    }

    
    public void Init() {
        // servicesMetadata.Clear();
        actionObjectsMetadata.Clear();
        AbstractOnlyObjects = true;
        ActionsReady = false;
        ActionObjectsLoaded = false;
    }

    public async void ObjectTypeUpdated(object sender, ObjectTypesEventArgs args) {
        ActionsReady = false;
        enabled = true;
        bool updatedRobot = false;
        List<string> updated = new List<string>();
        foreach (ObjectTypeMeta obj in args.ObjectTypes) {
            if (actionObjectsMetadata.TryGetValue(obj.Type, out ActionObjectMetadataH actionObjectMetadata)) {
                actionObjectMetadata.Update(obj);
                if (actionObjectMetadata.Robot)
                    updatedRobot = true;
                if (AbstractOnlyObjects && !actionObjectMetadata.Abstract)
                    AbstractOnlyObjects = false;
                if (!actionObjectMetadata.Abstract && !actionObjectMetadata.BuiltIn)
                    UpdateActionsOfActionObject(actionObjectMetadata);
                else
                    actionObjectMetadata.ActionsLoaded = true;
                updated.Add(obj.Type);
                foreach (ActionObjectH updatedObj in SceneManagerH.Instance.GetAllObjectsOfType(obj.Type)) {
                    updatedObj.UpdateModel();
                }
            } else {
   //             Notifications.Instance.ShowNotification("Update of object types failed", "Server trying to update non-existing object!");
            }
        }
        if (updatedRobot){
            UpdateRobotsMetadata(await WebSocketManagerH.Instance.GetRobotMeta());
        }
            
        OnObjectTypesUpdated?.Invoke(this, new StringListEventArgs(updated));
    }


    public async void ObjectTypeAdded(object sender, ObjectTypesEventArgs args) {
        ActionsReady = false;
        enabled = true;
        bool robotAdded = false;
        List<string> added = new List<string>();
        foreach (ObjectTypeMeta obj in args.ObjectTypes) {
            ActionObjectMetadataH m = new ActionObjectMetadataH(meta: obj);
            if (AbstractOnlyObjects && !m.Abstract)
                AbstractOnlyObjects = false;
            if (!m.Abstract && !m.BuiltIn)
                UpdateActionsOfActionObject(m);
            else
                m.ActionsLoaded = true;
            m.Robot = IsDescendantOfType("Robot", m);
            m.Camera = IsDescendantOfType("Camera", m);
            m.CollisionObject = IsDescendantOfType("VirtualCollisionObject", m);
            actionObjectsMetadata.Add(obj.Type, m);
            if (m.Robot)
                robotAdded = true;
            added.Add(obj.Type);
        }
        if (robotAdded){
          
            UpdateRobotsMetadata(await WebSocketManagerH.Instance.GetRobotMeta());
        }
            
        
        OnObjectTypesAdded?.Invoke(this, new StringListEventArgs(added));//THIS IS FOR MENU ADD OBJECT FOR SELECT
    }


    private bool IsDescendantOfType(string type, ActionObjectMetadataH actionObjectMetadata) {
        if (actionObjectMetadata.Type == type)
            return true;
        if (actionObjectMetadata.Type == "Generic")
            return false;
        foreach (KeyValuePair<string, ActionObjectMetadataH> kv in actionObjectsMetadata) {
            if (kv.Key == actionObjectMetadata.Base) {
                return IsDescendantOfType(type, kv.Value);
            }
        }
        return false;
    }


    
    private void UpdateActionsOfActionObject(ActionObjectMetadataH actionObject) {
        if (!actionObject.Disabled)
            try {
                WebSocketManagerH.Instance.GetActions(actionObject.Type, GetActionsCallback);                    
            } catch (RequestFailedException e) {
                Debug.LogError("Failed to load action for object " + actionObject.Type);
                
             
            HNotificationManager.Instance.ShowNotification("Failed to load actions Failed to load action for object " + actionObject.Type);
         //       Notifications.Instance.SaveLogs();
            }            
    }

    public void GetActionsCallback(string actionName, string data) {
        IO.Swagger.Model.GetActionsResponse getActionsResponse = JsonConvert.DeserializeObject<IO.Swagger.Model.GetActionsResponse>(data);
        if (actionObjectsMetadata.TryGetValue(actionName, out ActionObjectMetadataH actionObject)) {
            actionObject.ActionsMetadata = ParseActions(getActionsResponse.Data);
            if (actionObject.ActionsMetadata == null) {
                actionObject.Disabled = true;
                actionObject.Problem = "Failed to load actions";
            }
            actionObject.ActionsLoaded = true;
        }
    }

    private Dictionary<string, ActionMetadataH> ParseActions(List<IO.Swagger.Model.ObjectAction> actions) {
        if (actions == null) {
            return null;
        }
        Dictionary<string, ActionMetadataH> metadata = new Dictionary<string, ActionMetadataH>();
        foreach (IO.Swagger.Model.ObjectAction action in actions) {
            ActionMetadataH a = new ActionMetadataH(action);
            metadata[a.Name] = a;
        }
        return metadata;
    }

    internal void UpdateRobotsMetadata(List<RobotMeta> list) {
        RobotsMeta.Clear();
        foreach (RobotMeta robotMeta in list) {
            Debug.Log(robotMeta);
            RobotsMeta[robotMeta.Type] = robotMeta;
        }
    }

    
    public void WaitUntilActionsReady(int timeout) {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        while (!ActionsReady) {
            if (sw.ElapsedMilliseconds > timeout)
                throw new TimeoutException();
            System.Threading.Thread.Sleep(100);
        }
    }

    public void UpdateObjects(List<IO.Swagger.Model.ObjectTypeMeta> newActionObjectsMetadata) {
        ActionsReady = false;
        actionObjectsMetadata.Clear();
        foreach (IO.Swagger.Model.ObjectTypeMeta metadata in newActionObjectsMetadata) {
            ActionObjectMetadataH m = new ActionObjectMetadataH(meta: metadata);
            if (AbstractOnlyObjects && !m.Abstract)
                AbstractOnlyObjects = false;
            if (!m.Abstract && !m.BuiltIn)
                UpdateActionsOfActionObject(m);
            else
                m.ActionsLoaded = true;
            actionObjectsMetadata.Add(metadata.Type, m);
        }
        foreach (KeyValuePair<string, ActionObjectMetadataH> kv in actionObjectsMetadata) {
            kv.Value.Robot = IsDescendantOfType("Robot", kv.Value);
            kv.Value.Camera = IsDescendantOfType("Camera", kv.Value);
            kv.Value.CollisionObject = IsDescendantOfType("VirtualCollisionObject", kv.Value);
        }
        enabled = true;
       
        ActionObjectsLoaded = true;
    }

    public void ObjectTypeRemoved(object sender, StringListEventArgs type) {
        List<string> removed = new List<string>();
        foreach (string item in type.Data) {
            if (actionObjectsMetadata.ContainsKey(item)) {
                actionObjectsMetadata.Remove(item);
                removed.Add(item);
            }
        }
        if (type.Data.Count > 0) {
            AbstractOnlyObjects = true;
            foreach (ActionObjectMetadataH obj in actionObjectsMetadata.Values) {
                if (AbstractOnlyObjects && !obj.Abstract)
                    AbstractOnlyObjects = false;
            }
            OnObjectTypesRemoved?.Invoke(this, new StringListEventArgs(new List<string>(removed)));
        }

    }


}
