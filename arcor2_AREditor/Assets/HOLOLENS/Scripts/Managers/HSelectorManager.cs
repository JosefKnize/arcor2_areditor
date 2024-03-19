using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Base;
using System.Threading.Tasks;
using Hololens;
using UnityEngine.Events;
using System;
using LunarConsolePluginInternal;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.UX.Deprecated;
using RosSharp.Urdf;
using IO.Swagger.Model;
using RosSharp.RosBridgeClient;
using static System.Collections.Specialized.BitVector32;

public class HSelectorManager : Singleton<HSelectorManager>
{
    public GameObject ConfirmDialog;
    public GameObject RenameDialog;


    private HAction Output;
    private string placedActionId;
    private IActionProviderH placedActionProvider;


    public event StartedPlacingActionPointEvent StartedPlacingActionPoint;
    public delegate void StartedPlacingActionPointEvent(object sender, EventArgs e);

    public event EndedPlacingActionPointEvent EndedPlacingActionPoint;
    public delegate void EndedPlacingActionPointEvent(object sender, EventArgs e);


    private HInteractiveObject selectedObject;
    public HInteractiveObject SelectedObject
    {
        get { return selectedObject; }
        set
        {
            selectedObject = value;
            if (selectedObject != null)
            {
                if (selectedObject is ActionObjectH actionObject && GameManagerH.Instance.GetGameState() == GameManagerH.GameStateEnum.ProjectEditor)
                {
                    return; // Don't show near object menu on robots in project editor
                }
                else if (selectedObject is HStartEndAction)
                {
                    return;
                }

                NearObjectMenuManager.Instance.Display(selectedObject);
            }
        }
    }

    public SelectorState selectorState;
    private IActionPointParentH parentOfPlacedActionPoint;

    public enum SelectorState
    {
        Normal,
        PlacingAction,
        MakingConnection,
        WaitingForReleaseAfterPlacingAP,
    }

    private void Update()
    {
        // Processing long hovered object from update since the call that adds them to list is from different thread and couldn't manipulate UI
        if (LongHoveredObjects.FirstOrDefault() is HInteractiveObject interactive)
        {
            LongHoveredObjects.Remove(interactive);
            SelectedObject = interactive;
        }
    }

    public void renameClicked(bool removeOnCancel, UnityAction confirmCallback = null, bool keepObjectLocked = false)
    {
        //if (selectedObject is null)
        //    return;

        //if (removeOnCancel)
        //    RenameDialog.Open(selectedObject, true, keepObjectLocked, () => selectedObject.Remove(), confirmCallback);
        //else
        //    RenameDialog.Open(selectedObject, false, keepObjectLocked, null, confirmCallback);
        //// RenameDialog.Open();
    }

    public async void DuplicateObjectClicked()
    {
        switch (SelectedObject)
        {
            case ActionObjectH actionObject:
                if (GameManagerH.Instance.GetGameState() == GameManagerH.GameStateEnum.SceneEditor)
                {
                    List<IO.Swagger.Model.Parameter> parameters = new List<IO.Swagger.Model.Parameter>();
                    foreach (Base.Parameter p in actionObject.ObjectParameters.Values)
                    {
                        parameters.Add(DataHelper.ActionParameterToParameter(p));
                    }
                    string newName = SceneManagerH.Instance.GetFreeAOName(actionObject.GetName());
                    await WebSocketManagerH.Instance.AddObjectToScene(newName,
                        actionObject.ActionObjectMetadata.Type, new IO.Swagger.Model.Pose(
                            orientation: DataHelper.QuaternionToOrientation(TransformConvertor.UnityToROS(actionObject.transform.localRotation)),
                            position: DataHelper.Vector3ToPosition(TransformConvertor.UnityToROS(actionObject.transform.localPosition))), parameters);
                }
                break;
            case HAction action:
                string newActionName = HProjectManager.Instance.GetFreeActionName(action.GetName() + "_copy");
                await WebSocketManagerH.Instance.AddAction(action.ActionPoint.GetId(), action.Parameters.Values.Cast<ActionParameter>().ToList(), Base.Action.BuildActionType(
                action.ActionProvider.GetProviderId(), action.Metadata.Name), newActionName, action.Metadata.GetFlows(newActionName));
                break;
            case HActionPoint actionPoint:
                string newActionPointName = HProjectManager.Instance.GetFreeAPName(actionPoint.GetName() + "_copy");
                WebSocketManagerH.Instance.CopyActionPoint(selectedObject.GetId(), null, selectedObject.GetName(), (_, _) => { });
                break;
        }

    }

