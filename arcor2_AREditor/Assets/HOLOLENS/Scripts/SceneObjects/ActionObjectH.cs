using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using IO.Swagger.Model;
using System;
using Base;
using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine.XR.Interaction.Toolkit;
using RequestResult = Base.RequestResult;
using System.Threading;
using MixedReality.Toolkit;
using Microsoft.MixedReality.GraphicsTools;
using System.Linq;


namespace Hololens
{
    public abstract class ActionObjectH : HInteractiveObject, IActionProviderH, IActionPointParentH
    {
        public GameObject ActionPointsSpawn;
        [System.NonSerialized]
        public int CounterAP = 0;
        protected float visibility;

        public Collider Collider;

        public GameObject InteractionObject => gameObject;
        public GameObject InteractionObjectCollider;
        public GameObject Visual;
        public GameObject BoundsControlBoxPrefab;
        public Material OutlineStencilMaterial;
        public Material OutlineMaterial;
        public GameObject AddActionReticle;

        public IO.Swagger.Model.SceneObject Data = new IO.Swagger.Model.SceneObject(id: "", name: "", pose: DataHelper.CreatePose(new Vector3(), new Quaternion()), type: "");
        public ActionObjectMetadataH ActionObjectMetadata;

        public Dictionary<string, Base.Parameter> ObjectParameters = new Dictionary<string, Base.Parameter>();
        public Dictionary<string, Base.Parameter> Overrides = new Dictionary<string, Base.Parameter>();
        private bool allowScale;
        private Vector3 initialPosition;
        private Quaternion initialRotation;


        public virtual void InitActionObject(IO.Swagger.Model.SceneObject sceneObject, Vector3 position, Quaternion orientation, ActionObjectMetadataH actionObjectMetadata, IO.Swagger.Model.CollisionModels customCollisionModels = null, bool loadResuources = true)
        {
            Data.Id = sceneObject.Id;
            Data.Type = sceneObject.Type;
            name = sceneObject.Name; // show actual object name in unity hierarchy
            ActionObjectMetadata = actionObjectMetadata;

            if (actionObjectMetadata.HasPose)
            {
                SetScenePosition(position);
                SetSceneOrientation(orientation);
            }

            CreateModel(customCollisionModels);
            enabled = true;

            if (PlayerPrefsHelper.LoadBool($"ActionObject/{GetId()}/blocklisted", false))
            {
                Enable(false, true, false);
            }

            HSelectorManager.Instance.StartedPlacingActionPoint += DisplayAndPositionMakeParentButton;
            HSelectorManager.Instance.EndedPlacingActionPoint += HideMakeParentButton;

            GameManagerH.Instance.OnGameStateChanged += OnGameStateChanged;

            RegisterMakeParentButtonEvent();
        }

        private void OnGameStateChanged(object sender, HololensGameStateEventArgs args)
        {
            if (args.Data == GameManagerH.GameStateEnum.ProjectEditor && transform.GetComponent<StatefulInteractable>() is StatefulInteractable interactable)
            {
                interactable.customReticle = AddActionReticle;
            }
        }

        private void RegisterMakeParentButtonEvent()
        {
            var button = transform.Find("MakeParentButton");
            button?.GetComponentInChildren<StatefulInteractable>()?.OnClicked.AddListener(() => HSelectorManager.Instance.RegisterParentOfActionPoint(this));
        }

        private void HideMakeParentButton(object sender, EventArgs e)
        {
            var button = transform.Find("MakeParentButton");
            if (button is null)
            {
                return;
            }
            button.gameObject.SetActive(false);
        }

        private void DisplayAndPositionMakeParentButton(object sender, EventArgs e)
        {
            var button = transform.Find("MakeParentButton");
            if (button is null)
            {
                return;
            }
            button.gameObject.SetActive(true);

            UI3DHelper.PlaceOnClosestCollisionPointInMiddle(this, Camera.main.transform.position, button.transform);
        }

        public virtual void UpdateObjectName(string newUserId)
        {
            Data.Name = newUserId;
        }

        protected virtual void Update()
        {
            if (ActionObjectMetadata != null && ActionObjectMetadata.HasPose && gameObject.transform.hasChanged)
            {
                transform.hasChanged = false;
            }
        }

