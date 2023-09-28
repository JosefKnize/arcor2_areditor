using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using Base;

public class HRenameDialog : MonoBehaviour
{  
  public DialogShell dialogShell;

  public GameObject buttonConfirm;
  public GameObject buttonCancel;

  public MRTKUGUIInputField newName;

  private UnityAction confirmCallback;

  private HInteractiveObject interactiveObject;

  private bool newObject;
  private bool keepObjectLocked;


    public async void Open(HInteractiveObject objectToRename,bool newObject = false, bool keepObjectLocked = false, UnityAction confirmationCallback = null, UnityAction cancelCallback = null/*, string confirmLabel = "Confirm", string cancelLabel = "Cancel", bool wideButtons = false*/) {
        
        if (objectToRename == null)
            return;
        if (!await objectToRename.WriteLock(false))
            return;

        this.newObject = newObject;
            this.keepObjectLocked = keepObjectLocked;
        interactiveObject = objectToRename;
        
        newName.text = interactiveObject.GetName();

        dialogShell.DescriptionText.text = "Rename " + interactiveObject.GetObjectTypeName();
       
        buttonCancel.GetComponent<Interactable>().OnClick.RemoveAllListeners();
        buttonCancel.GetComponent<ButtonConfigHelper>().OnClick.RemoveAllListeners();
        buttonConfirm.GetComponent<ButtonConfigHelper>().MainLabelText = "Confirm";
        buttonCancel.GetComponent<ButtonConfigHelper>().MainLabelText = "Cancel";

        buttonCancel.GetComponent<ButtonConfigHelper>().OnClick.AddListener(() => Close());
        buttonCancel.GetComponent<Interactable>().OnClick.AddListener(() => Close());

        if (cancelCallback != null)
             buttonCancel.GetComponent<Interactable>().OnClick.AddListener(cancelCallback);
        this.confirmCallback = confirmationCallback;
        gameObject.SetActive(true);
        
    }

    public async void Confirm() {
        if (newName.text == interactiveObject.GetName()) { //for new objects, without changing name
            Close();
            confirmCallback?.Invoke();
            return;
            
        }

        try {
            await interactiveObject.Rename(newName.text);
            Close();
            confirmCallback?.Invoke();
        } catch (RequestFailedException) {
            //notification already shown, nothing else to do
        }
    }

    public virtual void Close(){
        if (interactiveObject != null && !keepObjectLocked) {
            _ = interactiveObject.WriteUnlock();
        }

        gameObject.SetActive(false);
        interactiveObject = null;
    }

}
