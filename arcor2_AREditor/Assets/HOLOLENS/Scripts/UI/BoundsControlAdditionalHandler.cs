using Hololens;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class BoundsControlAdditionalHandler : MonoBehaviour
{
    static BoundsControl lastActivatedBoundsControl;
    static GameObject lastActivatedCollisionCube;

    private Transform boundingBoxInstance;
    private BoundsControl boundsControl;

    public bool EnableBoundsControlAfterInteraction = false;
    public bool EnableTransformGizmoAfterInteraction = false;

    // Start is called before the first frame update
    void Start()
    {
        boundingBoxInstance = transform.Find("BoundingBoxWithTraditionalHandles(Clone)");
        boundsControl = transform.GetComponent<BoundsControl>();

        var interactable = transform.GetComponent<StatefulInteractable>();
        interactable.lastSelectExited.AddListener(EnableBoundsControl);
        interactable.firstSelectEntered.AddListener(DisableLastBoundsControl);
    }

    // Update is called once per frame
    void Update()
    {
        boundingBoxInstance?.gameObject.SetActive(boundsControl.enabled);
    }

    private void DisableLastBoundsControl(SelectEnterEventArgs arg0)
    {
        if (EnableBoundsControlAfterInteraction && lastActivatedBoundsControl is not null && lastActivatedBoundsControl != boundsControl)
        {
            lastActivatedCollisionCube.SetActive(false);
            lastActivatedBoundsControl.enabled = false;
        }
    }

    private void EnableBoundsControl(SelectExitEventArgs arg0)
    {
        if (EnableBoundsControlAfterInteraction)
        {
            lastActivatedCollisionCube = transform.GetComponent<ActionObjectH>().InteractionObjectCollider;
            lastActivatedCollisionCube.SetActive(true);
            boundsControl.enabled = true;
            lastActivatedBoundsControl = boundsControl;
            boundsControl.RecomputeBounds();
        }
    }
}
