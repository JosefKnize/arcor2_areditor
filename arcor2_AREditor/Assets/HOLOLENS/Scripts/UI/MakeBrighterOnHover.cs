using MixedReality.Toolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class MakeBrighterOnHover : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var interactable = transform.GetComponent<StatefulInteractable>();
        interactable.firstHoverEntered.AddListener(MakeThisBrighter);
        interactable.lastHoverExited.AddListener(MakeThisDimmer);
    }

    private void MakeThisDimmer(HoverExitEventArgs arg0)
    {
        var renderers = transform.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.material.color = renderer.material.color / 1.5f;
        }
    }

    private void MakeThisBrighter(HoverEnterEventArgs arg0)
    {
        var renderers = transform.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.material.color = renderer.material.color * 1.5f;
        }
    }
}
