using UnityEngine;
using UnityEngine.Events;
using MixedReality.Toolkit.UX.Deprecated;
using MixedReality.Toolkit;
using TMPro;
using System;

public class HConfirmDialog : MonoBehaviour
{
    //public GameObject DialogGameObject;

    public StatefulInteractable ConfirmButton;
    public StatefulInteractable CancelButton;
    public TextMeshProUGUI Title;
    public TextMeshProUGUI MainText;

    private UnityAction confirmAction;
    private UnityAction cancelAction;

    private void Start()
    {
        ConfirmButton.OnClicked.AddListener(ConfirmClicked);
        CancelButton.OnClicked.AddListener(CancelClicked);
    }

    public virtual void Open(string title, string description, UnityAction confirmationCallback, UnityAction cancelCallback)
    {
        transform.gameObject.SetActive(true);
        Title.text = title;
        MainText.text = description;

        confirmAction = confirmationCallback;
        cancelAction = cancelCallback;
    }

    private void CancelClicked()
    {
        cancelAction.Invoke();
        transform.gameObject.SetActive(false);
    }

    private void ConfirmClicked()
    {
        confirmAction.Invoke();
        transform.gameObject.SetActive(false);
    }
}
