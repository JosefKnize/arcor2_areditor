using MixedReality.Toolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using TriLibCore.Extensions;
using UnityEngine;
using UnityEngine.Events;

public class ComboBox : MonoBehaviour
{

    public GameObject SelectedButtonPrefab;
    public GameObject ButtonPrefab;
    public GameObject DropdownPrefab;

    public IEnumerable<object> Items;

    private bool dropdownMenuOpen;
    private GameObject dropDownGameObject;

    private object selectedItem;
    public object SelectedItem
    {
        get => selectedItem;
        set
        {
            selectedItem = value;
            transform.GetComponentInChildren<TextMeshProUGUI>().text = selectedItem?.ToString();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        var statefulInteractable = transform.GetComponent<StatefulInteractable>();
        statefulInteractable.OnClicked.AddListener(ComboboxClicked);

        List<string> a = new List<string>()
        {
            "Ahoj",
            "Èau",
            "Hi",
        };

        Items = a.Cast<object>().ToList();
    }

    void Update()
    {

    }

    private void ComboboxClicked()
    {
        if (dropdownMenuOpen)
        {
            CloseDropDownMenu();
        }
        else
        {
            dropDownGameObject = Instantiate(DropdownPrefab);
            var dropDownButtonCollection = dropDownGameObject.transform.Find("Canvas");

            var button = Instantiate(SelectedButtonPrefab, dropDownButtonCollection);
            button.transform.GetComponentInChildren<TextMeshProUGUI>().text = SelectedItem?.ToString();
            button.GetComponent<StatefulInteractable>().OnClicked.AddListener(() => DropDownButtonClicked(SelectedItem));

            foreach (var item in Items)
            {
                if (SelectedItem?.Equals(item) ?? false)
                {
                    continue;
                }

                button = Instantiate(ButtonPrefab, dropDownButtonCollection);
                button.transform.GetComponentInChildren<TextMeshProUGUI>().text = item.ToString();
                button.GetComponent<StatefulInteractable>().OnClicked.AddListener(() => DropDownButtonClicked(item));
            }

            dropDownGameObject.transform.position = transform.position;
            dropDownGameObject.transform.rotation = transform.rotation;
            dropDownGameObject.transform.position += dropDownGameObject.transform.forward * -0.02f;
            dropdownMenuOpen = true;
        }
    }

    private void DropDownButtonClicked(object item)
    {
        SelectedItem = item;
        CloseDropDownMenu();
    }

    private void CloseDropDownMenu()
    {
        dropdownMenuOpen = false;
        Destroy(dropDownGameObject);
    }
}