        public virtual void ActionObjectUpdate(IO.Swagger.Model.SceneObject actionObjectSwagger)
        {
            if (Data != null & Data.Name != actionObjectSwagger.Name)
                UpdateObjectName(actionObjectSwagger.Name);
            Data = actionObjectSwagger;
            foreach (IO.Swagger.Model.Parameter p in Data.Parameters)
            {

                if (!ObjectParameters.ContainsKey(p.Name))
                {
                    if (TryGetParameterMetadata(p.Name, out ParameterMeta parameterMeta))
                    {
                        ObjectParameters[p.Name] = new Base.Parameter(parameterMeta, p.Value);
                    }
                    else
                    {
                        Debug.LogError("Failed to load metadata for parameter " + p.Name);
                        HNotificationManager.Instance.ShowNotification("Critical error + Failed to load parameter's metadata.");
                        return;
                    }

                }
                else
                {
                    ObjectParameters[p.Name].Value = p.Value;
                }

            }
            Show();
            //TODO: update all action points and actions.. ?

            // update position and rotation based on received data from swagger
            //if (visibility)
            //    Show();
            //else
            //    Hide();


        }

        public void ResetPosition()
        {
            transform.localPosition = GetScenePosition();
            transform.localRotation = GetSceneOrientation();
        }

        public bool TryGetParameter(string id, out IO.Swagger.Model.Parameter parameter)
        {
            foreach (IO.Swagger.Model.Parameter p in Data.Parameters)
            {
                if (p.Name == id)
                {
                    parameter = p;
                    return true;
                }
            }
            parameter = null;
            return false;
        }

        public bool TryGetParameterMetadata(string id, out IO.Swagger.Model.ParameterMeta parameterMeta)
        {
            foreach (IO.Swagger.Model.ParameterMeta p in ActionObjectMetadata.Settings)
            {
                if (p.Name == id)
                {
                    parameterMeta = p;
                    return true;
                }
            }
            parameterMeta = null;
            return false;
        }

        public abstract Vector3 GetScenePosition();

        public abstract void SetScenePosition(Vector3 position);

        public abstract Quaternion GetSceneOrientation();

        public abstract void SetSceneOrientation(Quaternion orientation);

        public void SetWorldPosition(Vector3 position)
        {
            Data.Pose.Position = DataHelper.Vector3ToPosition(position);
        }

        public Vector3 GetWorldPosition()
        {
            return DataHelper.PositionToVector3(Data.Pose.Position);
        }
        public void SetWorldOrientation(Quaternion orientation)
        {
            Data.Pose.Orientation = DataHelper.QuaternionToOrientation(orientation);
        }

        public Quaternion GetWorldOrientation()
        {
            return DataHelper.OrientationToQuaternion(Data.Pose.Orientation);
        }

        public string GetProviderName()
        {
            return Data.Name;
        }


        public ActionMetadataH GetActionMetadata(string action_id)
        {
            if (ActionObjectMetadata.ActionsLoaded)
            {
                if (ActionObjectMetadata.ActionsMetadata.TryGetValue(action_id, out ActionMetadataH actionMetadata))
                {
                    return actionMetadata;
                }
                else
                {
                    throw new Exception("Metadata not found");
                }
            }
            return null; //TODO: throw exception
        }


        public bool IsRobot()
        {
            return ActionObjectMetadata.Robot;
        }

        public bool IsCamera()
        {
            return ActionObjectMetadata.Camera;
        }

        public virtual void DeleteActionObject()
        {
            // Remove all actions of this action point
            RemoveActionPoints();

            // Remove this ActionObject reference from the scene ActionObject list
            SceneManagerH.Instance.ActionObjects.Remove(this.Data.Id);

            HSelectorManager.Instance.StartedPlacingActionPoint -= DisplayAndPositionMakeParentButton;
            HSelectorManager.Instance.EndedPlacingActionPoint -= HideMakeParentButton;
            GameManagerH.Instance.OnGameStateChanged -= OnGameStateChanged;

            DestroyObject();
            Destroy(gameObject);

        }

        public void DestroyObject()
        {
            GameManagerH.Instance.OnOpenSceneEditor -= RegisterTransformationEvents;
            // LockingEventsCache.Instance.OnObjectLockingEvent -= OnObjectLockingEvent;
        }

