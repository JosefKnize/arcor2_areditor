using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Base;
using IO.Swagger.Model;
using UnityEngine;
using Hololens;


public class HAPOrientation : HInteractiveObject
{
     public HActionPoint ActionPoint;
    public string OrientationId;

    [SerializeField]
    private MeshRenderer renderer;


    public void SetOrientation(IO.Swagger.Model.Orientation orientation) {
        transform.localRotation = TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(orientation));
    }

    public override string GetName() {
        return ActionPoint.GetNamedOrientation(OrientationId).Name;
    }

    public override string GetId() {
        return OrientationId;
    }


    public async Task<bool> OpenDetailMenu() {
        if (await ActionPoint.ShowOrientationDetailMenu(OrientationId)) {
            return true;
        }
        return false;
    }

    public async override Task<RequestResult> Movable() {
        return new RequestResult(false, "Orientation could not be moved");
    }

    public override void StartManipulation() {
        throw new System.NotImplementedException();
    }

   public async override Task<RequestResult> Removable() {
        try {
            await WebSocketManagerH.Instance.RemoveActionPointOrientation(OrientationId, true);
            return new RequestResult(true);
        } catch (RequestFailedException ex) {
            return new RequestResult(false, ex.Message);
        }
    }

    public async override void Remove() {
        try {
            await WebSocketManagerH.Instance.RemoveActionPointOrientation(OrientationId, false);
        } catch (RequestFailedException ex) {
            Notifications.Instance.ShowNotification("Failed to remove orientation", ex.Message);
        }//
    }

    public async override Task Rename(string name) {
        try {
            await WebSocketManagerH.Instance.RenameActionPointOrientation(GetId(), name);
        //    Notifications.Instance.ShowToastMessage("Orientation renamed");
        } catch (RequestFailedException e) {
          //  Notifications.Instance.ShowNotification("Failed to rename orientation", e.Message);
            throw;
        }
    }

    public override string GetObjectTypeName() {
        return "Orientation";
    }

    public override void UpdateColor() {

            
    }

    public HInteractiveObject GetParentObject() {
        return ActionPoint;
    }

    public override void DestroyObject() {
        base.DestroyObject();
        Destroy(gameObject);
    }


    public override void EnableVisual(bool enable) {
        throw new System.NotImplementedException();
    }
}
