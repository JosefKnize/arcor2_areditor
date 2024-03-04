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

public class HSelectorManager : Singleton<HSelectorManager>
{
    public HConfirmDialog confirmDialog;
    public HRenameDialog renameDialog;
    private HInteractiveObject selectedObject;


    protected List<HInteractiveObject> lockedObjects = new List<HInteractiveObject>();

    private HAction Output;

    private string placedActionId;
    private IActionProviderH placedActionProvider;

    public SelectorState selectorState;

    public enum SelectorState
    {
        Normal,
        PlacingAction,
        MakingConnection,
        WaitingBeforeNextInteraction,
        WaitingForReleaseAfterPlacingAP,
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    public void renameClicked(bool removeOnCancel, UnityAction confirmCallback = null, bool keepObjectLocked = false)
    {

        if (selectedObject is null)
            return;

        if (removeOnCancel)
            renameDialog.Open(selectedObject, true, keepObjectLocked, () => selectedObject.Remove(), confirmCallback);
        else
            renameDialog.Open(selectedObject, false, keepObjectLocked, null, confirmCallback);
        // RenameDialog.Open();
    }

    public void deleteClicked()
    {
        HDeleteActionManager.Instance.Hide();
        if (!(selectedObject is ActionObjectH actionO) || GameManagerH.Instance.GetGameState().Equals(GameManagerH.GameStateEnum.SceneEditor))
        {

            if (selectedObject is HAction action)
            {
                if (!action.Output.AnyConnection())
                {
                    if (action is HAction3D action3D)
                    {
                        deleteObject();
                    }

                }
                else
                {
                    HDeleteActionManager.Instance.Show(action);
                    if (action is HStartAction start)
                    {
                        HDeleteActionManager.Instance.setActiveActionButton(false);
                    }
                }

            }
            else
            {
                deleteObject();
            }
        }
    }

    public void deleteObject()
    {
        confirmDialog.Open($"Remove {selectedObject.GetObjectTypeName().ToLower()}",
          $"Do you want to remove {selectedObject.GetName()}",
          () => RemoveObject(selectedObject),
            null);
    }

    public async void copyObjectClicked()
    {
        if (selectedObject is ActionObjectH actionObject && GameManagerH.Instance.GetGameState().Equals(GameManagerH.GameStateEnum.SceneEditor))
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
    }

    private void RemoveObject(HInteractiveObject obj)
    {
        if (obj is HAction action)
        {
            HDeleteActionManager.Instance.Hide();

        }
        obj.Remove();
        confirmDialog.Close();
    }

    public async Task<bool> LockObject(HInteractiveObject interactiveObject, bool lockTree)
    {
        if (await interactiveObject.WriteLock(lockTree))
        {
            lockedObjects.Add(interactiveObject);
            return true;
        }

        return false;
    }

    public async Task<RequestResult> UnlockAllObjects()
    {

        if (GameManagerH.Instance.ConnectionStatus == GameManagerH.ConnectionStatusEnum.Disconnected)
        {
            lockedObjects.Clear();
            return new RequestResult(true);
        }
        for (int i = lockedObjects.Count - 1; i >= 0; --i)
        {
            if (lockedObjects[i].IsLockedByMe)
            {
                if (!await lockedObjects[i].WriteUnlock())
                {
                    return new RequestResult(false, $"Failed to unlock {lockedObjects[i].GetName()}");
                }
                if (lockedObjects[i] is CollisionObjectH co)
                {
                    await co.WriteUnlockObjectType();
                }

                lockedObjects.RemoveAt(i);
            }
        }
        return new RequestResult(true);
    }


    #region Adding action

    private void DisplayActionPickerMenu(ActionObjectH actionObject)
    {
        HActionPickerMenu.Instance.PopulateMenu(actionObject);
        HActionPickerMenu.Instance.ShowMenu(actionObject);
    }