        public void RemoveActionPoints()
        {
            // Remove all action points of this action object
            List<Base.ActionPoint> actionPoints = GetActionPoints();
            foreach (Base.ActionPoint actionPoint in actionPoints)
            {
                actionPoint.DeleteAP();
            }
        }


        public virtual void SetVisibility(float value, bool forceShaderChange = false)
        {
            visibility = value;
        }

        public float GetVisibility()
        {
            return visibility;
        }

        public abstract void Show();

        public abstract void Hide();

        public abstract void SetInteractivity(bool interactive);


        public virtual void ActivateForGizmo(string layer)
        {
            gameObject.layer = LayerMask.NameToLayer(layer);
        }

        public string GetProviderId()
        {
            return Data.Id;
        }

        public abstract void UpdateModel();

        //TODO: is this working?
        public List<Base.ActionPoint> GetActionPoints()
        {
            List<Base.ActionPoint> actionPoints = new List<Base.ActionPoint>();
            /*   foreach (ActionPoint actionPoint in ProjectManager.Instance.ActionPoints.Values) {
                   if (actionPoint.Data.Parent == Data.Id) {
                       actionPoints.Add(actionPoint);
                   }
               }*/
            return actionPoints;
        }

        public override string GetName()
        {
            return Data.Name;
        }


        public bool IsActionObject()
        {
            return true;
        }

        public Hololens.ActionObjectH GetActionObject()
        {
            return this;
        }


        public Transform GetTransform()
        {
            return transform;
        }

        public string GetProviderType()
        {
            return Data.Type;
        }

        public GameObject GetGameObject()
        {
            return gameObject;
        }

        public override string GetId()
        {
            return Data.Id;
        }

        public async override Task<RequestResult> Movable()
        {
            if (!ActionObjectMetadata.HasPose)
                return new RequestResult(false, "Selected action object has no pose");
            else if (GameManagerH.Instance.GetGameState() != GameManagerH.GameStateEnum.SceneEditor)
            {
                return new RequestResult(false, "Action object could be moved only in scene editor");
            }
            else
            {
                return new RequestResult(true);
            }
        }

        public abstract void CreateModel(IO.Swagger.Model.CollisionModels customCollisionModels = null);
        public abstract GameObject GetModelCopy();

        public IO.Swagger.Model.Pose GetPose()
        {
            if (ActionObjectMetadata.HasPose)
                return new IO.Swagger.Model.Pose(position: DataHelper.Vector3ToPosition(TransformConvertor.UnityToROS(transform.localPosition)),
                    orientation: DataHelper.QuaternionToOrientation(TransformConvertor.UnityToROS(transform.localRotation)));
            else
                return new IO.Swagger.Model.Pose(new IO.Swagger.Model.Position(), new IO.Swagger.Model.Orientation());
        }
        public async override Task Rename(string name)
        {
            try
            {
                await WebSocketManagerH.Instance.RenameObject(GetId(), name);
                //   Notifications.Instance.ShowToastMessage("Action object renamed");
            }
            catch (RequestFailedException e)
            {
                //   Notifications.Instance.ShowNotification("Failed to rename action object", e.Message);
                throw;
            }
        }
        public async override Task<RequestResult> Removable()
        {
            if (GameManagerH.Instance.GetGameState() != GameManagerH.GameStateEnum.SceneEditor)
            {
                return new RequestResult(false, "Action object could be removed only in scene editor");
            }
            else if (SceneManagerH.Instance.SceneStarted)
            {
                return new RequestResult(false, "Scene online");
            }
            else
            {
                IO.Swagger.Model.RemoveFromSceneResponse response = await WebSocketManagerH.Instance.RemoveFromScene(GetId(), false, true);
                if (response.Result)
                    return new RequestResult(true);
                else
                    return new RequestResult(false, response.Messages[0]);
            }
        }


        public async override void Remove()
        {
            IO.Swagger.Model.RemoveFromSceneResponse response =
            await WebSocketManagerH.Instance.RemoveFromScene(GetId(), false, false);
            if (!response.Result)
            {
                HNotificationManager.Instance.ShowNotification("Failed to remove object " + GetName() + " " + response.Messages[0]);
                return;
            }
        }

