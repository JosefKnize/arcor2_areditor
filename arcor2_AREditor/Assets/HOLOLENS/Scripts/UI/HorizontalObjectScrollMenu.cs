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

    private float maxOffset = 0;

    public void UpdateCollection()
    {
        var i = 0;
        foreach (Transform child in transform)
        {
            child.transform.localPosition = new Vector3(i * 0.25f, 0, 0);
            i++;

            var source = child.GetComponent<ObjectManipulator>();
            source.HostTransform = transform;
        }

        maxOffset = transform.childCount * -0.25f;
    }
}
