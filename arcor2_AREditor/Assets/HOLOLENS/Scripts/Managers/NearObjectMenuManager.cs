using Base;
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
    // TODO rename

    private void Start()
    {
        DeleteButton.OnClicked.AddListener(DeleteClicked);
        DuplicateButton.OnClicked.AddListener(DuplicateClicked);
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
        NearObjectMenuGameObject.SetActive(true);
        UI3DHelper.PlaceOnClosestCollisionPointAtBottom(selectedObject, Camera.main.transform.position, NearObjectMenuGameObject.transform);

        // reverse look at
        Vector3 lookDirection = Camera.main.transform.position - NearObjectMenuGameObject.transform.position;
        Quaternion lookRotation = Quaternion.LookRotation(-lookDirection);
        NearObjectMenuGameObject.transform.rotation = lookRotation;

        // move to camera
        //NearObjectMenuGameObject.transform.localPosition -= new Vector3(0, 0, 0.2f);
        NearObjectMenuGameObject.transform.position -= new Vector3(0, 0.05f, 0);
    }

    internal void Hide()
    {
        NearObjectMenuGameObject.SetActive(false);

    }
}