        public Transform GetSpawnPoint()
        {
            return transform;
        }

        protected void SetupManipulationComponents(bool allowScale = false)
        {
            this.allowScale = allowScale;

            var om = gameObject.AddComponent(typeof(ObjectManipulator)) as ObjectManipulator;
            om.AllowedManipulations = TransformFlags.None;
            om.firstHoverEntered.AddListener(HoverEntered);
            om.lastHoverExited.AddListener(HoverExited);

            var bc = gameObject.AddComponent(typeof(BoundsControl)) as BoundsControl;
            bc.EnabledHandles = HandleType.None;
            bc.RotateAnchor = RotateAnchorType.ObjectOrigin;
            bc.BoundsVisualsPrefab = BoundsControlBoxPrefab;
            bc.HandlesActive = true;
            bc.ToggleHandlesOnClick = false;
            bc.Interactable = om;
            bc.BoundsOverride = Visual.transform;
            bc.OverrideBounds = true;
            bc.enabled = false;

            var bcah = gameObject.AddComponent(typeof(BoundsControlAdditionalHandler)) as BoundsControlAdditionalHandler;

            var outline = Visual.AddComponent(typeof(MeshOutlineHierarchy)) as MeshOutlineHierarchy;
            outline.OutlineMaterial = OutlineMaterial;
            outline.enabled = false;

            InteractionObjectCollider.SetActive(false);

            if (GameManagerH.Instance.GetGameState() == GameManagerH.GameStateEnum.SceneEditor)
            {
                RegisterTransformationEvents();
            }
            else
            {
                GameManagerH.Instance.OnOpenSceneEditor += RegisterTransformationEvents;
            }

            if (GameManagerH.Instance.GetGameState() == GameManagerH.GameStateEnum.ProjectEditor)
            {
                transform.GetComponent<StatefulInteractable>().customReticle = AddActionReticle;
            }

            transform.GetComponent<StatefulInteractable>().OnClicked.AddListener(() => HSelectorManager.Instance.OnObjectInteraction(this));
            transform.GetComponent<StatefulInteractable>().RegisterOnLongHover(() => HSelectorManager.Instance.OnObjectSelected(this));

        }

        public virtual void HoverExited(HoverExitEventArgs arg0)
        {
            transform.GetComponentInChildren<MeshOutlineHierarchy>().enabled = false;
            SetVisibility(0.7f);
        }

        public virtual void HoverEntered(HoverEnterEventArgs arg0)
        {
            transform.GetComponentInChildren<MeshOutlineHierarchy>().enabled = true;
            SetVisibility(0.7f);
        }

        private void RegisterTransformationEvents(object sender, EventArgs e) => RegisterTransformationEvents();

        internal void RegisterTransformationEvents()
        {
            Debug.Log("Registered for transform events");
            var boundsControl = transform.GetComponent<BoundsControl>();
            var objectManipulator = transform.GetComponent<ObjectManipulator>();
            var boundsControlAdditionalHandler = transform.GetComponent<BoundsControlAdditionalHandler>();

            boundsControlAdditionalHandler.EnableBoundsControlAfterInteraction = true;

            // Handle axis gizmo
            objectManipulator.lastSelectExited.AddListener((_) => transform.GetComponent<TransformGizmoHandler>()?.ShowGizmo());
            objectManipulator.firstSelectEntered.AddListener((_) => transform.GetComponent<TransformGizmoHandler>()?.HideLastGizmo());

            // Register start and stop transforms which will handle locks and calls to servers
            foreach (var gizmoInteractable in transform.GetComponentsInChildren<StatefulInteractable>(true))
            {
                gizmoInteractable.firstSelectEntered.AddListener(StartTransform);
                gizmoInteractable.lastSelectExited.AddListener(EndTransform);
            }
        }

        public async void EndTransform(SelectExitEventArgs arg0)
        {
            if (IsLockedByMe && GameManagerH.Instance.GetGameState() == GameManagerH.GameStateEnum.SceneEditor)
            {
                if (arg0.interactable is BoundsHandleInteractable boundsHandleInteractable && boundsHandleInteractable.HandleType == HandleType.Scale)
                {
                    await UploadNewScaleAsync();
                }
                else
                {
                    UndoManager.Instance.AddUndoRecord(new ActionObjectUpdateUndoRecord()
                    {
                        ActionObject = this,
                        NewPosition = transform.localPosition,
                        NewRotation = transform.localRotation,
                        InitialPosition = initialPosition,
                        InitialRotation = initialRotation
                    });

                    await UploadNewPositionAsync();
                }

                await WriteUnlock();
            }
        }

