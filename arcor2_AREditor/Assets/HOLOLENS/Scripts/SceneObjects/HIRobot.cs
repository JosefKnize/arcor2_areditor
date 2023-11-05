using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public interface HIRobot
{
    string GetName();

    string GetId();

    Task<List<string>> GetEndEffectorIds(string arm_id = null);

    Task<List<string>> GetArmsIds();

    Task<HRobotEE> GetEE(string ee_id, string arm_id);

    Task<List<HRobotEE>> GetAllEE();

    bool HasUrdf();

    void SetJointValue(List<IO.Swagger.Model.Joint> joints, bool angle_in_degrees = false, bool forceJointsValidCheck = false);

    void SetJointValue(string name, float angle, bool angle_in_degrees = false);

    List<IO.Swagger.Model.Joint> GetJoints();

    void SetGrey(bool grey, bool force = false);

    Transform GetTransform();

    bool MultiArm();

    Task<bool> WriteLock(bool lockTree);

    Task<bool> WriteUnlock();

    string LockOwner();

    HInteractiveObject GetInteractiveObject();
}
