using System;
using System.Threading.Tasks;
using Base;
using Hololens;
using UnityEngine;

public class CollisionObjectH : ActionObject3DH
{
    public override string GetObjectTypeName() {
        return "Collision object";
    }

    public override async Task<bool> WriteLock(bool lockTree)
    {
        return await base.WriteLock(lockTree) && await WriteLockObjectType();
    }

    public override async Task<bool> WriteUnlock()
    {
        return await base.WriteUnlock() && await WriteUnlockObjectType();
    }

    public async Task<bool> WriteLockObjectType() {
        try {
            await WebSocketManagerH.Instance.WriteLock(ActionObjectMetadata.Type, false);
            LockByMe();
            return true;
        } catch (RequestFailedException) {
            return false;
        }
    }

    public async Task<bool> WriteUnlockObjectType() {
        try {
            await WebSocketManagerH.Instance.WriteUnlock(ActionObjectMetadata.Type);
            return true;
        } catch (RequestFailedException) {
            return false;
        }
    }
}
