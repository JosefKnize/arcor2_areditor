using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Base;
using Hololens;
using System;


public class HNotificationManager : Singleton<HNotificationManager>
{

    public GameObject notificationScreen;
    public GameObject parentNotif;
    public GameObject tempObject;
    // Start is called before the first frame update
    public void ShowNotification(String text){
        GameObject notification = Instantiate(tempObject);
        notification.SetActive(true);
        notification.GetComponent<TMPro.TMP_Text>().text = text;
        notification.transform.SetParent(parentNotif.transform, false);
    }


    public void ShowNotificationScreen(){
        notificationScreen.SetActive(true);
    }

    public void HideNotificationScreen(){
        notificationScreen.SetActive(false);
    }
}
