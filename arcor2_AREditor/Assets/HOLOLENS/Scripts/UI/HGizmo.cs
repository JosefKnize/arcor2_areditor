/*
 Author: Simona Hiadlovská
 Amount of changes: 50% changed - Added some logic that was needed because of transform rework.
 Edited by: Josef Kníže
*/

using MixedReality.Toolkit.SpatialManipulation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HGizmo : MonoBehaviour
{
    public enum Axis
    {
        X,
        Y,
        Z,
        NONE
    }
    [SerializeField] private TMPro.TMP_Text XAxisLabel;
    [SerializeField] private TMPro.TMP_Text YAxisLabel;
    [SerializeField] private TMPro.TMP_Text ZAxisLabel;

    private Vector3 initialPosition;

    private void Start()
    {
        string[] axis = new string[] { "X_axis", "Y_axis", "Z_axis" };
        foreach (var ax in axis)
        {
            SetupEvents(ax);
        }
    }

    private void SetupEvents(string ax)
    {
        var axis = transform.Find(ax);
        var manipulator = axis.GetComponent<ObjectManipulator>();

        manipulator.firstHoverEntered.AddListener((arg) => MakeAxisBright(axis));
        //manipulator.IsGrabHovered.OnEntered.AddListener((arg) => MakeAxisBright(axis));

        manipulator.lastHoverExited.AddListener((arg) => MakeAxisDim(axis));
        //manipulator.IsGrabHovered.OnExited.AddListener((arg) => MakeAxisBright(axis));
        
    }

    private void MakeAxisDim(Transform axis)
    {
        var renderers = axis.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers.Take(2))
        {
            renderer.material.color = renderer.material.color / 1.8f;
        }
    }

    private void MakeAxisBright(Transform axis)
    {
        var renderers = axis.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers.Take(2))
        {
            renderer.material.color = 1.8f * renderer.material.color;
        }
    }

    private void Update()
    {
        var delta = transform.InverseTransformDirection(transform.position - initialPosition);
        SetXDelta(delta.x);
        SetYDelta(delta.y);
        SetZDelta(delta.z);
    }

    private string FormatValue(float value)
    {
        if (Mathf.Abs(value) < 0.000099f)
            return $"0cm";
        if (Mathf.Abs(value) < 0.00999f)
            return $"{value * 1000:0.##}mm";
        if (Mathf.Abs(value) < 0.9999f)
            return $"{value * 100:0.##}cm";
        return $"{value:0.###}m";
    }

    public void SetXDelta(float value)
    {
        XAxisLabel.text = $"Δ{FormatValue(value)}";
    }

    public void SetYDelta(float value)
    {
        YAxisLabel.text = $"Δ{FormatValue(value)}";
    }

    public void SetZDelta(float value)
    {
        ZAxisLabel.text = $"Δ{FormatValue(value)}";
    }

    internal void SetInitialPosition(Vector3 position)
    {
        initialPosition = position;
    }
}
