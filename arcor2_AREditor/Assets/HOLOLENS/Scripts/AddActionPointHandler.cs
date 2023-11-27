using UnityEngine;
using Base;
using UnityEngine.InputSystem;
using MixedReality.Toolkit;
using UnityEngine.XR;
using MixedReality.Toolkit.Subsystems;

public class AddActionPointHandler : Singleton<AddActionPointHandler>
{

    private bool isFocused = false;
    private bool registered = false;

    public GameObject actionPointPrefab;

    [SerializeField]
    private InputActionReference leftHandReference;
    [SerializeField]
    private InputActionReference rightHandReference;



    void Update()
    {
        ProcessHand(XRNode.LeftHand);
        ProcessHand(XRNode.RightHand);
    }

    void ProcessHand(XRNode hand)
    {
        if (registered)
        {
            var aggregator = XRSubsystemHelpers.GetFirstRunningSubsystem<HandsAggregatorSubsystem>();
            if (aggregator.TryGetJoint(TrackedHandJoint.IndexTip, hand, out HandJointPose pose))
            {
                actionPointPrefab.GetComponent<Renderer>().enabled = true;
                actionPointPrefab.transform.position = pose.Position;
            }
            else
            {
                actionPointPrefab.GetComponent<Renderer>().enabled = false;
            }
        }
    }

    public void registerHandlers(bool register = true)
    {
        registered = register;
        leftHandReference.action.performed += HandActionPerformed;
        rightHandReference.action.performed += HandActionPerformed;
    }



    public void unregisterHandlers()
    {
        if (registered)
        {
            actionPointPrefab.GetComponent<Renderer>().enabled = false;
        }
        registered = false;

        leftHandReference.action.performed -= HandActionPerformed;
        rightHandReference.action.performed -= HandActionPerformed;
    }


    private void HandActionPerformed(InputAction.CallbackContext obj)
    {

    }

    //void IMixedRealityPointerHandler.OnPointerClicked(MixedRealityPointerEventData eventData)
    //{
    //    GameObject targetObject = eventData.Pointer.Result.CurrentPointerTarget;
    //    if (targetObject == null || targetObject.name.Contains("SpatialMesh"))
    //    {
    //        HSelectorManager.Instance.OnSelectObject(null);
    //    }
    //    /*  if(!isFocused){
    //          HSelectorManager.Instance.OnSelectObject(null);
    //      }*/
    //}

}