    public void OpenRemoveObjectDialog()
    {
        var dialog = ConfirmDialog.GetComponent<HConfirmDialog>();
        dialog.Open($"Remove {SelectedObject.GetObjectTypeName().ToLower()}",
                    $"Do you want to remove {SelectedObject.GetName()}",
                    () => RemoveSelectedObject(),
                    () => { });
    }

    private void RemoveSelectedObject()
    {
        SelectedObject.Remove();
        SelectedObject = null;
        NearObjectMenuManager.Instance.Hide();
    }

    #region Adding action

    private void DisplayActionPickerMenu(ActionObjectH actionObject)
    {
        HActionPickerMenu.Instance.PopulateMenu(actionObject);
        HActionPickerMenu.Instance.ShowMenu(actionObject);
    }

    internal void PlacedActionPicked(string actionId, IActionProviderH actionProvider)
    {
        placedActionId = actionId;
        placedActionProvider = actionProvider;

        selectorState = SelectorState.PlacingAction;
        StartedPlacingActionPoint?.Invoke(this, new EventArgs());
        AddActionPointHandler.Instance.registerHandlers();

    }

    internal async Task CreateActionPointAndPlaceAction(Vector3 position)
    {
        selectorState = SelectorState.WaitingForReleaseAfterPlacingAP;
        var parent = parentOfPlacedActionPoint;
        string name = parent is null ? HProjectManager.Instance.GetFreeAPName("global") : HProjectManager.Instance.GetFreeAPName(parent.GetName());

        HProjectManager.Instance.OnActionPointAddedToScene += AddDefaultOrientation;
        HProjectManager.Instance.OnActionPointOrientation += FinishPlacingActionPoint;

        bool result = await HProjectManager.Instance.AddActionPoint(name, parent);

        if (!result)
        {
            // TODO notify
            return;
        }

        parentOfPlacedActionPoint = null;
    }

    private void FinishPlacingActionPoint(object sender, HololensActionPointOrientationEventArgs args)
    {
        CreateAction(placedActionId, placedActionProvider, args.ActionPoint);
        args.ActionPoint.WriteUnlock();
        HProjectManager.Instance.OnActionPointOrientation -= FinishPlacingActionPoint;
    }

    private void AddDefaultOrientation(object sender, HololensActionPointEventArgs args)
    {
        args.ActionPoint.WriteLock(true);
        Orientation orientation = new Orientation(1, 0, 180, 0);
        WebSocketManagerH.Instance.AddActionPointOrientation(args.ActionPoint.Data.Id, orientation, args.ActionPoint.GetFreeOrientationName());
        HProjectManager.Instance.OnActionPointAddedToScene -= AddDefaultOrientation;
    }

    private void CreateActionPointAndPlaceActionUsingRobot(HRobotEE ee)
    {
        HProjectManager.Instance.OnActionPointOrientation += FinishPlacingActionPoint;
        WebSocketManagerH.Instance.AddActionPointUsingRobot(HProjectManager.Instance.GetFreeAPName("global"), ee.EEId, ee.Robot.GetId(), false, (_, _) => { }, null);
    }

