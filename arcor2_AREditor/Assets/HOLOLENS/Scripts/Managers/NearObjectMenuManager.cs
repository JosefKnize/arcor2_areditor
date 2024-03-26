using Base;
using Hololens;
using MixedReality.Toolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class NearObjectMenuManager : Singleton<NearObjectMenuManager>
{
    public GameObject NearObjectMenuGameObject;

    public StatefulInteractable DeleteButton;
    public StatefulInteractable DuplicateButton;
    public StatefulInteractable ConfigureButton;
    public StatefulInteractable UndoButton;
    public StatefulInteractable RedoButton;

    private HInteractiveObject selectedObject;

    // TODO rename

    private void Start()
    {
        DeleteButton.OnClicked.AddListener(DeleteClicked);
        DuplicateButton.OnClicked.AddListener(DuplicateClicked);
        ConfigureButton.OnClicked.AddListener(ConfigClicked);
        UndoButton.OnClicked.AddListener(UndoClicked);
        RedoButton.OnClicked.AddListener(RedoClicked);

        GameManagerH.Instance.OnGameStateChanged += (_, _) => { selectedObject = null; Hide(); };
    }

    internal void Display(HInteractiveObject selectedObject)
    {
        this.selectedObject = selectedObject;
        EnableButtonsBasedOnObject();
        NearObjectMenuGameObject.SetActive(true);

        UI3DHelper.PlaceOnClosestCollisionPointAtBottom(selectedObject, Camera.main.transform.position, NearObjectMenuGameObject.transform);
        NearObjectMenuGameObject.transform.position += NearObjectMenuGameObject.transform.forward * -0.06f;
        NearObjectMenuGameObject.transform.position -= new Vector3(0, 0.06f, 0);
    }

    internal void Hide()
    {
        NearObjectMenuGameObject.SetActive(false);
    }

    public void EnableButtonsBasedOnObject()
    {
        switch (selectedObject)
        {
            case ActionObjectH actionObject:
                DeleteButton.gameObject.SetActive(true);
                DuplicateButton.gameObject.SetActive(true);
                ConfigureButton.gameObject.SetActive(true);
                UndoButton.gameObject.SetActive(UndoManager.Instance.HasUndo(actionObject));
                RedoButton.gameObject.SetActive(UndoManager.Instance.HadRedo(actionObject));
                break;
            case HAction3D action:
                DeleteButton.gameObject.SetActive(true);
                DuplicateButton.gameObject.SetActive(true);
                ConfigureButton.gameObject.SetActive(true);
                
                break;
            case HActionPoint3D actionPoint:
                DeleteButton.gameObject.SetActive(true);
                DuplicateButton.gameObject.SetActive(true);
                ConfigureButton.gameObject.SetActive(false);
                UndoButton.gameObject.SetActive(UndoManager.Instance.HasUndo(actionPoint));
                RedoButton.gameObject.SetActive(UndoManager.Instance.HadRedo(actionPoint));
                break;
        }
    }

    private void UndoClicked()
    {
        switch (selectedObject)
        {
            case HActionPoint3D actionPoint:
                UndoManager.Instance.Undo(actionPoint);
                break;
            case ActionObjectH actionObject:
                UndoManager.Instance.Undo(actionObject);
                break;
        }
    }

    private void RedoClicked()
    {
        switch (selectedObject)
        {
            case HActionPoint3D actionPoint:
                UndoManager.Instance.Redo(actionPoint);
                break;
            case ActionObjectH actionObject:
                UndoManager.Instance.Redo(actionObject);
                break;
        }
    }

    private void ConfigClicked()
    {
        HSelectorManager.Instance.ConfigureClicked();
    }

    private void DuplicateClicked()
    {
        HSelectorManager.Instance.DuplicateObjectClicked();
    }

    private void DeleteClicked()
    {
        HSelectorManager.Instance.OpenRemoveObjectDialog();
    }
}