        public async Task UploadNewScaleAsync()
        {
            if (this is CollisionObjectH collisionObject)
            {
                try
                {
                    ObjectModel objectModel = ActionObjectMetadata.ObjectModel;
                    Vector3 transformedScale = TransformConvertor.UnityToROSScale(collisionObject.Model.transform.lossyScale);

                    switch (objectModel.Type)
                    {
                        case IO.Swagger.Model.ObjectModel.TypeEnum.Box:
                            objectModel.Box.SizeX = (decimal)transformedScale.x;
                            objectModel.Box.SizeY = (decimal)transformedScale.y;
                            objectModel.Box.SizeZ = (decimal)transformedScale.z;
                            break;
                        case IO.Swagger.Model.ObjectModel.TypeEnum.Cylinder:
                            objectModel.Cylinder.Radius = (decimal)transformedScale.x;
                            objectModel.Cylinder.Height = (decimal)transformedScale.z;
                            break;
                        case IO.Swagger.Model.ObjectModel.TypeEnum.Sphere:
                            objectModel.Sphere.Radius = (decimal)transformedScale.x;
                            break;
                    }
                    await WebSocketManagerH.Instance.UpdateObjectModel(ActionObjectMetadata.Type, objectModel);

                }
                catch (RequestFailedException e)
                {
                    Debug.Log("Failed to update size of collision object   " + e.Message);
                }
            }
        }

        public async Task UploadNewPositionAsync()
        {
            await WebSocketManagerH.Instance.UpdateActionObjectPose(this.GetId(),
                new IO.Swagger.Model.Pose(
                    position: DataHelper.Vector3ToPosition(TransformConvertor.UnityToROS(GameManagerH.Instance.Scene.transform.InverseTransformPoint(transform.position))),
                    orientation: DataHelper.QuaternionToOrientation(TransformConvertor.UnityToROS(Quaternion.Inverse(GameManagerH.Instance.Scene.transform.rotation) * this.transform.rotation)))
                );
        }

        public async void StartTransform(SelectEnterEventArgs arg0)
        {
            if (GameManagerH.Instance.GetGameState() == GameManagerH.GameStateEnum.SceneEditor && await WriteLock(true))
            {
                initialPosition = transform.localPosition;
                initialRotation = transform.localRotation;

                EnableManipulation();
            }
            else
            {
                DisableManipulation();
                // TODO probably notify user MAYBE just sound
            }
        }

        private void DisableManipulation()
        {
            var boundsControl = transform.GetComponent<BoundsControl>();
            boundsControl.EnabledHandles = HandleType.None;

            foreach (var objectManipulator in transform.GetComponentsInChildren<ObjectManipulator>())
            {
                objectManipulator.AllowedManipulations = TransformFlags.None;
            }
        }

        private void EnableManipulation()
        {
            var boundsControl = transform.GetComponent<BoundsControl>();
            boundsControl.EnabledHandles = allowScale ? (HandleType.Scale | HandleType.Rotation) : HandleType.Rotation;

            foreach (var objectManipulator in transform.GetComponentsInChildren<ObjectManipulator>())
            {
                objectManipulator.AllowedManipulations = TransformFlags.Move;
            }
        }

        internal async void UploadNewParametersAsync(List<Base.Parameter> editedParameters)
        {
            if (await WriteLock(true))
            {
                try
                {
                    List<IO.Swagger.Model.Parameter> SwaggerParameters = editedParameters.Select(parameter => new IO.Swagger.Model.Parameter(parameter.Name, parameter.Type, parameter.Value)).ToList();
                    await WebSocketManagerH.Instance.UpdateObjectParameters(GetId(), SwaggerParameters, false);
                }
                catch (RequestFailedException e)
                {
                    Debug.Log("Failed to upload action parameter");
                }

                await WriteUnlock();
            }
            
        }
    }
}