    public async void CreateAction(string action_id, IActionProviderH actionProvider, HActionPoint actionPoint, string newName = null)
    {
        ActionMetadataH actionMetadata = actionProvider.GetActionMetadata(action_id);
        List<IO.Swagger.Model.ActionParameter> parameters = new List<IO.Swagger.Model.ActionParameter>();

        foreach (ParameterMetadataH parameterMetadata in actionMetadata.ParametersMetadata.Values.ToList())
        {
            string value = InitActionValue(actionPoint, parameterMetadata);
            parameters.Add(new ActionParameter(name: parameterMetadata.Name, value: value, type: parameterMetadata.Type));
        }

        string newActionName = string.IsNullOrEmpty(newName)
            ? HProjectManager.Instance.GetFreeActionName(actionMetadata.Name)
            : HProjectManager.Instance.GetFreeActionName(newName);

        try
        {
            await WebSocketManagerH.Instance.AddAction(actionPoint.GetId(), parameters, Base.Action.BuildActionType(
                    actionProvider.GetProviderId(), actionMetadata.Name), newActionName, actionMetadata.GetFlows(newActionName));
        }
        catch (Base.RequestFailedException e)
        {
            // Base.Notifications.Instance.ShowNotification("Failed to add action", e.Message);
        }
    }

    private string InitActionValue(HActionPoint actionPoint, ParameterMetadataH actionParameterMetadata)
    {
        object value = null;
        switch (actionParameterMetadata.Type)
        {
            case "string":
                value = actionParameterMetadata.GetDefaultValue<string>();
                break;
            case "integer":
                value = actionParameterMetadata.GetDefaultValue<int>();
                break;
            case "double":
                value = actionParameterMetadata.GetDefaultValue<double>();
                break;
            case "boolean":
                value = actionParameterMetadata.GetDefaultValue<bool>();
                break;
            case "pose":
                try
                {
                    value = actionPoint.GetFirstOrientation().Id;
                }
                catch (ItemNotFoundException)
                {
                    // there is no orientation on this action point
                    try
                    {
                        value = actionPoint.GetFirstOrientationFromDescendants().Id;
                    }
                    catch (ItemNotFoundException)
                    {
                        try
                        {
                            value = HProjectManager.Instance.GetAnyNamedOrientation().Id;
                        }
                        catch (ItemNotFoundException)
                        {

                        }
                    }
                }
                break;
            case "joints":
                try
                {
                    value = actionPoint.GetFirstJoints().Id;
                }
                catch (ItemNotFoundException)
                {
                    // there are no valid joints on this action point
                    try
                    {
                        value = actionPoint.GetFirstJointsFromDescendants().Id;
                    }
                    catch (ItemNotFoundException)
                    {
                        try
                        {
                            value = HProjectManager.Instance.GetAnyJoints().Id;
                        }
                        catch (ItemNotFoundException)
                        {
                            // there are no valid joints in the scene
                        }
                    }

                }
                break;
            case "string_enum":
                value = ((ARServer.Models.StringEnumParameterExtra)actionParameterMetadata.ParameterExtra).AllowedValues.First();
                break;
            case "integer_enum":
                value = ((ARServer.Models.IntegerEnumParameterExtra)actionParameterMetadata.ParameterExtra).AllowedValues.First().ToString();
                break;
        }
        if (value != null)
        {
            value = JsonConvert.SerializeObject(value);
        }

        return (string)value;
    }

    internal void RegisterParentOfActionPoint(IActionPointParentH actionObjectH)
    {
        parentOfPlacedActionPoint = actionObjectH;
    }

    #endregion

    #region Connection
    private void StartMakingConnection(HAction action)
    {
        if (action is HEndAction)
        {
            // Maybe Notify
            return;
        }

        selectorState = SelectorState.MakingConnection;
        action.AddConnection();
        Output = action;
        AddActionPointHandler.Instance.registerHandlers(false);
    }

    private void FinishMakingConnection(HAction action)
    {
        if (action is HStartAction)
        {
            // Maybe Notify
            return;
        }

        selectorState = SelectorState.Normal; // TODO maybe start making connection from this
        Output.GetOtherAction(action);
        AddActionPointHandler.Instance.unregisterHandlers();
    }

    private void CancelMakingConnection()
    {
        selectorState = SelectorState.Normal;
        HConnectionManagerArcoro.Instance.DestroyConnectionToMouse();
        AddActionPointHandler.Instance.unregisterHandlers();
    }
    #endregion

    #region Selection inputs

