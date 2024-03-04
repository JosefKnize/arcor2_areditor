using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Hololens;
using Newtonsoft.Json;
using Base;
using MixedReality.Toolkit;
using TMPro;
using System;

public class HActionPickerMenu : Singleton<HActionPickerMenu>
{
    public GameObject CloseButton;
    public GameObject ActionsButtonCollection;
    public GameObject ActionButtonPrefab;
    public GameObject ActionPickerMenu;

    public Dictionary<string, GameObject> listOfActions = new Dictionary<string, GameObject>();
    private HActionPoint currentActionPoint;

    void Start()
    {
        CloseButton.GetComponent<StatefulInteractable>().OnClicked.AddListener(() => ActionPickerMenu.SetActive(false));
    }

    public void ClearMenu()
    {
        foreach (KeyValuePair<string, GameObject> kvp in listOfActions)
        {
            Destroy(kvp.Value);
        }
        listOfActions.Clear();
    }

    public void ShowMenu(ActionObjectH parent)
    {
        // Adjust position
        ActionPickerMenu.transform.parent = parent.transform;
        var collider = parent.InteractionObjectCollider.GetComponent<Collider>();
        parent.InteractionObjectCollider.SetActive(true);
        var closestPoint = collider.ClosestPoint(Camera.main.transform.position);
        parent.InteractionObjectCollider.SetActive(false);

        ActionPickerMenu.transform.position = closestPoint;
        ActionPickerMenu.SetActive(true);
    }

    public void CloseMenu()
    {
        ClearMenu();
        ActionPickerMenu.transform.parent = null;
        ActionPickerMenu.SetActive(false);
    }

    public void PopulateMenu(ActionObjectH actionObject)
    {
        ClearMenu();

        if (ActionsManagerH.Instance.ActionObjectsMetadata.TryGetValue(actionObject.Data.Type, out ActionObjectMetadataH aom))
        {
            aom.ActionsMetadata.Values.ToList();
            foreach (ActionMetadataH am in aom.ActionsMetadata.Values.ToList())
            {
                GameObject button = Instantiate(ActionButtonPrefab);
                button.transform.parent = ActionsButtonCollection.transform;
                button.transform.localPosition = Vector3.zero;
                button.transform.eulerAngles = ActionsButtonCollection.transform.eulerAngles;

                button.GetComponent<StatefulInteractable>().OnClicked.AddListener(() => ActionSelected(am.Name, actionObject));
                button.GetComponentInChildren<TextMeshProUGUI>().text = am.Name;

                listOfActions.Add(am.Name, button);
            }
        }
    }

    public void ActionSelected(string action_id, IActionProviderH actionProvider)
    {
        CloseMenu();
        HSelectorManager.Instance.ActionSelected(action_id, actionProvider);
    }
}
