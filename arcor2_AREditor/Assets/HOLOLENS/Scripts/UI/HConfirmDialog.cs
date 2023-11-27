using UnityEngine;
using UnityEngine.Events;
using MixedReality.Toolkit.UX.Deprecated;
using MixedReality.Toolkit;

public class HConfirmDialog : MonoBehaviour
{
  public DialogShell dialogShell;

  public GameObject buttonLeft;
  public GameObject buttonRight;


    public virtual void Open(string title, string description, UnityAction confirmationCallback, UnityAction cancelCallback, string confirmLabel = "Confirm", string cancelLabel = "Cancel", bool wideButtons = false) {
       
       //buttonLeft.GetComponent<StatefulInteractable>().OnClicked.RemoveAllListeners();
       //buttonRight.GetComponent<StatefulInteractable>().OnClicked.RemoveAllListeners();
       //buttonLeft.GetComponent<ButtonConfigHelper>().OnClick.RemoveAllListeners();
       //buttonRight.GetComponent<ButtonConfigHelper>().OnClick.RemoveAllListeners();
       //dialogShell.TitleText.text = title;
       //dialogShell.DescriptionText.text = description;
       //buttonLeft.GetComponent<ButtonConfigHelper>().MainLabelText = confirmLabel;
       //buttonRight.GetComponent<ButtonConfigHelper>().MainLabelText = cancelLabel;
       //buttonLeft.GetComponent<ButtonConfigHelper>().OnClick.AddListener(confirmationCallback);
       //buttonRight.GetComponent<ButtonConfigHelper>().OnClick.AddListener(() => Close());
       //buttonLeft.GetComponent<StatefulInteractable>().OnClick.AddListener(confirmationCallback);
       //buttonRight.GetComponent<StatefulInteractable>().OnClick.AddListener(() => Close());
       //gameObject.SetActive(true);
    }

    public virtual void Close(){
        gameObject.SetActive(false);
    }

    
}