    public void OnSelectObject(HInteractiveObject newSelectedObject)
    {
        SelectedObject = newSelectedObject;
        if (GameManagerH.Instance.GetGameState() == GameManagerH.GameStateEnum.ProjectEditor)
        {
            if (selectorState == SelectorState.Normal)
            {
                switch (newSelectedObject)
                {
                    case ActionObjectH actionObjectH:
                        DisplayActionPickerMenu(actionObjectH);
                        break;
                    case HAction action3D:
                        StartMakingConnection(action3D);
                        break;
                }
            }
            else if (selectorState == SelectorState.MakingConnection && newSelectedObject is HAction action)
            {
                FinishMakingConnection(action);
            }
        }
    }

    internal async Task OnEmptyClick(Vector3 position)
    {
        switch (selectorState)
        {
            case SelectorState.Normal:
                break;
            case SelectorState.MakingConnection:
                CancelMakingConnection();
                break;
            case SelectorState.PlacingAction:
                await CreateActionPointAndPlaceAction(position);
                break;
        }
    }

    internal async Task OnSelectObjectFromActionPointsHandler(Vector3 position, HInteractiveObject interactive)
    {
        if (selectorState == SelectorState.PlacingAction)
        {
            if (interactive is HAction3D action)
            {
                CreateAction(placedActionId, placedActionProvider, action.ActionPoint);
                selectorState = SelectorState.WaitingForReleaseAfterPlacingAP;
            }
            else if (interactive is HActionPoint3D actionPoint)
            {
                RegisterParentOfActionPoint(actionPoint);
            }
            else if (interactive is HRobotEE ee)
            {
                CreateActionPointAndPlaceActionUsingRobot(ee);
            }
            else
            {
                await CreateActionPointAndPlaceAction(position);
            }
        }
    }

    internal void OnRelease()
    {
        if (selectorState == SelectorState.WaitingForReleaseAfterPlacingAP)
        {
            selectorState = SelectorState.Normal;
            AddActionPointHandler.Instance.unregisterHandlers();
            EndedPlacingActionPoint?.Invoke(this, new EventArgs());
        }
    }

    private List<HInteractiveObject> LongHoveredObjects = new List<HInteractiveObject>();
    internal void OnObjectLongHover(HInteractiveObject HInteractiveObject)
    {
        LongHoveredObjects.Add(HInteractiveObject);
    }

    #endregion
}

public static class UI3DHelper
{
    public static void PlaceOnClosestCollisionPoint(HInteractiveObject interactiveObject, Vector3 source, Transform movedObject)
    {
        var collidCubeGameObject = interactiveObject.transform.Find("Visual").Find("CollidCube");
        var collider = collidCubeGameObject.GetComponent<BoxCollider>();
        var wasColliderActive = collider.transform.gameObject.activeSelf;
        collider.transform.gameObject.SetActive(true);
        var closestPoint = collider.ClosestPoint(source);
        collider.transform.gameObject.SetActive(wasColliderActive);
        movedObject.position = closestPoint;
    }

    public static void PlaceOnClosestCollisionPointInMiddle(HInteractiveObject interactiveObject, Vector3 source, Transform movedObject)
    {
        var collidCubeGameObject = interactiveObject.transform.Find("Visual").Find("CollidCube");
        var collider = collidCubeGameObject.GetComponent<BoxCollider>();
        var wasColliderActive = collider.transform.gameObject.activeSelf;
        collider.transform.gameObject.SetActive(true);

        source.y = collider.bounds.center.y;

        var closestPoint = collider.ClosestPoint(source);
        collider.transform.gameObject.SetActive(wasColliderActive);
        movedObject.position = closestPoint;
    }

    public static void PlaceOnClosestCollisionPointAtBottom(HInteractiveObject interactiveObject, Vector3 source, Transform movedObject)
    {
        var collidCubeGameObject = interactiveObject.transform.Find("Visual").Find("CollidCube");
        var collider = collidCubeGameObject.GetComponent<BoxCollider>();
        var wasColliderActive = collider.transform.gameObject.activeSelf;
        collider.transform.gameObject.SetActive(true);

        //source.y = collider.bounds.center.y;
        source.y = collider.bounds.min.y;

        var closestPoint = collider.ClosestPoint(source);
        collider.transform.gameObject.SetActive(wasColliderActive);
        movedObject.position = closestPoint;
    }
}
