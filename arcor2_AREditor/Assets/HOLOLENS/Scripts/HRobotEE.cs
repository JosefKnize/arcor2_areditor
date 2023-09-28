using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Base;
using RosSharp.Urdf;
using UnityEngine;

public class HRobotEE : HInteractiveObject, HISubItem
{
       [SerializeField]
    private TMPro.TMP_Text eeName;
    //public OutlineOnClick OutlineOnClick;
    public string EEId, ARMId;
    public HIRobot Robot;

    private void Awake() {
        SceneManagerH.Instance.OnRobotSelected += OnRobotSelected;
    }

    private void OnDestroy() {
        SceneManagerH.Instance.OnRobotSelected -= OnRobotSelected;
    }

    private void OnRobotSelected(object sender, System.EventArgs e) {
       /* if (SceneManagerH.Instance.SelectedEndEffector == this) {
            OutlineOnClick.Select();
        } else {
            OutlineOnClick.Deselect();
        }*/
    }

    public void InitEE(HIRobot robot, string armId, string eeId) {
        Robot = robot;
        ARMId = armId;
        EEId = eeId;
        UpdateLabel();
     //   SelectorItem = SelectorMenu.Instance.CreateSelectorItem(this);

        
    }

    public void UpdateLabel() {
        if (Robot.MultiArm())
            eeName.text = $"{Robot.GetName()}/{ARMId}/{EEId}";
        else
            eeName.text = $"{Robot.GetName()}/{EEId}";
    }

    public bool IsSelected => SceneManagerH.Instance.SelectedEndEffector == this;



    /// <summary>
    /// Takes world space pose of end effector, converts them to SceneOrigin frame and apply to RobotEE
    /// </summary>
    /// <param name="position">Position in world frame</param>
    /// <param name="orientation">Orientation in world frame</param>
    public void UpdatePosition(Vector3 position, Quaternion orientation) {
        transform.position = SceneManagerH.Instance.SceneOrigin.transform.TransformPoint(position);
        // rotation set according to this
        // https://answers.unity.com/questions/275565/what-is-the-rotation-equivalent-of-inversetransfor.html
        transform.rotation = GameManagerH.Instance.Scene.transform.rotation * orientation;
    }

    public override string GetName() {
        if (Robot.MultiArm())
            return $"{ARMId}/{EEId}";
        else
            return EEId;
    }

    public override string GetId() {
        return $"{Robot.GetId()}/{ARMId}/{EEId}";
    }

    public async override Task<RequestResult> Movable() {
        return new RequestResult(false, "Robot EE could not be moved at the moment (will be added in next release)");
    }

    public override void StartManipulation() {
        throw new System.NotImplementedException();
    }

    public async override Task<RequestResult> Removable() {
        return new RequestResult(false, "Robot EE could not be removed");
    }

    public override void Remove() {
        throw new System.NotImplementedException();
    }

    public override Task Rename(string name) {
        throw new System.NotImplementedException();
    }

    public override string GetObjectTypeName() {
        return "End effector";
    }

    public override void UpdateColor() {
        //nothing to do here
    }

    public HInteractiveObject GetParentObject() {
        try {
            return SceneManagerH.Instance.GetActionObject(Robot.GetId());
        } catch (KeyNotFoundException) {
            return null;
        }
    }


    public override void EnableVisual(bool enable) {
        throw new System.NotImplementedException();
    }
}
