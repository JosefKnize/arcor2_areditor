using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using static UnityEngine.GraphicsBuffer;

public class HorizontalObjectScrollMenu : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        UpdateCollection();
    }

    private Vector3 initialPosition;
    private Transform manipulatedCube;
    private float maxOffset = 0;

    // Update is called once per frame
    void Update()
    {
        if (manipulatedCube is not null)
        {
            var difference = manipulatedCube.localPosition - initialPosition;
            var newPosition = transform.localPosition + difference;
            if (newPosition.x < maxOffset)
            {
                //transform.localPosition = new Vector3(maxOffset, 0, 0);
                transform.localPosition = newPosition;
            }
            else
            {
                transform.localPosition = newPosition;
            }
        }
    }

    public void UpdateCollection()
    {
        var i = 0;
        foreach (Transform child in transform)
        {
            child.transform.localPosition = new Vector3(i * 0.25f, 0, 0);
            i++;

            var source = child.GetComponent<ObjectManipulator>();
            source.firstSelectEntered.AddListener(RegisterForManipulationUpdatesPropagation);
            source.lastSelectExited.AddListener(UnregisterForManipulationUpdatesPropagation);
        }

        maxOffset = transform.childCount * -0.25f;
    }

    private void UnregisterForManipulationUpdatesPropagation(SelectExitEventArgs arg0)
    {
        if (manipulatedCube is not null)
        {
            manipulatedCube.localPosition = initialPosition;
            manipulatedCube = null;
        }
    }

    private void RegisterForManipulationUpdatesPropagation(SelectEnterEventArgs arg0)
    {
        manipulatedCube = arg0.interactableObject.transform;
        initialPosition = manipulatedCube.transform.localPosition;
    }
}
