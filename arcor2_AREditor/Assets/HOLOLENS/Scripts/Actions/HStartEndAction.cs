using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hololens;
using System.Threading.Tasks;
using System;
using Base;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;

public abstract class HStartEndAction : HAction
{
    public Renderer Visual;
    public GameObject VisualRoot;

    protected string playerPrefsKey;
    [SerializeField]
  //  protected OutlineOnClick outlineOnClick;
    public GameObject ModelPrefab;
    public GameObject interactObject;


    public virtual void Init(IO.Swagger.Model.Action projectAction, ActionMetadataH metadata, HActionPoint ap, IActionProviderH actionProvider, string actionType) {
        base.Init(projectAction, metadata, ap, actionProvider);

        transform.GetComponentInChildren<StatefulInteractable>().OnClicked.AddListener(() => HSelectorManager.Instance.OnSelectObject(this));


        if (!HProjectManager.Instance.ProjectMeta.HasLogic) {
            Destroy(gameObject);
            return;
        }
        playerPrefsKey = "project/" + HProjectManager.Instance.ProjectMeta.Id + "/" + actionType;

    }

    public void SavePosition() {
        PlayerPrefsHelper.SaveVector3(playerPrefsKey, transform.localPosition);
    }

    public async override Task<RequestResult> Movable() {
        return new RequestResult(true);
    }

    public override void StartManipulation() {
        throw new NotImplementedException();
    }

    public override async Task<bool> WriteUnlock() {
        return true;
    }

 /*   public override async Task<bool> WriteLock(bool lockTree) {
        return true;
    }*/

    protected override void OnObjectLockingEvent(object sender, ObjectLockingEventArgs args) {
        return;
    }

    public override void OnObjectLocked(string owner) {
        return;
    }

    public override void OnObjectUnlocked() {
        return;
    }

    public override string GetName() {
        return Data.Name;
    }    

    public async override Task<RequestResult> Removable() {
        return new RequestResult(false, GetObjectTypeName() + " could not be removed");
    }

    public override void Remove() {
        throw new NotImplementedException();
    }

    public override Task Rename(string name) {
        throw new NotImplementedException();
    }

    public GameObject GetModelCopy() {
        return Instantiate(ModelPrefab);
    }

    public override void EnableVisual(bool enable) {
        VisualRoot.SetActive(enable);
        interactObject.SetActive(enable);
    }

     public void setInterarction(GameObject interactComponents){

        BoxCollider collider = interactComponents.GetComponent<BoxCollider>();
        collider.size = interactObject.transform.localScale;
        collider.center = interactObject.transform.localPosition;

        BoundsControl boundsControl = interactComponents.GetComponent<BoundsControl>();
        ObjectManipulator objectManipulator = interactComponents.GetComponent<ObjectManipulator>();

             
        boundsControl.ScaleLerpTime = 1L;
        boundsControl.RotateLerpTime = 1L;
        objectManipulator.ScaleLerpTime = 1L;
        objectManipulator.RotateLerpTime = 1L;

        //boundsControl.ScaleHandlesConfig.ShowScaleHandles = false;
        //boundsControl.RotationHandlesConfig.ShowHandleForX = false;
        //boundsControl.RotationHandlesConfig.ShowHandleForY = false;
        //boundsControl.RotationHandlesConfig.ShowHandleForZ = false;
        //boundsControl.UpdateBounds();
    }
}