    internal void ActionSelected(string actionId, IActionProviderH actionProvider)
    {
        placedActionId = actionId;
        placedActionProvider = actionProvider;

        selectorState = SelectorState.PlacingAction;
        AddActionPointHandler.Instance.registerHandlers();
    }

    internal async Task CreateActionPointAndPlaceActionAsync(Vector3 position, IActionPointParentH parent = null)
    {
        string name = parent is null ? HProjectManager.Instance.GetFreeAPName("global") : HProjectManager.Instance.GetFreeAPName(parent.GetName());

        bool result = await HProjectManager.Instance.AddActionPoint(name, parent);
        if (result)
        {
            HActionPoint actionPoint = null;
            HProjectManager.Instance.OnActionPointOrientation += async (sender, args) =>
            {
                CreateAction(placedActionId, placedActionProvider, args.ActionPoint);

                await args.ActionPoint.WriteUnlock();
            };
        }
        else
        {
            // TODO Notify
        }

        selectorState = SelectorState.WaitingForReleaseAfterPlacingAP;
    }


    internal void OnRelease()
    {
        if (selectorState == SelectorState.WaitingForReleaseAfterPlacingAP)
        {
            selectorState = SelectorState.Normal;
            AddActionPointHandler.Instance.unregisterHandlers();
        }
    }

    public async void CreateAction(string action_id, IActionProviderH actionProvider, HActionPoint actionPoint, string newName = null)
    {
        ActionMetadataH actionMetadata = actionProvider.GetActionMetadata(action_id);
        List<IO.Swagger.Model.ActionParameter> parameters = new List<IO.Swagger.Model.ActionParameter>();

        foreach (ParameterMetadataH parameterMetadata in actionMetadata.ParametersMetadata.Values.ToList())
        {
            string value = InitActionValue(actionPoint, parameterMetadata);
            IO.Swagger.Model.ActionParameter ap = new IO.Swagger.Model.ActionParameter(name: parameterMetadata.Name, value: value, type: parameterMetadata.Type);
            parameters.Add(ap);
        }
        string newActionName;

        if (string.IsNullOrEmpty(newName))
            newActionName = HProjectManager.Instance.GetFreeActionName(actionMetadata.Name);
        else
            newActionName = HProjectManager.Instance.GetFreeActionName(newName);

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

    #endregion


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

    public void OnSelectObject(HInteractiveObject selectedObject)
    {
        Debug.Log("Interaction");

        if (GameManagerH.Instance.GetGameState() == GameManagerH.GameStateEnum.ProjectEditor)
        {
            if (selectorState == SelectorState.Normal)
            {
                switch (selectedObject)
                {
                    case ActionObjectH actionObjectH:
                        DisplayActionPickerMenu(actionObjectH);
                        break;
                    case HAction action3D:
                        StartMakingConnection(action3D);
                        break;
                }
            }
            else if (selectorState == SelectorState.MakingConnection && selectedObject is HAction action)
            {
                FinishMakingConnection(action);
            }
        }
    }

    internal void OnEmptyClick(Vector3 position)
    {
        switch (selectorState)
        {
            case SelectorState.Normal:
                break;
            case SelectorState.MakingConnection:
                CancelMakingConnection();
                break;
            case SelectorState.PlacingAction:
                CreateActionPointAndPlaceActionAsync(position);
                break;
        }
    }

    internal void OnSelectObjectFromActionPointsHandler(Vector3 position, HInteractiveObject interactive)
    {
        if (selectorState == SelectorState.PlacingAction)
        {
            // If target is parent button remember parent and don't finish making AP
            // If target is AP remember parent and don't finish making AP
            // If target is action place new action on same AP
            if (interactive is HAction3D action)
            {
                CreateAction(placedActionId, placedActionProvider, action.ActionPoint);
                selectorState = SelectorState.WaitingForReleaseAfterPlacingAP;
            }
            else
            {
                // Otherwise create AP
                CreateActionPointAndPlaceActionAsync(position);
            }
        }
    }
}
