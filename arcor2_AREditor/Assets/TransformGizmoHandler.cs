using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformGizmoHandler : MonoBehaviour
{
    public GameObject TransformGizmo;
    public BoxCollider CollidCube;
    public static GameObject LastActivatedGizmo;

    void Update()
    {
        TransformGizmo.transform.position = CollidCube.bounds.center;
        TransformGizmo.transform.localPosition += CollidCube.transform.localScale / 2;
        TransformGizmo.transform.localPosition += new Vector3(0.01f, -0.005f, 0.01f);
        TransformGizmo.transform.localPosition -= new Vector3(0, CollidCube.transform.localScale.y, 0);
    }

    public void HideLastGizmo()
    {
        LastActivatedGizmo?.SetActive(false);
    }

    public void ShowGizmo()
    {
        TransformGizmo.SetActive(true);
        LastActivatedGizmo = TransformGizmo;
    }
}
