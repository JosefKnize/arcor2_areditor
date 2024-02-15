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

public class ActionObject3DH : ActionObjectH {
    public TextMeshPro ActionObjectName;
    public GameObject Visual, Model;

    private bool transparent = false;
    public GameObject interactObject;
    public GameObject CubePrefab;
    private Shader standardShader;
    private Shader transparentShader;
    private bool isGreyColorForced;

    private List<Renderer> aoRenderers = new List<Renderer>();

    public GameObject CylinderPrefab, SpherePrefab, PlanePrefab;


    protected override void Start() {
        base.Start();
        transform.localScale = new Vector3(1f, 1f, 1f);

    }


    public override Vector3 GetScenePosition() {
        return TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(Data.Pose.Position));
    }

    public override void SetScenePosition(Vector3 position) {
        Data.Pose.Position = DataHelper.Vector3ToPosition(TransformConvertor.UnityToROS(position));
    }

    public override Quaternion GetSceneOrientation() {
        return TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(Data.Pose.Orientation));
    }

    public override void SetSceneOrientation(Quaternion orientation) {
        Data.Pose.Orientation = DataHelper.QuaternionToOrientation(TransformConvertor.UnityToROS(orientation));
    }

    public override void UpdateObjectName(string newUserId) {
        base.UpdateObjectName(newUserId);
        ActionObjectName.text = newUserId;
    }

    public override void ActionObjectUpdate(IO.Swagger.Model.SceneObject actionObjectSwagger) {
        base.ActionObjectUpdate(actionObjectSwagger);
        ActionObjectName.text = actionObjectSwagger.Name;
        ResetPosition();
    }

    public override void SetVisibility(float value, bool forceShaderChange = false) {
        float normalizedValue = value;
        if (Blocklisted)
            normalizedValue = 0;
        base.SetVisibility(normalizedValue);
        if (standardShader == null) {
            standardShader = Shader.Find("Standard");
        }

        if (transparentShader == null) {
            transparentShader = Shader.Find("Transparent/Diffuse");
        }

        // Set opaque shader
        if (value >= 1) {
            transparent = false;
            foreach (Renderer renderer in aoRenderers) {
                // Object has its outline active, we need to select second material,
                // (first is mask, second is object material, third is outline)
                if (renderer.materials.Length == 3) {
                    renderer.materials[1].shader = standardShader;
                } else {
                    renderer.material.shader = standardShader;
                }
            }
        }
        // Set transparent shader
        else {
            transparent = false;
            if (forceShaderChange || !transparent) {
                foreach (Renderer renderer in aoRenderers) {
                    if (renderer.materials.Length == 3) {
                        renderer.materials[1].shader = transparentShader;
                    } else {
                        renderer.material.shader = transparentShader;
                    }
                }
                transparent = true;
            }
            // set alpha of the material
            foreach (Renderer renderer in aoRenderers) {
                Material mat;
                if (renderer.materials.Length == 3) {
                    mat = renderer.materials[1];
                } else {
                    mat = renderer.material;
                }
                Color color = mat.color;
                color.a = value;
                mat.color = color;
            }
        }
    }

    public override void Show() {
        Debug.Assert(Model != null);
        SetVisibility(1);
    }

    public override void Hide() {
        Debug.Assert(Model != null);
        SetVisibility(0);
    }

    public override void SetInteractivity(bool interactivity) {
        Debug.Assert(Model != null && ActionObjectMetadata.HasPose);
        //Model.GetComponent<Collider>().enabled = interactivity;
        if (ActionObjectMetadata.ObjectModel != null &&
            ActionObjectMetadata.ObjectModel.Type == ObjectModel.TypeEnum.Mesh) {
            foreach (var col in Colliders) {
                col.enabled = interactivity;
            }
        } else {
            Collider.enabled = interactivity;
        }
    }

    public override void ActivateForGizmo(string layer) {
        base.ActivateForGizmo(layer);
        Model.layer = LayerMask.NameToLayer(layer);
    }


    public GameObject getInteractObject() {
        return interactObject;
    }

    public void setInterarction(GameObject interactComponents) {

        BoxCollider collider = interactComponents.GetComponent<BoxCollider>();
        collider.size = getInteractObject().transform.localScale;
        collider.center = getInteractObject().transform.localPosition;

        BoundsControl boundsControl = interactComponents.GetComponent<BoundsControl>();
        ObjectManipulator objectManipulator = interactComponents.GetComponent<ObjectManipulator>();

        if (ActionObjectMetadata.ObjectModel.Type.Equals(ObjectModel.TypeEnum.Mesh)) {
            boundsControl.ScaleLerpTime = 1L;
            objectManipulator.ScaleLerpTime = 1L;

            //boundsControl.ScaleHandlesConfig.ShowScaleHandles = false;
            //boundsControl.RotationHandlesConfig.ShowHandleForX = false;
            ////boundsControl.RotationHandlesConfig.ShowHandleForY = false;
            //boundsControl.RotationHandlesConfig.ShowHandleForZ = false;
        } else {
            boundsControl.ScaleLerpTime = 0.6f;
            objectManipulator.ScaleLerpTime = 1f;

            //boundsControl.ScaleHandlesConfig.ShowScaleHandles = true;
            //boundsControl.RotationHandlesConfig.ShowHandleForX = true;
            //boundsControl.RotationHandlesConfig.ShowHandleForY = true;
            //boundsControl.RotationHandlesConfig.ShowHandleForZ = true;
        }



        //boundsControl.UpdateBounds();
    }


    public override void CreateModel(CollisionModels customCollisionModels = null) {
        if (ActionObjectMetadata.ObjectModel == null || ActionObjectMetadata.ObjectModel.Type == IO.Swagger.Model.ObjectModel.TypeEnum.None) {
            Model = Instantiate(CubePrefab, Visual.transform);
            Model.transform.localScale = new Vector3(0.05f, 0.01f, 0.05f);
        } else {
            switch (ActionObjectMetadata.ObjectModel.Type) {
                case IO.Swagger.Model.ObjectModel.TypeEnum.Box:

                    Model = Instantiate(CubePrefab, Visual.transform);


                    if (customCollisionModels == null) {
                        Model.transform.localScale = TransformConvertor.ROSToUnityScale(new Vector3((float) ActionObjectMetadata.ObjectModel.Box.SizeX, (float) ActionObjectMetadata.ObjectModel.Box.SizeY, (float) ActionObjectMetadata.ObjectModel.Box.SizeZ));
                    } else {
                        foreach (IO.Swagger.Model.Box box in customCollisionModels.Boxes) {
                            if (box.Id == ActionObjectMetadata.Type) {
                                Model.transform.localScale = TransformConvertor.ROSToUnityScale(new Vector3((float) box.SizeX, (float) box.SizeY, (float) box.SizeZ));
                                break;
                            }
                        }
                    }

                    break;
                case IO.Swagger.Model.ObjectModel.TypeEnum.Cylinder:
                    Model = Instantiate(CylinderPrefab, Visual.transform);
                    if (customCollisionModels == null) {
                        Model.transform.localScale = new Vector3((float) ActionObjectMetadata.ObjectModel.Cylinder.Radius, (float) ActionObjectMetadata.ObjectModel.Cylinder.Height / 2, (float) ActionObjectMetadata.ObjectModel.Cylinder.Radius);
                    } else {
                        foreach (IO.Swagger.Model.Cylinder cylinder in customCollisionModels.Cylinders) {
                            if (cylinder.Id == ActionObjectMetadata.Type) {
                                Model.transform.localScale = new Vector3((float) cylinder.Radius, (float) cylinder.Height, (float) cylinder.Radius);
                                break;
                            }
                        }
                    }
                    break;
                case IO.Swagger.Model.ObjectModel.TypeEnum.Sphere:
                    Model = Instantiate(SpherePrefab, Visual.transform);
                    if (customCollisionModels == null) {
                        Model.transform.localScale = new Vector3((float) ActionObjectMetadata.ObjectModel.Sphere.Radius, (float) ActionObjectMetadata.ObjectModel.Sphere.Radius, (float) ActionObjectMetadata.ObjectModel.Sphere.Radius);
                    } else {
                        foreach (IO.Swagger.Model.Sphere sphere in customCollisionModels.Spheres) {
                            if (sphere.Id == ActionObjectMetadata.Type) {
                                Model.transform.localScale = new Vector3((float) sphere.Radius, (float) sphere.Radius, (float) sphere.Radius);
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
        interactObject.transform.localScale = new Vector3(vec.x + 0.01f, vec.y + 0.01f, vec.z + 0.01f);
        interactObject.transform.position = Model.transform.position;
        gameObject.GetComponent<StatefulInteractable>().OnClicked.AddListener(() => HSelectorManager.Instance.OnSelectObject(this));

        //if (IsRobot()) {
        //    Model.tag = "Robot";
        //}

        gameObject.GetComponent<BindParentToChildH>().ChildToBind = Model;
        Collider = Model.GetComponent<Collider>();
        Colliders.Add(Collider);
        // Model.GetComponent<OnClickCollider>().Target = gameObject;

        //  outlineOnClick = gameObject.GetComponent<OutlineOnClick>();

        aoRenderers.Clear();
        aoRenderers.AddRange(Model.GetComponentsInChildren<Renderer>(true));

        if (!ActionObjectMetadata.ObjectModel.Type.Equals(ObjectModel.TypeEnum.Mesh)) {
            if (Model.GetComponent<Outline>() == null) {
                Outline outline = Model.AddComponent<Outline>();

                outline.OutlineColor = new Color(45, 76, 255, 255);
                outline.OutlineWidth = 4;
                outline.enabled = false;

                gameObject.GetComponent<ObjectManipulator>().hoverEntered.AddListener((a) => outline.enabled = true);
                gameObject.GetComponent<ObjectManipulator>().hoverExited.AddListener((a) => outline.enabled = false);

            }

        }
        /*       BoundsControl boundsControl = transform.GetComponent<BoundsControl>();
               Debug.Log(boundsControl);
               transform.GetComponent<BoundsControl>().BoundsOverride = Model.GetComponent<BoxCollider>();*/

        //  outlineOnClick.InitRenderers(aoRenderers);
    }

    public override GameObject GetModelCopy() {
        GameObject model = Instantiate(Model);
        model.transform.localScale = Model.transform.localScale;
        return model;
    }

    /// <summary>
    /// For meshes...
    /// </summary>
    /// <param name="assetLoaderContext"></param>
    public void OnModelLoaded(object sender, ImportedMeshEventArgsH args) {
        if (args.Name != this.GetId())
            return;

        //  bool outlineWasHighlighted = outlineOnClick.Highlighted;

        if (Model != null) {
            /*  outlineOnClick.UnHighlight();
              outlineOnClick.ClearRenderers();*/

            Model.SetActive(false);
            Destroy(Model);
        }



        Model = args.RootGameObject;
        Model.gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
        Model.gameObject.transform.parent = Visual.transform;
        Model.gameObject.transform.localPosition = Vector3.zero;
        Model.gameObject.transform.localRotation = Quaternion.identity;

        gameObject.GetComponent<BindParentToChildH>().ChildToBind = Model;

        foreach (Renderer child in Model.GetComponentsInChildren<Renderer>(true)) {
            //   child.gameObject.AddComponent<OnClickCollider>().Target = gameObject;
            child.gameObject.AddComponent<MeshCollider>();
        }

        aoRenderers.Clear();
        Colliders.Clear();
        aoRenderers.AddRange(Model.GetComponentsInChildren<Renderer>(true));
        Colliders.AddRange(Model.GetComponentsInChildren<MeshCollider>(true));
        /*   outlineOnClick.InitRenderers(aoRenderers);
           outlineOnClick.InitMaterials();*/

        if (aoRenderers.Count > 0) {
            Bounds totalBounds = new Bounds();

            totalBounds = aoRenderers[0].bounds;

            foreach (Renderer renderer in aoRenderers) {
                totalBounds.Encapsulate(renderer.bounds);
            }
            //      totalBounds.Encapsulate (robotColliders[6].bounds);
            interactObject.transform.localScale = transform.InverseTransformVector(totalBounds.size);
            interactObject.transform.position = totalBounds.center;
            interactObject.transform.localRotation = Quaternion.identity;
        }

        //transparent = false; //needs to be set before 1st call of SetVisibility after model loading
        SetVisibility(visibility, forceShaderChange: true);

        /*    if (outlineWasHighlighted) {
                outlineOnClick.Highlight();*/
        /*  if (SelectorMenu.Instance.ManuallySelected) {
              DisplayOffscreenIndicator(true);
          }*/
        //    }
        if (Model.GetComponent<Outline>() == null) {
            Outline outline = Model.AddComponent<Outline>();

            outline.OutlineColor = new Color(45, 76, 255, 255);
            outline.OutlineWidth = 4;
            outline.enabled = false;

            gameObject.GetComponent<ObjectManipulator>().hoverEntered.AddListener((a) => outline.enabled = true);
            gameObject.GetComponent<ObjectManipulator>().hoverExited.AddListener((a) => outline.enabled = false);

        }

        MeshImporterH.Instance.OnMeshImported -= OnModelLoaded;

    }


    /// <summary>
    /// For meshes...
    /// </summary>
    /// <param name="obj"></param>
    private void OnModelLoadError(IContextualizedError obj) {
        //   Notifications.Instance.ShowNotification("Unable to show mesh " + this.GetName(), obj.GetInnerException().Message);
    }


    public override void UpdateColor() {
        if (Blocklisted)
            return;

        SetGrey(IsLockedByOtherUser || isGreyColorForced);
    }

    /// <summary>
    /// Sets grey color of AO model (indicates that model is not in position of real robot)
    /// </summary>
    /// <param name="grey">True for setting grey, false for standard state.</param>
    public void SetGrey(bool grey, bool force = false) {
        isGreyColorForced = force && grey;
        if (force) {
            UpdateColor();
            return;
        }

        if (grey) {
            foreach (Renderer renderer in aoRenderers) {
                foreach (Material mat in renderer.materials) {
                    mat.SetTexture("_EmissionMap", null);
                    mat.SetColor("_EmissionColor", new Color(0.2f, 0.05f, 0.05f));
                    mat.EnableKeyword("_EMISSION");
                }
            }
        } else {
            foreach (Renderer renderer in aoRenderers) {
                foreach (Material mat in renderer.materials) {
                    mat.DisableKeyword("_EMISSION");
                }
            }
        }
    }

    public override void Enable(bool enable, bool putOnBlocklist = false, bool removeFromBlocklist = false) {
        bool prevBlocklisted = Blocklisted;
        base.Enable(enable, putOnBlocklist, removeFromBlocklist);
        if (prevBlocklisted != Blocklisted) {
            if (Blocklisted) {
                SetVisibility(0);
            } else {
                SetVisibility((float) 0.5);
                //    SetVisibility(MainSettingsMenu.Instance.GetVisibilityActionObjects());
            }
        }
    }


    public override string GetObjectTypeName() {
        return "Action object";
    }

    public override void OnObjectLocked(string owner) {
        base.OnObjectLocked(owner);
        if (owner != HLandingManager.Instance.GetUsername())
            ActionObjectName.text = GetLockedText();
    }

    public override void OnObjectUnlocked() {
        base.OnObjectUnlocked();
        ActionObjectName.text = GetName();
    }

    public override void StartManipulation() {
        throw new NotImplementedException();
    }

    public override void EnableVisual(bool enable) {
        //  interactObject.GetComponent<Outline>().enabled = enable;
        Visual.SetActive(enable);
        //  interactObject.SetActive(enable);
    }

    public override void UpdateModel() {
        if (ActionObjectMetadata.ObjectModel == null)
            return;
        Vector3? dimensions = null;
        switch (ActionObjectMetadata.ObjectModel.Type) {
            case ObjectModel.TypeEnum.Box:
                dimensions = TransformConvertor.ROSToUnityScale(new Vector3((float) ActionObjectMetadata.ObjectModel.Box.SizeX, (float) ActionObjectMetadata.ObjectModel.Box.SizeY, (float) ActionObjectMetadata.ObjectModel.Box.SizeZ));
                break;
            case ObjectModel.TypeEnum.Sphere:
                dimensions = TransformConvertor.ROSToUnityScale(new Vector3((float) ActionObjectMetadata.ObjectModel.Sphere.Radius, (float) ActionObjectMetadata.ObjectModel.Sphere.Radius, (float) ActionObjectMetadata.ObjectModel.Sphere.Radius));
                break;
            case ObjectModel.TypeEnum.Cylinder:
                dimensions = TransformConvertor.ROSToUnityScale(new Vector3((float) ActionObjectMetadata.ObjectModel.Cylinder.Radius, (float) ActionObjectMetadata.ObjectModel.Cylinder.Radius, (float) ActionObjectMetadata.ObjectModel.Cylinder.Height));
                break;

        }
        if (dimensions != null)
            Model.transform.localScale = new Vector3(dimensions.Value.x, dimensions.Value.y, dimensions.Value.z);

        Vector3 vec = Model.transform.localScale;
        interactObject.transform.localScale = new Vector3(vec.x + 0.01f, vec.y + 0.01f, vec.z + 0.01f);
        interactObject.transform.position = Model.transform.position;
    }
}
