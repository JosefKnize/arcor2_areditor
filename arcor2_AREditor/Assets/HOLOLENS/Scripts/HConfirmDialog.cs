using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Microsoft.MixedReality.Toolkit.UI;


public class HConfirmDialog : MonoBehaviour
{
  public DialogShell dialogShell;

  public GameObject buttonLeft;
  public GameObject buttonRight;


    public virtual void Open(string title, string description, UnityAction confirmationCallback, UnityAction cancelCallback, string confirmLabel = "Confirm", string cancelLabel = "Cancel", bool wideButtons = false) {
       
       buttonLeft.GetComponent<Interactable>().OnClick.RemoveAllListeners();
       buttonRight.GetComponent<Interactable>().OnClick.RemoveAllListeners();
       buttonLeft.GetComponent<ButtonConfigHelper>().OnClick.RemoveAllListeners();
       buttonRight.GetComponent<ButtonConfigHelper>().OnClick.RemoveAllListeners();
       dialogShell.TitleText.text = title;
       dialogShell.DescriptionText.text = description;
       buttonLeft.GetComponent<ButtonConfigHelper>().MainLabelText = confirmLabel;
       buttonRight.GetComponent<ButtonConfigHelper>().MainLabelText = cancelLabel;
       buttonLeft.GetComponent<ButtonConfigHelper>().OnClick.AddListener(confirmationCallback);
       buttonRight.GetComponent<ButtonConfigHelper>().OnClick.AddListener(() => Close());
       buttonLeft.GetComponent<Interactable>().OnClick.AddListener(confirmationCallback);
       buttonRight.GetComponent<Interactable>().OnClick.AddListener(() => Close());
       gameObject.SetActive(true);
    }

    public virtual void Close(){
        gameObject.SetActive(false);
    }

    
}
