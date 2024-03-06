using UnityEngine;
using Base;
using System.Threading.Tasks;
using Hololens;
using RosSharp.Urdf;
using UnityEngine.Animations;
using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine.XR.Interaction.Toolkit;

public class HTransformMenu : Singleton<HTransformMenu>
{

    HInteractiveObject InteractiveObject;
    GameObject model;

    GameObject transformDetail;

    /// <summary>
    /// Prefab for transform gizmo
    /// </summary>
    public GameObject GizmoPrefab;
   
}
