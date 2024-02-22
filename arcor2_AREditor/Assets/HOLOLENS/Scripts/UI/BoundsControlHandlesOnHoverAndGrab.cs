using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class BoundsControlHandlesOnHoverAndGrab : MonoBehaviour
{
    [SerializeField]
    [Tooltip("BoundsControl that manages handles")]
    private BoundsControl boundsControl;

    private GameObject hoverBox;

    // Start is called before the first frame update
    void Start()
    {
        hoverBox = new GameObject("HoverBox");
        hoverBox.tag = "HoverOnly";
        hoverBox.transform.parent = transform.parent;

        hoverBox.AddComponent<BoxCollider>();
        UpdateHoverBox();
        var hoverBoxInteractable = hoverBox.AddComponent<MRTKBaseInteractable>();

        hoverBoxInteractable.hoverEntered.AddListener(HoverEntered);
        hoverBoxInteractable.hoverExited.AddListener(HoverExited);
        boundsControl.ManipulationEnded.AddListener(ManipulationEnded);

        var OM = boundsControl.Interactable as ObjectManipulator;
        OM.lastSelectExited.AddListener(ManipulationEnded);
    }

    private void Update()
    {
        if (boundsControl.IsManipulated || (boundsControl.Interactable is ObjectManipulator om && om.isSelected))
        {
            UpdateHoverBox();
        }
    }

    private void HoverEntered(HoverEnterEventArgs arg0)
    {
        boundsControl.HandlesActive = true;
    }

    private void ManipulationEnded(SelectExitEventArgs arg0)
    {
        boundsControl.HandlesActive = false;
        UpdateHoverBox();
    }

    private void HoverExited(HoverExitEventArgs arg0)
    {
        if (!boundsControl.IsManipulated)
        {
            boundsControl.HandlesActive = false;
        }
    }

    public void UpdateHoverBox()
    {
        var boundingBox = transform.Find("BoundingBoxWithTraditionalHandles(Clone)");
        var padding = new Vector3(0.05f, 0.05f, 0.05f);

        hoverBox.transform.localScale = boundingBox.localScale + padding;
        hoverBox.transform.position = boundingBox.position;
        hoverBox.transform.rotation = boundingBox.rotation;
    }
}
