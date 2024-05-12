/*
 Author: Simona Hiadlovská
 Amount of changes: 50% changed - A lot of changes, moved a lot of logic to SelectorManager, changed the order of adding action
 Edited by: Josef Kníže
*/

using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Hololens;
using Newtonsoft.Json;
using Base;
using MixedReality.Toolkit;
using TMPro;
using System;
using UnityEngine.InputSystem.HID;

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
        var collider = parent.InteractionObjectCollider.GetComponent<Collider>();

        parent.InteractionObjectCollider.SetActive(true);

        var closestPoint = collider.ClosestPoint(Camera.main.transform.position - new Vector3(0, 0.3f, 0));
        ActionPickerMenu.transform.position = closestPoint;

        ActionPickerMenu.SetActive(true);
        parent.InteractionObjectCollider.SetActive(false);
    }

    public void CloseMenu()
    {
        ClearMenu();
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
        HSelectorManager.Instance.PlacedActionPicked(action_id, actionProvider);
    }
}
