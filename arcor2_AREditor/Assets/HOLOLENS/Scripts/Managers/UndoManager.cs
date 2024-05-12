/*
 Author: Josef Kníže
*/

using Base;
using Hololens;
using MixedReality.Toolkit.UX;
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

        if (UndoActions.Count > 50)
        {
            UndoActions.RemoveAt(0);
            RedoActions.Clear();
        }
        UpdateButtonStates();
    }


    #region ActionPoint specific

    public void Undo(HActionPoint3D actionPoint)
    {
        var undoRecord = UndoActions.LastOrDefault(x => x is ActionPointUpdateUndoRecord apur && apur.ActionPoint == actionPoint);
        if (undoRecord is not null)
        {
            ExecuteSpecificUndo(undoRecord);
        }
    }

    public void Redo(HActionPoint3D actionPoint)
    {
        var undoRecord = RedoActions.LastOrDefault(x => x is ActionPointUpdateUndoRecord apur && apur.ActionPoint == actionPoint);
        if (undoRecord is not null)
        {
            ExecuteSpecificRedo(undoRecord);
        }
    }

    internal bool HasUndo(HActionPoint3D actionPoint) => UndoActions.Any(x => x is ActionPointUpdateUndoRecord apur && apur.ActionPoint == actionPoint);
    internal bool HadRedo(HActionPoint3D actionPoint) => RedoActions.Any(x => x is ActionPointUpdateUndoRecord apur && apur.ActionPoint == actionPoint);

    #endregion

    #region ActionObject specific

    public void Undo(ActionObjectH actionObject)
    {
        var undoRecord = UndoActions.LastOrDefault(x => x is ActionObjectUpdateUndoRecord aour && aour.ActionObject == actionObject);
        if (undoRecord is not null)
        {
            ExecuteSpecificUndo(undoRecord);
        }
    }

    public void Redo(ActionObjectH actionObject)
    {
        var undoRecord = RedoActions.LastOrDefault(x => x is ActionObjectUpdateUndoRecord aour && aour.ActionObject == actionObject);
        if (undoRecord is not null)
        {
            ExecuteSpecificRedo(undoRecord);
        }
    }

    internal bool HasUndo(ActionObjectH actionObject) => UndoActions.Any(x => x is ActionObjectUpdateUndoRecord aour && aour.ActionObject == actionObject);
    internal bool HadRedo(ActionObjectH actionObject) => RedoActions.Any(x => x is ActionObjectUpdateUndoRecord aour && aour.ActionObject == actionObject);

    #endregion

    public void ExecuteSpecificUndo(UndoRecord undoRecord)
    {
        undoRecord.Undo();
        UndoActions.Remove(undoRecord);
        RedoActions.Add(undoRecord);
        UpdateButtonStates();
    }

    public void ExecuteSpecificRedo(UndoRecord undoRecord)
    {
        undoRecord.Redo();
        RedoActions.Remove(undoRecord);
        UndoActions.Add(undoRecord);
        UpdateButtonStates();
    }

    public void UpdateButtonStates()
    {
        UndoInteractable.enabled = UndoActions.Count > 0;
        RedoInteractable.enabled = RedoActions.Count > 0;

        NearObjectMenuManager.Instance.EnableButtonsBasedOnObject();
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
