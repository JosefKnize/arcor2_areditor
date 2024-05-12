/*
 Author: Josef Kníže
*/

using Base;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnityMainThreadExecutor : Singleton<UnityMainThreadExecutor>
{
    private List<System.Action> actions = new List<System.Action>();
    private void Start()
    {
        // HACK: someone need to at least once get instance otherwise secondary threads cannot get it
        var instance  = UnityMainThreadExecutor.Instance;
    }

    private void Update()
    {
        foreach (var action in actions.ToList())
        {
            actions.Remove(action);
            action.Invoke();
        }
    }

    public void RegisterAction(System.Action action)
    {
        actions.Add(action);
    }
}
