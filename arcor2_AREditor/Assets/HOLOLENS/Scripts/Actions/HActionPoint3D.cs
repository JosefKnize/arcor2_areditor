using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IO.Swagger.Model;
using TMPro;
using System;
using System.Threading.Tasks;
using Base;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine.XR.Interaction.Toolkit;
using RequestResult = Base.RequestResult;

//[RequireComponent(typeof(OutlineOnClick))]
//[RequireComponent(typeof(Target))]
public class HActionPoint3D : HActionPoint
{
    public GameObject Sphere, Visual, CollapsedPucksVisual, Lock;
    public TextMeshPro ActionPointName;
    public Material BreakPointMaterial, SphereMaterial;
    /* [SerializeField]
     private OutlineOnClick outlineOnClick;*/
    public GameObject ActionsVisuals;

    public GameObject InteractionObject;
    private Vector3 initialPosition;

    private void Start()
    {
        base.Start();
        RegisterTransformationEvents();
    }

    private void LateUpdate()
    {
        // Fix of AP rotations - works on both PC and tablet
        transform.rotation = SceneManagerH.Instance.SceneOrigin.transform.rotation;
        ActionsVisuals.transform.rotation = SceneManagerH.Instance.SceneOrigin.transform.rotation;
        //Visual.transform.rotation = Base.SceneManager.Instance.SceneOrigin.transform.rotation;
        if (Parent != null)
            orientations.transform.rotation = Parent.GetTransform().rotation;
        else
            orientations.transform.rotation = SceneManagerH.Instance.SceneOrigin.transform.rotation;
    }

    public override bool BreakPoint
    {
        get => base.BreakPoint;
        set
        {
            base.BreakPoint = value;
            Renderer r = Sphere.GetComponent<Renderer>();
            if (r.materials.Length == 3)
            {
                List<Material> materials = new List<Material>(r.materials)
                {
                    [1] = BreakPoint ? BreakPointMaterial : SphereMaterial
                };
                r.materials = materials.ToArray();
            }
            else
            {
                r.material = BreakPoint ? BreakPointMaterial : SphereMaterial;
            }
        }
    }

    public async void ShowMenu(bool enableBackButton = false)
    {
        throw new NotImplementedException();
    }


    public override Vector3 GetScenePosition()
    {
        return TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(Data.Position));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="position">Global position of AP</param>
    public override void SetScenePosition(Vector3 position)
    {
        Data.Position = DataHelper.Vector3ToPosition(TransformConvertor.UnityToROS(position));
    }

    public override Quaternion GetSceneOrientation()
    {
        //return TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(Data.Orientations[0].Orientation));
        return Quaternion.identity;
    }

    public override void SetSceneOrientation(Quaternion orientation)
    {
        //Data.Orientations.Add(new IO.Swagger.Model.NamedOrientation(id: "default", orientation:DataHelper.QuaternionToOrientation(TransformConvertor.UnityToROS(orientation))));
    }

    public override void UpdatePositionsOfPucks()
    {
        CollapsedPucksVisual.SetActive(HProjectManager.Instance.AllowEdit && ActionsCollapsed);
        if (HProjectManager.Instance.AllowEdit && ActionsCollapsed)
        {
            foreach (HAction3D action in Actions.Values)
            {
                action.transform.localPosition = new Vector3(0, 0, 0);
                action.transform.localScale = new Vector3(0, 0, 0);
            }

        }
        else
        {
            int i = 1;
            foreach (HAction3D action in Actions.Values)
            {
                action.transform.localPosition = new Vector3(0, i * 0.05f + 0.025f, 0);
                ++i;
                action.transform.localScale = new Vector3(1, 1, 1);
            }
        }
    }

    public override bool ProjectInteractable()
    {
        return base.ProjectInteractable();
    }

    public override void ActivateForGizmo(string layer)
    {
        base.ActivateForGizmo(layer);
        Sphere.layer = LayerMask.NameToLayer(layer);
    }

    /// <summary>
    /// Changes size of shpere representing action point
    /// </summary>
    /// <param name="size"><0; 1> - 0 means invisble, 1 means 10cm in diameter</param>
    public override void SetSize(float size)
    {
        Visual.transform.localScale = new Vector3(size / 10, size / 10, size / 10);
    }

    public override (List<string>, Dictionary<string, string>) UpdateActionPoint(IO.Swagger.Model.ActionPoint projectActionPoint)
    {
        (List<string>, Dictionary<string, string>) result = base.UpdateActionPoint(projectActionPoint);
        ActionPointName.text = projectActionPoint.Name;
        return result;
    }

    public override void UpdateOrientation(NamedOrientation orientation)
    {
        base.UpdateOrientation(orientation);
    }

    public override void AddOrientation(NamedOrientation orientation)
    {
        base.AddOrientation(orientation);
    }

    public override void HighlightAP(bool highlight)
    {
        /*  if (highlight) {
              outlineOnClick.Highlight();
          } else {
              outlineOnClick.UnHighlight();
          }*/
    }


