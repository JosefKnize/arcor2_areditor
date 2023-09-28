using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using Base;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Utilities;
using Hololens;

public class AddActionPointHandler : Singleton<AddActionPointHandler>, IMixedRealityPointerHandler,  IMixedRealityFocusChangedHandler
{

    private bool isFocused = false;
    private bool registered = false;

    public GameObject actionPointPrefab;

    void Update() {
        if(registered){
            if(HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Any, out MixedRealityPose pose)){
                actionPointPrefab.GetComponent<Renderer>().enabled = true;
                actionPointPrefab.transform.position = pose.Position;
            }
            else {
                actionPointPrefab.GetComponent<Renderer>().enabled = false;

            }
           
        }
    }

  

    public void registerHandlers(bool register = true){
        registered = register;
        CoreServices.InputSystem?.RegisterHandler<IMixedRealityPointerHandler>(this); 
        CoreServices.InputSystem?.RegisterHandler< IMixedRealityFocusChangedHandler>(this); 
    }

    public void unregisterHandlers(){
        if(registered){
            actionPointPrefab.GetComponent<Renderer>().enabled = false;
        }
        registered = false;
         
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealityPointerHandler>(this); 
        CoreServices.InputSystem?.UnregisterHandler< IMixedRealityFocusChangedHandler>(this); 

    }
    void IMixedRealityPointerHandler.OnPointerUp(MixedRealityPointerEventData eventData){}
    void IMixedRealityPointerHandler.OnPointerDown(MixedRealityPointerEventData eventData){}
    void IMixedRealityPointerHandler.OnPointerDragged(MixedRealityPointerEventData eventData){}
    void  IMixedRealityFocusChangedHandler.OnBeforeFocusChange(FocusEventData eventData){}
    void  IMixedRealityFocusChangedHandler.OnFocusChanged(FocusEventData eventData ) {

        if(eventData.NewFocusedObject == null){
            isFocused = false;
        }
        else {
            isFocused = true;
        } 
    }


    void IMixedRealityPointerHandler.OnPointerClicked(MixedRealityPointerEventData eventData)
    {
        GameObject targetObject = eventData.Pointer.Result.CurrentPointerTarget;
        if(targetObject == null || targetObject.name.Contains("SpatialMesh")){
            HSelectorManager.Instance.OnSelectObject(null);
        }
      /*  if(!isFocused){
            HSelectorManager.Instance.OnSelectObject(null);
        }*/
    }

}
