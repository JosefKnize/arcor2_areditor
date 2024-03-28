using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Base;
using IO.Swagger.Model;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hololens;
using Base;

using UnityEngine.XR.Interaction.Toolkit;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit;
using RequestResult = Base.RequestResult;

//[RequireComponent(typeof(OutlineOnClick))]
//[RequireComponent(typeof(Target))]
public class HAction3D : HAction
{
    public Renderer Visual;

    public GameObject interactObject;

    private Color32 colorDefault = new Color32(229, 215, 68, 255);
    private Color32 colorRunnning = new Color32(255, 0, 255, 255);


    public override void Init(IO.Swagger.Model.Action projectAction, ActionMetadataH metadata,HActionPoint ap, IActionProviderH actionProvider) {
        base.Init(projectAction, metadata, ap, actionProvider);
        transform.GetComponent<StatefulInteractable>().RegisterOnShortClick(() => HSelectorManager.Instance.OnObjectInteraction(this, false));
        //transform.GetComponent<StatefulInteractable>().RegisterOnLongSelect(() => HSelectorManager.Instance.OnObjectSelected(this));
        transform.GetComponent<StatefulInteractable>().RegisterOnLongHover(() => HSelectorManager.Instance.OnObjectSelected(this));
    }

    protected override void Start() {
        base.Start();
    }

    private void LateUpdate() {
        UpdateRotation();
    }

    private void OnEnable() {
    }

    private void OnDisable() {
        
    }

    private void OnProjectStop(object sender, System.EventArgs e) {
        StopAction();
    }

    public override void RunAction() {
        Visual.material.color = colorRunnning;
        foreach (IO.Swagger.Model.ActionParameter p in Data.Parameters) {
            if (p.Type == "pose") {
                string orientationId = JsonConvert.DeserializeObject<string>(p.Value);
            }
        }
    }

    public override void StopAction() {
        if (Visual != null) {
            Visual.material.color = colorDefault;
        }
        foreach (IO.Swagger.Model.ActionParameter p in Data.Parameters) {
            if (p.Type == "pose") {
                string orientationId = JsonConvert.DeserializeObject<string>(p.Value);
            }
        }
    }

    public override void UpdateName(string newName) {
        base.UpdateName(newName);
        NameText.text = newName;
    }

    public override void ActionUpdateBaseData(IO.Swagger.Model.BareAction aData = null) {
        base.ActionUpdateBaseData(aData);
        NameText.text = aData.Name;
    }

    public bool CheckClick() {

        return true;
      

    }

    public override void UpdateColor() {
        

        foreach (Material material in Visual.materials)
            if (Enabled && !(IsLocked && !IsLockedByMe))
                material.color = new Color(0.9f, 0.84f, 0.27f);
            else
                material.color = Color.gray;
    }

    public override string GetName() {
        return Data.Name;
    }


    public override void StartManipulation() {
        throw new NotImplementedException();
    }

    public async override Task<RequestResult> Removable() {
        if (GameManagerH.Instance.GetGameState() != GameManagerH.GameStateEnum.ProjectEditor)
            return new RequestResult(false, "Action could only be removed in project editor");
        else {
            try {
                await WebSocketManagerH.Instance.RemoveAction(GetId(), true);
                return new RequestResult(true);
            } catch (RequestFailedException ex) {
                return new RequestResult(false, ex.Message);
            }
        }
    }

    public async override void Remove() {
        try {
            await this.Input.RemoveAllLogicItemsAsync();
            await this.Output.RemoveAllLogicItemsAsync();
            await WebSocketManagerH.Instance.RemoveAction(GetId(), false);
        } catch (RequestFailedException ex) {
           // Notifications.Instance.ShowNotification("Failed to remove action " + GetName(), ex.Message);
        }
    }

    public async override Task Rename(string newName) {
        try {
            await WebSocketManagerH.Instance.RenameAction(GetId(), newName);
          //  Notifications.Instance.ShowToastMessage("Action renamed");
        } catch (RequestFailedException e) {
           // Notifications.Instance.ShowNotification("Failed to rename action", e.Message);
            throw;
        }
    }

    public override string GetObjectTypeName() {
        return "Action";
    }

    public override void OnObjectLocked(string owner) {
        base.OnObjectLocked(owner);
  /*      if (owner != LandingScreen.Instance.GetUsername()) {
            NameText.text = GetLockedText();
        }*/
    }

    public override void OnObjectUnlocked() {
        base.OnObjectUnlocked();
        NameText.text = GetName();
    }

    public HInteractiveObject GetParentObject() {
        return ActionPoint;
    }


    public override void EnableVisual(bool enable) {
        throw new NotImplementedException();
    }
}
