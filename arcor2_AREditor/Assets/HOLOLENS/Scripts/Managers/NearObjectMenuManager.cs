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
    // TODO rename

    private void Start()
    {
        DeleteButton.OnClicked.AddListener(DeleteClicked);
        DuplicateButton.OnClicked.AddListener(DuplicateClicked);
        ConfigureButton.OnClicked.AddListener(ConfigClicked);
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

    internal void Display(HInteractiveObject selectedObject)
    {
        EnableButtonsBasedOnObject(selectedObject);

        NearObjectMenuGameObject.SetActive(true);
        UI3DHelper.PlaceOnClosestCollisionPointAtBottom(selectedObject, Camera.main.transform.position, NearObjectMenuGameObject.transform);

        // reverse look at
        Vector3 lookDirection = Camera.main.transform.position - NearObjectMenuGameObject.transform.position;
        Quaternion lookRotation = Quaternion.LookRotation(-lookDirection);
        NearObjectMenuGameObject.transform.rotation = lookRotation;

        // move to camera
        NearObjectMenuGameObject.transform.position -= new Vector3(0, 0.05f, 0);
    }

    private void EnableButtonsBasedOnObject(HInteractiveObject selectedObject)
    {
        switch (selectedObject)
        {
            case HAction3D:
            case ActionObjectH:
                DeleteButton.gameObject.SetActive(true);
                DuplicateButton.gameObject.SetActive(true);
                ConfigureButton.gameObject.SetActive(true);
                break;
            case HActionPoint3D:
                DeleteButton.gameObject.SetActive(true);
                DuplicateButton.gameObject.SetActive(true);
                ConfigureButton.gameObject.SetActive(false);
                break;
        }
    }

    internal void Hide()
    {
        NearObjectMenuGameObject.SetActive(false);

    }
}
