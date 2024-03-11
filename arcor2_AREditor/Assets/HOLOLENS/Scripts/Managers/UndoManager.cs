using Base;
using Hololens;
using IO.Swagger.Model;
using MixedReality.Toolkit;
using MixedReality.Toolkit.UX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UndoManager : Singleton<UndoManager>
{

    public PressableButton UndoInteractable;
    public PressableButton RedoInteractable;
    public List<UndoRecord> UndoActions { get; set; } = new ();
    public List<UndoRecord> RedoActions { get; set; } = new ();

    public void Undo()
    {
        if (UndoActions.Count > 0)
        {
            UndoRecord lastRecord = UndoActions.Last();
            lastRecord.Undo();
            UndoActions.Remove(lastRecord);
            RedoActions.Add(lastRecord);
            UpdateButtonStates();
        }
    }

    public void Redo()
    {
        if (RedoActions.Count > 0)
        {
            UndoRecord lastUndoneRecord = RedoActions.Last();
            lastUndoneRecord.Redo();
            RedoActions.Remove(lastUndoneRecord);
            UndoActions.Add(lastUndoneRecord);
            UpdateButtonStates();
        }
    }

    public void AddUndoRecord(UndoRecord record)
    {
        UndoActions.Add(record);

        if (UndoActions.Count > 10)
        {
            UndoActions.RemoveAt(0);
            RedoActions.Clear();
        }
        UpdateButtonStates();
    }

    public void UpdateButtonStates()
    {
        UndoInteractable.enabled = UndoActions.Count > 0;
        RedoInteractable.enabled = RedoActions.Count > 0;
    }
}

public abstract class UndoRecord
{
    public abstract void Undo();
    public abstract void Redo();
}

public class ActionObjectUpdateUndoRecord : UndoRecord
{
    public ActionObjectH ActionObject { get; internal set; }
    public Vector3 NewPosition { get; internal set; }
    public Quaternion NewRotation { get; internal set; }
    public Vector3 InitialPosition { get; internal set; }
    public Quaternion InitialRotation { get; internal set; }

    public override void Undo()
    {
        ActionObject.transform.localPosition = InitialPosition;
        ActionObject.transform.localRotation = InitialRotation;
        ActionObject.UploadNewPositionAsync();
    }

    public override void Redo()
    {
        ActionObject.transform.localPosition = NewPosition;
        ActionObject.transform.localRotation = NewRotation;
        ActionObject.UploadNewPositionAsync();
    }
}

public class ActionPointUpdateUndoRecord : UndoRecord
{
    public HActionPoint3D ActionPoint { get; internal set; }
    public Vector3 NewPosition { get; internal set; }
    public Vector3 InitialPosition { get; internal set; }

    public override void Undo()
    {
        ActionPoint.transform.localPosition = InitialPosition;
        ActionPoint.UploadNewPositionAsync();
    }

    public override void Redo()
    {
        ActionPoint.transform.localPosition = NewPosition;
        ActionPoint.UploadNewPositionAsync();
    }
}
