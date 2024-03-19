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
    public bool WaitingForReleaseLeft;
    public bool WaitingForReleaseRight;

    private bool previewActionPoint = false;
    private bool processPinch = true;

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

        if (processPinch && aggregator.TryGetPinchProgress(hand, out bool isReadyToPinch, out bool isPinching, out float pinchAmount))
        {
            var interactor = hand.IsLeftHand() ? LeftRayInteractor : RightRayInteractor;
            ProcessPinch(isPinching, pinchAmount, interactor, hand);
        }
    }

    private async void ProcessPinch(bool isPinching, float pinchAmount, MRTKRayInteractor interactor, XRNode hand)
    {
        if ((hand == XRNode.LeftHand && isPinching && WaitingForReleaseLeft) || (hand == XRNode.RightHand && isPinching && WaitingForReleaseRight))
        {
            return;
        }

        if (!isPinching)
        {
            if (hand == XRNode.LeftHand)
            {
                WaitingForReleaseLeft = false;
            }
            else
            {
                WaitingForReleaseRight = false;
            }
        }

        if (!isPinching && fingersPressed)
        {
            fingersPressed = false;
            HSelectorManager.Instance.OnRelease();
        }

        if (!isPinching || (isPinching && pinchAmount > 0.1f))
        {
            return;
        }

        fingersPressed = true;

        // Check if collision with parent object if yes remember parent and continue
        var validTargets = new List<IXRInteractable>();
        interactor.GetValidTargets(validTargets);

        if (validTargets.Count > 0 && validTargets[0].transform.GetComponent<HInteractiveObject>() is HInteractiveObject interactive)
        {
            await HSelectorManager.Instance.OnSelectObjectFromActionPointsHandler(GhostActionPoint.transform.position, interactive);
        }
        else if (validTargets.Count > 0 && validTargets[0].transform.parent.GetComponent<HInteractiveObject>() is HInteractiveObject interactiveParent)
        {
            await HSelectorManager.Instance.OnSelectObjectFromActionPointsHandler(GhostActionPoint.transform.position, interactiveParent);
        }
        else if (validTargets.Count > 0 && validTargets[0].transform.tag == "MakeParentButton")
        {
            // Ignore the button which will register as parent by itself
        }
        else
        {
            await HSelectorManager.Instance.OnEmptyClick(GhostActionPoint.transform.position);
        }
    }

    public void registerHandlers(bool previewActionPoint = true)
    {
        this.previewActionPoint = previewActionPoint;
        processPinch = true;
        WaitingForReleaseLeft = true;
        WaitingForReleaseRight = true;
    }

    public void unregisterHandlers()
    {
        if (previewActionPoint)
        {
            GhostActionPoint.GetComponent<Renderer>().enabled = false;
        }
        previewActionPoint = false;
        processPinch = false;
    }
}
