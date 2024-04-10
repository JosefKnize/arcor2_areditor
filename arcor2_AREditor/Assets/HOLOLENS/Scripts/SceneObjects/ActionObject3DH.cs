using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Base;
using Hololens;
using IO.Swagger.Model;
using TriLibCore;
using System;
using TriLibCore.General;
using MixedReality.Toolkit.SpatialManipulation;
using MixedReality.Toolkit;
using UnityEngine.XR.Interaction.Toolkit;
using System.Threading.Tasks;
using Unity.XR.CoreUtils;

public class ActionObject3DH : ActionObjectH
{
    public TextMeshPro ActionObjectName;
    public GameObject Model;

    private bool transparent = false;
    public GameObject CubePrefab;
    private Shader standardShader;
    private Shader transparentShader;
    private bool isGreyColorForced;

    private List<Renderer> aoRenderers = new List<Renderer>();

    public GameObject CylinderPrefab, SpherePrefab, PlanePrefab;


    protected override void Start()
    {
        base.Start();
        transform.localScale = new Vector3(1f, 1f, 1f);
    }

    public override Vector3 GetScenePosition()
    {
        return TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(Data.Pose.Position));
    }

    public override void SetScenePosition(Vector3 position)
    {
        Data.Pose.Position = DataHelper.Vector3ToPosition(TransformConvertor.UnityToROS(position));
    }

    public override Quaternion GetSceneOrientation()
    {
        return TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(Data.Pose.Orientation));
    }

    public override void SetSceneOrientation(Quaternion orientation)
    {
        Data.Pose.Orientation = DataHelper.QuaternionToOrientation(TransformConvertor.UnityToROS(orientation));
    }

    public override void UpdateObjectName(string newUserId)
    {
        base.UpdateObjectName(newUserId);
        ActionObjectName.text = newUserId;
    }

    public override void ActionObjectUpdate(IO.Swagger.Model.SceneObject actionObjectSwagger)
    {
        base.ActionObjectUpdate(actionObjectSwagger);
        ActionObjectName.text = actionObjectSwagger.Name;
        ResetPosition();
    }

    public static Dictionary<string, Color> GlobalOriginalColors = new Dictionary<string, Color>();
    public override void SetVisibility(float value, bool forceShaderChange = false)
    {
        foreach (Renderer renderer in aoRenderers)
        {
            string materialName = renderer.material.name;
            if (!GlobalOriginalColors.TryGetValue(materialName, out Color color))
            {
                GlobalOriginalColors[materialName] = renderer.material.color;
                color = renderer.material.color;
            }

            float h, s, v;
            Color.RGBToHSV(color, out h, out s, out v);
            color = Color.HSVToRGB(h, s, v * 0.4f);
            renderer.material.color = color;
        }
    }

    public override void Show()
    {
        Debug.Assert(Model != null);
        //SetVisibility(1);
    }

    public override void Hide()
    {
        Debug.Assert(Model != null);
        //SetVisibility(0);
    }

    public override void SetInteractivity(bool interactivity)
    {
        Debug.Assert(Model != null && ActionObjectMetadata.HasPose);
        //Model.GetComponent<Collider>().enabled = interactivity;
        if (ActionObjectMetadata.ObjectModel != null &&
            ActionObjectMetadata.ObjectModel.Type == ObjectModel.TypeEnum.Mesh)
        {
            foreach (var col in Colliders)
            {
                col.enabled = interactivity;
            }
        }
        else
        {
            Collider.enabled = interactivity;
        }
    }

    public override void ActivateForGizmo(string layer)
    {
        base.ActivateForGizmo(layer);
        Model.layer = LayerMask.NameToLayer(layer);
    }

    public override void CreateModel(CollisionModels customCollisionModels = null)
    {
        if (ActionObjectMetadata.ObjectModel == null || ActionObjectMetadata.ObjectModel.Type == IO.Swagger.Model.ObjectModel.TypeEnum.None)
        {
            Model = Instantiate(CubePrefab, Visual.transform);
            Model.transform.localScale = new Vector3(0.05f, 0.01f, 0.05f);
        }
        else
        {
            switch (ActionObjectMetadata.ObjectModel.Type)
            {
                case IO.Swagger.Model.ObjectModel.TypeEnum.Box:
                    Model = Instantiate(CubePrefab, Visual.transform);

                    if (customCollisionModels == null)
                    {
                        Model.transform.localScale = TransformConvertor.ROSToUnityScale(new Vector3((float)ActionObjectMetadata.ObjectModel.Box.SizeX, (float)ActionObjectMetadata.ObjectModel.Box.SizeY, (float)ActionObjectMetadata.ObjectModel.Box.SizeZ));
                    }
                    else
                    {
                        foreach (IO.Swagger.Model.Box box in customCollisionModels.Boxes)
                        {
                            if (box.Id == ActionObjectMetadata.Type)
                            {
                                Model.transform.localScale = TransformConvertor.ROSToUnityScale(new Vector3((float)box.SizeX, (float)box.SizeY, (float)box.SizeZ));
                                break;
                            }
                        }
                    }
                    break;
                case IO.Swagger.Model.ObjectModel.TypeEnum.Cylinder:
                    Model = Instantiate(CylinderPrefab, Visual.transform);
                    if (customCollisionModels == null)
                    {
                        Model.transform.localScale = new Vector3((float)ActionObjectMetadata.ObjectModel.Cylinder.Radius, (float)ActionObjectMetadata.ObjectModel.Cylinder.Height / 2, (float)ActionObjectMetadata.ObjectModel.Cylinder.Radius);
                    }
                    else
                    {
                        foreach (IO.Swagger.Model.Cylinder cylinder in customCollisionModels.Cylinders)
                        {
                            if (cylinder.Id == ActionObjectMetadata.Type)
                            {
                                Model.transform.localScale = new Vector3((float)cylinder.Radius, (float)cylinder.Height, (float)cylinder.Radius);
                                break;
                            }
                        }
                    }
                    break;
                case IO.Swagger.Model.ObjectModel.TypeEnum.Sphere:
                    Model = Instantiate(SpherePrefab, Visual.transform);
                    if (customCollisionModels == null)
                    {
                        Model.transform.localScale = new Vector3((float)ActionObjectMetadata.ObjectModel.Sphere.Radius, (float)ActionObjectMetadata.ObjectModel.Sphere.Radius, (float)ActionObjectMetadata.ObjectModel.Sphere.Radius);
                    }
                    else
                    {
                        foreach (IO.Swagger.Model.Sphere sphere in customCollisionModels.Spheres)
                        {
                            if (sphere.Id == ActionObjectMetadata.Type)
                            {
                                Model.transform.localScale = new Vector3((float)sphere.Radius, (float)sphere.Radius, (float)sphere.Radius);
                                break;
                            }
                        }
                    }
                    break;
                case ObjectModel.TypeEnum.Mesh:
                    MeshImporterH.Instance.OnMeshImported += OnModelLoaded;
                    MeshImporterH.Instance.LoadModel(ActionObjectMetadata.ObjectModel.Mesh, GetId());

                    Model = Instantiate(CubePrefab, Visual.transform);
                    Model.transform.localScale = new Vector3(0.05f, 0.01f, 0.05f);
                    break;
                default:
                    Model = Instantiate(CubePrefab, Visual.transform);
                    Model.transform.localScale = new Vector3(0.05f, 0.01f, 0.05f);
                    break;
            }
        }

        Vector3 vec = Model.transform.localScale;
        InteractionObjectCollider.transform.localScale = new Vector3(vec.x + 0.01f, vec.y + 0.01f, vec.z + 0.01f);
        InteractionObjectCollider.transform.position = Model.transform.position;

        if (ActionObjectMetadata.ObjectModel.Type != ObjectModel.TypeEnum.Mesh)
        {
            SetupManipulationComponents(true);
        }

        gameObject.GetComponent<BindParentToChildH>().ChildToBind = Model;
        Collider = Model.GetComponent<Collider>();
        Colliders.Add(Collider);

        aoRenderers.Clear();
        aoRenderers.AddRange(Model.GetComponentsInChildren<Renderer>(true));
    }

    public override GameObject GetModelCopy()
    {
        GameObject model = Instantiate(Model);
        model.transform.localScale = Model.transform.localScale;
        return model;
    }

    /// <summary>
    /// For meshes...
    /// </summary>
    /// <param name="assetLoaderContext"></param>
    public void OnModelLoaded(object sender, ImportedMeshEventArgsH args)
    {
        if (args.Name != this.GetId())
            return;

        if (Model != null)
        {
            Model.SetActive(false);
            Destroy(Model);
        }

        Model = args.RootGameObject;
        gameObject.GetComponent<BindParentToChildH>().ChildToBind = Model;

        foreach (Renderer child in Model.GetComponentsInChildren<Renderer>(true))
        {
            child.gameObject.AddComponent<MeshCollider>();
        }

        aoRenderers.Clear();
        Colliders.Clear();
        aoRenderers.AddRange(Model.GetComponentsInChildren<Renderer>(true));
        Colliders.AddRange(Model.GetComponentsInChildren<MeshCollider>(true));

        // HACK: Collision cube must be calculated after rescaling model, but before applying rotation
        // Also it must be child element when rotation is applied to model
        InteractionObjectCollider.transform.parent = Model.transform;
        Model.gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
        Model.gameObject.transform.parent = Visual.transform;
        Model.gameObject.transform.localPosition = Vector3.zero;
        CalculateTotalBoundingBox();
        Model.gameObject.transform.localRotation = Quaternion.identity;
        InteractionObjectCollider.transform.parent = Visual.transform;

        SetupManipulationComponents();

        SetVisibility(visibility, forceShaderChange: true);

        MeshImporterH.Instance.OnMeshImported -= OnModelLoaded;
    }

    private void CalculateTotalBoundingBox()
    {
        if (aoRenderers.Count > 0)
        {
            Bounds totalBounds = new Bounds();

            totalBounds = aoRenderers[0].bounds;

            foreach (Renderer renderer in aoRenderers)
            {
                totalBounds.Encapsulate(renderer.bounds);
            }

            var rotatedSize = transform.InverseTransformVector(totalBounds.size);
            rotatedSize = rotatedSize.Abs();
            InteractionObjectCollider.transform.localScale = rotatedSize;
            InteractionObjectCollider.transform.position = totalBounds.center;
            InteractionObjectCollider.transform.localRotation = Quaternion.identity;
        }
    }


    /// <summary>
    /// For meshes...
    /// </summary>
    /// <param name="obj"></param>
    private void OnModelLoadError(IContextualizedError obj)
    {
        //   Notifications.Instance.ShowNotification("Unable to show mesh " + this.GetName(), obj.GetInnerException().Message);
    }


    public override void UpdateColor()
    {
        if (Blocklisted)
            return;

        SetGrey(IsLockedByOtherUser || isGreyColorForced);
    }

    /// <summary>
    /// Sets grey color of AO model (indicates that model is not in position of real robot)
    /// </summary>
    /// <param name="grey">True for setting grey, false for standard state.</param>
    public void SetGrey(bool grey, bool force = false)
    {
        return;
    }

    public override void Enable(bool enable, bool putOnBlocklist = false, bool removeFromBlocklist = false)
    {
        bool prevBlocklisted = Blocklisted;
        base.Enable(enable, putOnBlocklist, removeFromBlocklist);
        if (prevBlocklisted != Blocklisted)
        {
            if (Blocklisted)
            {
                SetVisibility(0);
            }
            else
            {
                SetVisibility((float)0.5);
            }
        }
    }


    public override string GetObjectTypeName()
    {
        return "Action object";
    }

    public override void OnObjectLocked(string owner)
    {
        base.OnObjectLocked(owner);
        if (owner != HLandingManager.Instance.GetUsername())
            ActionObjectName.text = GetLockedText();
    }

    public override void OnObjectUnlocked()
    {
        base.OnObjectUnlocked();
        ActionObjectName.text = GetName();
    }

    public override void StartManipulation()
    {
        throw new NotImplementedException();
    }

    public override void EnableVisual(bool enable)
    {
        Visual.SetActive(enable);
        InteractionObjectCollider.SetActive(enable);
    }

    public override void UpdateModel()
    {
        if (ActionObjectMetadata.ObjectModel == null)
            return;
        Vector3? dimensions = null;
        switch (ActionObjectMetadata.ObjectModel.Type)
        {
            case ObjectModel.TypeEnum.Box:
                dimensions = TransformConvertor.ROSToUnityScale(new Vector3((float)ActionObjectMetadata.ObjectModel.Box.SizeX, (float)ActionObjectMetadata.ObjectModel.Box.SizeY, (float)ActionObjectMetadata.ObjectModel.Box.SizeZ));
                break;
            case ObjectModel.TypeEnum.Sphere:
                dimensions = TransformConvertor.ROSToUnityScale(new Vector3((float)ActionObjectMetadata.ObjectModel.Sphere.Radius, (float)ActionObjectMetadata.ObjectModel.Sphere.Radius, (float)ActionObjectMetadata.ObjectModel.Sphere.Radius));
                break;
            case ObjectModel.TypeEnum.Cylinder:
                dimensions = TransformConvertor.ROSToUnityScale(new Vector3((float)ActionObjectMetadata.ObjectModel.Cylinder.Radius, (float)ActionObjectMetadata.ObjectModel.Cylinder.Radius, (float)ActionObjectMetadata.ObjectModel.Cylinder.Height));
                break;

        }

        if (dimensions != null)
        {
            transform.localScale = Vector3.one;
            Model.transform.localScale = new Vector3(dimensions.Value.x, dimensions.Value.y, dimensions.Value.z);
        }

        Vector3 vec = Model.transform.localScale;
        InteractionObjectCollider.transform.localScale = new Vector3(vec.x + 0.01f, vec.y + 0.01f, vec.z + 0.01f);
        InteractionObjectCollider.transform.position = Model.transform.position;
    }
}
