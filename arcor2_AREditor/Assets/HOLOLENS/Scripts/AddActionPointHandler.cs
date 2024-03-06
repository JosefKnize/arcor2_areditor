using UnityEngine;
using Base;
using UnityEngine.InputSystem;
using MixedReality.Toolkit;
using UnityEngine.XR;
using MixedReality.Toolkit.Subsystems;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;
using MixedReality.Toolkit.Input;
using System;

public class AddActionPointHandler : Singleton<AddActionPointHandler>
{
    public float ReleaseLimit = 0.0053f;
    public float test = 0;

    private bool isFocused = false;
    private bool previewActionPoint = false;

    public GameObject GhostActionPoint;

    public InputActionReference LeftHandReference;
    public InputActionReference RightHandReference;

    public MRTKRayInteractor LeftRayInteractor;
    public MRTKRayInteractor RightRayInteractor;
    private bool fingersPressed;

    void Update()
    {
        ProcessHand(XRNode.LeftHand);
        ProcessHand(XRNode.RightHand);
    }

    void ProcessHand(XRNode hand)
    {
        var aggregator = XRSubsystemHelpers.GetFirstRunningSubsystem<HandsAggregatorSubsystem>();
        if (aggregator.TryGetJoint(TrackedHandJoint.IndexTip, hand, out HandJointPose pose))
        {
            GhostActionPoint.GetComponent<Renderer>().enabled = previewActionPoint;
            GhostActionPoint.transform.position = pose.Position;
        }
        else
        {
            GhostActionPoint.GetComponent<Renderer>().enabled = false;
        }
    }

    public void registerHandlers(bool previewActionPoint = true)
    {
        this.previewActionPoint = previewActionPoint;
        LeftHandReference.action.performed += LeftHandActionPerformed;
        RightHandReference.action.performed += RightHandActionPerformed;
    }

    public void unregisterHandlers()
    {
        if (previewActionPoint)
        {
            GhostActionPoint.GetComponent<Renderer>().enabled = false;
        }
        previewActionPoint = false;

        LeftHandReference.action.performed -= LeftHandActionPerformed;
        RightHandReference.action.performed -= RightHandActionPerformed;
    }

    private void LeftHandActionPerformed(InputAction.CallbackContext obj) => HandActionPerformed(obj, LeftRayInteractor);

    private void RightHandActionPerformed(InputAction.CallbackContext obj) => HandActionPerformed(obj, RightRayInteractor);

    private void HandActionPerformed(InputAction.CallbackContext obj, MRTKRayInteractor interactor)
    {
        test = obj.ReadValue<float>();

        if (obj.ReadValue<float>() < ReleaseLimit && fingersPressed)
        {
            fingersPressed = false;
            HSelectorManager.Instance.OnRelease();
        }

        if (obj.ReadValue<float>() < 0.95f)
        {
            return;
        }

        fingersPressed = true;

        // Check if collision with parent object if yes remember parent and continue
        var validTargets = new List<IXRInteractable>();
        interactor.GetValidTargets(validTargets);

        if (validTargets.Count > 0 && validTargets[0].transform.GetComponent<HInteractiveObject>() is HInteractiveObject interactive)
        {
            HSelectorManager.Instance.OnSelectObjectFromActionPointsHandler(GhostActionPoint.transform.position, interactive);
        }
        else if(validTargets.Count > 0 && validTargets[0].transform.tag == "MakeParentButton")
        {
            // Ignore the button which will register as parent by itself
        }
        else
        {
            HSelectorManager.Instance.OnEmptyClick(GhostActionPoint.transform.position);
        }

        // Pøidání akèního bodu (not ActionPointParentingButton) - Tady potøebuju zamezit normální interakci
        // Pøidání akèního bodu (ActionPointParentingButton)
        // zrušení spoje (empty)
    }
}
