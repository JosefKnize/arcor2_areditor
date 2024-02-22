using System;
using System.Threading.Tasks;
using Base;
using Hololens;
using IO.Swagger.Model;
using UnityEngine;

public class ActionObjectNoPoseH : ActionObjectH
{
    public override void CreateModel(CollisionModels customCollisionModels = null) {
        // no pose object has no model
    }

    public override void EnableVisual(bool enable) {
        throw new NotImplementedException();
    }

    public override GameObject GetModelCopy() {
        return null;
    }

    public override string GetObjectTypeName() {
        return "Action object";
    }

    public override Quaternion GetSceneOrientation() {
        throw new RequestFailedException("This object has no pose");
    }

    public override Vector3 GetScenePosition() {
        throw new RequestFailedException("This object has no pose");
    }

    public override void Hide() {
        throw new NotImplementedException();
    }


    public override void SetInteractivity(bool interactive) {
        
    }

    public override void SetSceneOrientation(Quaternion orientation) {
        throw new RequestFailedException("This object has no pose");
    }

    public override void SetScenePosition(Vector3 position) {
        throw new RequestFailedException("This object has no pose");
    }

    public override void Show() {
        throw new NotImplementedException();
    }

    public override void StartManipulation() {
        throw new RequestFailedException("This object has no pose");
    }

    public override void UpdateColor() {
        //nothing to do here
    }

    public override void UpdateModel() {
        // nothing to do here
    }
}
