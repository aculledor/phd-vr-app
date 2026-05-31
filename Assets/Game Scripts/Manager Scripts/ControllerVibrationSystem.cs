using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.Oculus;


public class ControllerVibrationSystem : MonoBehaviour
{

    private enum _controls
    {
        LEFT,
        RIGHT,
        BOTH
    }

    // Start is called before the first frame update
    public void OnEnable()
    {
        EventBus.Subscribe(HandleLeftControllerVibration, GameEvent.LEFT_HIT_SUCCESS, GameEvent.CROSS_RIGHT_HIT_SUCCESS);
        EventBus.Subscribe(HandleRightControllerVibration, GameEvent.RIGHT_HIT_SUCCESS, GameEvent.CROSS_LEFT_HIT_SUCCESS);
        EventBus.Subscribe(HandleBothControllersVibration, GameEvent.SQUAT_MID_FAILED, GameEvent.MOVEMENT_MID_FAILED,
            GameEvent.SQUAT_RIGHT_FAILED, GameEvent.SQUAT_LEFT_FAILED, 
            GameEvent.MOVEMENT_LEFT_FAILED, GameEvent.MOVEMENT_RIGHT_FAILED);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(HandleLeftControllerVibration, GameEvent.LEFT_HIT_SUCCESS);
        EventBus.Unsubscribe(HandleRightControllerVibration, GameEvent.RIGHT_HIT_SUCCESS);
        EventBus.Unsubscribe(HandleBothControllersVibration, GameEvent.SQUAT_MID_FAILED, GameEvent.MOVEMENT_MID_FAILED,
            GameEvent.SQUAT_RIGHT_FAILED, GameEvent.SQUAT_LEFT_FAILED,
            GameEvent.MOVEMENT_LEFT_FAILED, GameEvent.MOVEMENT_RIGHT_FAILED);
    }


    public  void HandleLeftControllerVibration()
    {
        OVRInput.SetControllerVibration(0.25f, 0.25f, OVRInput.Controller.LTouch);
        StartCoroutine(StopControllerVibration(_controls.LEFT));
    }

    public void HandleRightControllerVibration()
    {
        OVRInput.SetControllerVibration(0.25f, 0.25f, OVRInput.Controller.RTouch);
        StartCoroutine(StopControllerVibration(_controls.RIGHT));
    }

    public void HandleBothControllersVibration()
    {

        OVRInput.SetControllerVibration(0.25f, 0.25f, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(0.25f, 0.25f, OVRInput.Controller.LTouch);
        StartCoroutine(StopControllerVibration(_controls.BOTH));
    }

    private IEnumerator StopControllerVibration(_controls value)
    {
        yield return new WaitForSeconds(0.5f);
        if(value != _controls.LEFT)
            OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
        if(value != _controls.RIGHT)
            OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
    }

}
