using System.Collections;
using UnityEngine;
using UnityEngine.XR;

public class ControllerVibrationSystem : MonoBehaviour
{

    private enum _controls
    {
        LEFT,
        RIGHT,
        BOTH
    }

    private const float vibrationAmplitude = 0.25f;
    private const float vibrationDuration = 0.5f;

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


    public void HandleLeftControllerVibration()
    {
        SendHapticImpulse(XRNode.LeftHand, vibrationAmplitude, vibrationDuration);
        StartCoroutine(StopControllerVibration(_controls.LEFT));
    }

    public void HandleRightControllerVibration()
    {
        SendHapticImpulse(XRNode.RightHand, vibrationAmplitude, vibrationDuration);
        StartCoroutine(StopControllerVibration(_controls.RIGHT));
    }

    public void HandleBothControllersVibration()
    {
        SendHapticImpulse(XRNode.RightHand, vibrationAmplitude, vibrationDuration);
        SendHapticImpulse(XRNode.LeftHand, vibrationAmplitude, vibrationDuration);
        StartCoroutine(StopControllerVibration(_controls.BOTH));
    }

    private IEnumerator StopControllerVibration(_controls value)
    {
        yield return new WaitForSeconds(vibrationDuration);

        if (value != _controls.LEFT)
        {
            StopHaptics(XRNode.RightHand);
        }

        if (value != _controls.RIGHT)
        {
            StopHaptics(XRNode.LeftHand);
        }
    }

    private void SendHapticImpulse(XRNode node, float amplitude, float duration)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);

        if (!device.isValid)
        {
            Debug.LogWarning($"No se pudo enviar vibración: el controlador OpenXR {node} no está disponible.", this);
            return;
        }

        if (!device.TryGetHapticCapabilities(out HapticCapabilities capabilities) || !capabilities.supportsImpulse)
        {
            Debug.LogWarning($"No se pudo enviar vibración: el controlador OpenXR {node} no soporta impulsos hápticos.", this);
            return;
        }

        device.SendHapticImpulse(0, amplitude, duration);
    }

    private void StopHaptics(XRNode node)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);

        if (!device.isValid)
        {
            return;
        }

        device.StopHaptics();
    }
}
