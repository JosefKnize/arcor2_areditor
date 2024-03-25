using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using OrbCreationExtensions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GizmoCorner
{
    public Vector3 Position { get; internal set; }
    public float xDirection { get; internal set; }
    public float yDirection { get; internal set; }
    public float DistanceFromCamera { get; internal set; }
    public float RotationX { get; internal set; }
    public float RotationY { get; internal set; }
}

public class TransformGizmoHandler : MonoBehaviour
{
    public GameObject TransformGizmo;
    public BoxCollider CollidCube;

    public float Xrotation = 0;
    public float Yrotation = 0;


    private bool isGizmoManipulated;
    public bool IsGizmoManipulated
    {
        get => isGizmoManipulated;
        set
        {
            isGizmoManipulated = value;
            TransformGizmo.GetComponent<HGizmo>().SetInitialPosition(TransformGizmo.transform.position);
        }
    }

    public static GameObject LastActivatedGizmo;
    private List<GizmoCorner> Corners;

    private void Start()
    {
        Corners = new List<GizmoCorner>()
        {
            new GizmoCorner(){ xDirection =  1f, yDirection =  1f, RotationX = 180f , RotationY = 90f },
            new GizmoCorner(){ xDirection =  1f, yDirection = -1f, RotationX = 180f , RotationY = 270f },
            new GizmoCorner(){ xDirection = -1f, yDirection =  1f, RotationX = 0f,    RotationY = 90f},
            new GizmoCorner(){ xDirection = -1f, yDirection = -1f, RotationX = 0f,    RotationY = 270f},
        };
    }

    void Update()
    {
        if (!TransformGizmo.activeSelf || IsGizmoManipulated)
        {
            return;
        }

        foreach (var corner in Corners)
        {
            var offsetVector = new Vector3((CollidCube.transform.localScale.x / 2 + 0.02f) * corner.xDirection,
                                           (CollidCube.transform.localScale.y / 2) * -1 + 0.02f,
                                           (CollidCube.transform.localScale.z / 2 + 0.02f) * corner.yDirection);

            var offsetVectorLocalSpace = TransformGizmo.transform.parent.TransformDirection(offsetVector);
            corner.Position = CollidCube.bounds.center + offsetVectorLocalSpace;
            corner.DistanceFromCamera = Vector3.Distance(corner.Position, Camera.main.transform.position);
        }

        var closestCorner = Corners.First(x => x.DistanceFromCamera == Corners.Min(x => x.DistanceFromCamera));
        TransformGizmo.transform.position = closestCorner.Position;

        // Rotate gizmo accordingly
        //TransformGizmo.transform.localScale = closestCorner.NeedsFlip
        //    ? new Vector3(-1 * System.Math.Abs(TransformGizmo.transform.localScale.x), TransformGizmo.transform.localScale.y, TransformGizmo.transform.localScale.z)
        //    : new Vector3(System.Math.Abs(TransformGizmo.transform.localScale.x), TransformGizmo.transform.localScale.y, TransformGizmo.transform.localScale.z);

        TransformGizmo.transform.Find("X_axis").transform.localRotation = Quaternion.Euler(0, closestCorner.RotationX, 0);
        TransformGizmo.transform.Find("Z_axis").transform.localRotation = Quaternion.Euler(0, closestCorner.RotationY, 0);

        Xrotation = closestCorner.RotationX;
        Yrotation = closestCorner.RotationY;
    }

    public void HideLastGizmo()
    {
        if(LastActivatedGizmo?.activeInHierarchy ?? false)
        {
            LastActivatedGizmo?.SetActive(false);
        }
    }

    public void ShowGizmo()
    {
        TransformGizmo.SetActive(true);
        LastActivatedGizmo = TransformGizmo;
    }
}
