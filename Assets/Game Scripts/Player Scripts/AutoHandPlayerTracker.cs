using UnityEngine;

/// <summary>
/// Centralizes the body tracking references provided by the active Auto Hand/OpenXR rig.
/// Assign the head/camera transform and both Auto Hand hand transforms in the inspector.
/// </summary>
public class AutoHandPlayerTracker : MonoBehaviour
{
    public Transform headTransform;
    public Transform leftHandTransform;
    public Transform rightHandTransform;

    public Vector3 HeadPosition => HasHeadTracking ? headTransform.position : Vector3.zero;
    public Vector3 LeftHandPosition => HasLeftHandTracking ? leftHandTransform.position : Vector3.zero;
    public Vector3 RightHandPosition => HasRightHandTracking ? rightHandTransform.position : Vector3.zero;

    public Quaternion HeadRotation => HasHeadTracking ? headTransform.rotation : Quaternion.identity;
    public Quaternion LeftHandRotation => HasLeftHandTracking ? leftHandTransform.rotation : Quaternion.identity;
    public Quaternion RightHandRotation => HasRightHandTracking ? rightHandTransform.rotation : Quaternion.identity;

    public bool HasHeadTracking => headTransform != null;
    public bool HasLeftHandTracking => leftHandTransform != null;
    public bool HasRightHandTracking => rightHandTransform != null;
    public bool IsFullyConfigured => HasHeadTracking && HasLeftHandTracking && HasRightHandTracking;

    private void Awake()
    {
        ValidateConfiguration();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        ValidateConfiguration();
    }

    public bool ValidateConfiguration()
    {
        bool configured = true;

        if (!HasHeadTracking)
        {
            Debug.LogError("AutoHandPlayerTracker no tiene headTransform asignado. Asigna la cámara/cabeza del rig Auto Hand/OpenXR.", this);
            configured = false;
        }

        if (!HasLeftHandTracking)
        {
            Debug.LogError("AutoHandPlayerTracker no tiene leftHandTransform asignado. Asigna la mano izquierda del rig Auto Hand/OpenXR.", this);
            configured = false;
        }

        if (!HasRightHandTracking)
        {
            Debug.LogError("AutoHandPlayerTracker no tiene rightHandTransform asignado. Asigna la mano derecha del rig Auto Hand/OpenXR.", this);
            configured = false;
        }

        return configured;
    }
}
