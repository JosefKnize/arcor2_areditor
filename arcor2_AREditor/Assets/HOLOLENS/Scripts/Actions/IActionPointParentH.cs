using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IActionPointParentH
{
    string GetName();

    string GetId();

    bool IsActionObject();

    Hololens.ActionObjectH GetActionObject();

    Transform GetTransform();

    GameObject GetGameObject();

    Transform GetSpawnPoint();
}