    public override void ActionPointBaseUpdate(IO.Swagger.Model.BareActionPoint apData)
    {
        base.ActionPointBaseUpdate(apData);
        ActionPointName.text = apData.Name;
    }

    public override void InitAP(IO.Swagger.Model.ActionPoint apData, float size, IActionPointParentH parent = null)
    {
        base.InitAP(apData, size, parent);
        ActionPointName.text = apData.Name;
    }

    public override void UpdateColor()
    {
        if (Enabled && !(IsLocked && !IsLockedByMe))
        {
            SphereMaterial.color = new Color(0.51f, 0.51f, 0.89f);
            BreakPointMaterial.color = new Color(0.93f, 0.07f, 0.09f);
        }
        else
        {
            SphereMaterial.color = Color.gray;
            BreakPointMaterial.color = Color.gray;
        }
    }

    public async override void StartManipulation()
    {
        throw new NotImplementedException();
    }

    internal GameObject GetModelCopy()
    {
        GameObject sphere = Instantiate(Sphere);
        Destroy(sphere.GetComponent<SphereCollider>());
        sphere.transform.localScale = Visual.transform.localScale;
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localRotation = Quaternion.identity;
        return sphere;
    }

    public async override Task<RequestResult> Removable()
    {
        if (GameManagerH.Instance.GetGameState() != GameManagerH.GameStateEnum.ProjectEditor)
        {
            return new RequestResult(false, "AP could only be removed in project editor");
        }
        else
        {
            try
            {
                await WebSocketManagerH.Instance.RemoveActionPoint(GetId(), true);
                return new RequestResult(true);
            }
            catch (RequestFailedException ex)
            {
                return new RequestResult(false, ex.Message);
            }
        }
    }

    public async override void Remove()
    {
        try
        {
            await WebSocketManagerH.Instance.RemoveActionPoint(GetId(), false);
        }
        catch (RequestFailedException ex)
        {
            //Notifications.Instance.ShowNotification("Failed to remove AP " + GetName(), ex.Message);
        }
    }

    public async override Task Rename(string name)
    {
        try
        {
            await WebSocketManagerH.Instance.RenameActionPoint(GetId(), name);
            //     Notifications.Instance.ShowToastMessage("Action point renamed");
        }
        catch (RequestFailedException e)
        {
            //    Notifications.Instance.ShowNotification("Failed to rename action point", e.Message);
        }
    }

    public override string GetObjectTypeName()
    {
        return "Action point";
    }

    public override void OnObjectLocked(string owner)
    {
        base.OnObjectLocked(owner);
        //     if (owner != LandingScreen.Instance.GetUsername())
        //       ActionPointName.text = GetLockedText();
    }

    public override void OnObjectUnlocked()
    {
        base.OnObjectUnlocked();
        ActionPointName.text = GetName();
    }

    public override void EnableVisual(bool enable)
    {
        Visual.SetActive(enable);
        InteractionObject.SetActive(enable);
    }

    internal void RegisterTransformationEvents()
    {
        var objectManipulator = transform.GetComponent<ObjectManipulator>();

        objectManipulator.OnClicked.AddListener(() => HSelectorManager.Instance.OnObjectInteraction(this));
        objectManipulator.RegisterOnLongHover(() => HSelectorManager.Instance.OnObjectSelected(this));


        // Send new position to server after ending manipulation
        objectManipulator.lastSelectExited.AddListener(EndTransform);

        // On start lock object 
        objectManipulator.firstSelectEntered.AddListener(StartTransform);
    }

    public async void EndTransform(SelectExitEventArgs arg0)
    {
        Debug.Log("End AP transform");
        if (IsLockedByMe)
        {
            await UploadNewPositionAsync();

            UndoManager.Instance.AddUndoRecord(new ActionPointUpdateUndoRecord()
            {
                ActionPoint = this,
                NewPosition = transform.localPosition,
                InitialPosition = initialPosition,
            });

            await WriteUnlock();
            var objectManipulator = transform.GetComponent<ObjectManipulator>();
            objectManipulator.AllowedManipulations = TransformFlags.None;
        }
    }

    public async Task UploadNewPositionAsync()
    {
        await WebSocketManagerH.Instance.UpdateActionPointPosition(
                GetId(),
                DataHelper.Vector3ToPosition(TransformConvertor.UnityToROS(transform.parent.InverseTransformPoint(transform.position))));
    }

    public async void StartTransform(SelectEnterEventArgs arg0)
    {
        Debug.Log("Start AP transform");
        var objectManipulator = transform.GetComponent<ObjectManipulator>();
        initialPosition = transform.localPosition;

        if (HSelectorManager.Instance.selectorState == HSelectorManager.SelectorState.Normal && await WriteLock(true))
        {
            objectManipulator.AllowedManipulations = TransformFlags.Move;
            return;
        }
        else
        {
            Debug.Log("Cant move object because it's locked");
            objectManipulator.AllowedManipulations = TransformFlags.None;
            // TODO probably notify user MAYBE just sound
        }
    }

}
