/*
 Author: Josef Kníže
*/

using MixedReality.Toolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;

public static class StatefulInteractableLongSelectExtension
{
    private static Dictionary<StatefulInteractable, Timer> SelectStartedDictionary = new();
    public static void RegisterOnLongSelect(this StatefulInteractable interactable, System.Action callback, int milliseconds = 300)
    {
        interactable.firstSelectEntered.AddListener((arg) =>
        {
            var timer = new Timer(milliseconds);
            timer.Elapsed += (sender, e) => TimerElapsed(interactable, callback);
            timer.AutoReset = false;
            timer.Enabled = true;
            SelectStartedDictionary[interactable] = timer;

        });

        interactable.lastSelectExited.AddListener((arg) =>
        {
            var timer = SelectStartedDictionary[interactable];
            SelectStartedDictionary.Remove(interactable);
            timer.Dispose();
        });
    }

    private static void TimerElapsed(StatefulInteractable interactable, System.Action callback)
    {
        UnityMainThreadExecutor.Instance.RegisterAction(callback);
    }
}

public static class StatefulInteractableShortClickExtension
{
    private static Dictionary<StatefulInteractable, DateTime> clickStartedDictionary = new();
    public static void RegisterOnShortClick(this StatefulInteractable interactable, System.Action callback, int milliseconds = 300)
    {
        interactable.firstSelectEntered.AddListener((args) =>
        {
            clickStartedDictionary[interactable] = DateTime.Now;
        });

        interactable.lastSelectExited.AddListener((args) =>
        {
            var difference = DateTime.Now - clickStartedDictionary[interactable];
            if (difference.TotalMilliseconds < milliseconds)
            {
                callback();
            }
            else
            {
                Debug.Log("Ignored long click");
            }
        });
    }
}

public static class StatefulInteractableLongHoverExtension
{
    private static Dictionary<StatefulInteractable, Timer> HoverStartedDictionary = new();
    public static void RegisterOnLongHover(this StatefulInteractable interactable, System.Action callback)
    {
        interactable.firstHoverEntered.AddListener((arg) =>
        {
            var timer = new Timer(1200);
            timer.Elapsed += (sender, e) => TimerElapsed(interactable, callback);
            timer.AutoReset = false;
            timer.Enabled = true;
            HoverStartedDictionary[interactable] = timer;

        });

        interactable.lastHoverExited.AddListener((arg) =>
        {
            var timer = HoverStartedDictionary[interactable];
            HoverStartedDictionary.Remove(interactable);
            timer.Dispose();
        });
    }

    private static void TimerElapsed(StatefulInteractable interactable, System.Action callback)
    {
        UnityMainThreadExecutor.Instance.RegisterAction(callback);
    }
}