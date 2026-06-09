using UnityEngine;

public class PlayerTracker : Subject
{
    [Header("OpenXR / Auto Hand tracking references")]
    [SerializeField] private Transform headTransform;
    [SerializeField] private Transform leftHandTransform;
    [SerializeField] private Transform rightHandTransform;

    [Header("Project references")]
    [SerializeField] private UserManager userManager;
    [SerializeField] private PunchStateDetectionScript punchStateDetectionScript;

    private Vector3 startingPosition;
    private float minHeight;

    [System.NonSerialized]
    public bool standing;

    public bool ValidRightPunch
    {
        get
        {
            return punchStateDetectionScript != null && punchStateDetectionScript.ValidRightPunch;
        }
    }

    public bool ValidLeftPunch
    {
        get
        {
            return punchStateDetectionScript != null && punchStateDetectionScript.ValidLeftPunch;
        }
    }

    public Vector3 PlayerPosition
    {
        get { return GetPosition(TrackedBodyPart.Head); }
    }

    public Vector3 LControllerPosition
    {
        get { return GetPosition(TrackedBodyPart.LeftHand); }
    }

    public Vector3 RControllerPosition
    {
        get { return GetPosition(TrackedBodyPart.RightHand); }
    }

    public Quaternion HeadRotation
    {
        get { return GetRotation(TrackedBodyPart.Head); }
    }

    public Quaternion LeftHandRotation
    {
        get { return GetRotation(TrackedBodyPart.LeftHand); }
    }

    public Quaternion RightHandRotation
    {
        get { return GetRotation(TrackedBodyPart.RightHand); }
    }

    public float LeftHitDistance
    {
        get { return -LControllerPosition.z + PlayerPosition.z; }
    }

    public float RightHitDistance
    {
        get { return -RControllerPosition.z + PlayerPosition.z; }
    }

    public float HorizontalMovement
    {
        get { return -PlayerPosition.x + startingPosition.x; }
    }

    public float CurrentSquatHeight
    {
        get
        {
            float standingHeight = ReferenceStandingHeight;
            return standingHeight - Mathf.Min(standingHeight, PlayerPosition.y);
        }
    }

    public float SquatMinHeight
    {
        get { return ReferenceStandingHeight - minHeight; }
    }

    private float ReferenceStandingHeight
    {
        get
        {
            RoutineManager routineManager = ServiceLocator.Instance != null
                ? ServiceLocator.Instance.RoutineManager
                : null;

            if (routineManager != null &&
                routineManager.selectedRoutine is ServerRoutineData serverRoutine &&
                serverRoutine.userHeightMeters > 0.0f)
            {
                return serverRoutine.userHeightMeters;
            }

            if (userManager != null && userManager.activeUser != null)
            {
                return userManager.activeUser.standingHeight;
            }

            return PlayerPosition.y;
        }
    }

    private void Awake()
    {
        ResolveTrackingReferences();
    }

    private void Start()
    {
        minHeight = 0.0f;

        ValidateTrackingConfiguration();

        EventBus.Subscribe(ResetTrackingData, GameEvent.START_REHAB);
        EventBus.Subscribe(EnableStanding, GameEvent.START_REHAB);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe(ResetTrackingData, GameEvent.START_REHAB);
        EventBus.Unsubscribe(EnableStanding, GameEvent.START_REHAB);
    }

    private void Update()
    {
        if (standing && minHeight > PlayerPosition.y)
        {
            minHeight = PlayerPosition.y;
        }

#if UNITY_EDITOR
        // Solo para pruebas en editor sin gafas.
        if (headTransform == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            headTransform.position = new Vector3(0, 0.5f, 3);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            headTransform.position = new Vector3(0, 1.5f, 3);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            headTransform.position = new Vector3(-1.285f, 1.5f, 3);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            headTransform.position = new Vector3(+1.285f, 1.5f, 3);
        }
#endif
    }

    private void EnableStanding()
    {
        standing = true;
    }

    public void ResetTrackingData()
    {
        minHeight = ReferenceStandingHeight;
        startingPosition = PlayerPosition;
    }

    public void ResetStandingState()
    {
        standing = true;
        NotifyObservers();
    }

    public void UpdateToSquatPerformedState()
    {
        standing = false;
        NotifyObservers();
    }

    private enum TrackedBodyPart
    {
        Head,
        LeftHand,
        RightHand
    }

    private void ResolveTrackingReferences()
    {
        if (headTransform == null && Camera.main != null)
        {
            headTransform = Camera.main.transform;
        }

        /*
         * Las manos es mejor asignarlas manualmente desde el Inspector.
         * Auto Hand/OpenXR puede tener nombres distintos según el prefab:
         *
         * - LeftHand
         * - RightHand
         * - Left Hand Controller
         * - Right Hand Controller
         * - Hand Left
         * - Hand Right
         *
         * Si quieres, puedes añadir búsqueda automática por nombre, pero
         * para VR es más seguro asignarlas a mano.
         */
    }

    private bool ValidateTrackingConfiguration()
    {
        ResolveTrackingReferences();

        bool valid = true;

        if (headTransform == null)
        {
            Debug.LogError("PlayerTracker necesita headTransform. Asigna la Main Camera del rig OpenXR/Auto Hand.", this);
            valid = false;
        }

        if (leftHandTransform == null)
        {
            Debug.LogError("PlayerTracker necesita leftHandTransform. Asigna la mano/controlador izquierdo del rig Auto Hand.", this);
            valid = false;
        }

        if (rightHandTransform == null)
        {
            Debug.LogError("PlayerTracker necesita rightHandTransform. Asigna la mano/controlador derecho del rig Auto Hand.", this);
            valid = false;
        }

        return valid;
    }

    private Vector3 GetPosition(TrackedBodyPart bodyPart)
    {
        Transform trackedTransform = GetTransform(bodyPart);

        if (trackedTransform != null)
        {
            return trackedTransform.position;
        }

        LogMissingTracking(bodyPart);
        return Vector3.zero;
    }

    private Quaternion GetRotation(TrackedBodyPart bodyPart)
    {
        Transform trackedTransform = GetTransform(bodyPart);

        if (trackedTransform != null)
        {
            return trackedTransform.rotation;
        }

        LogMissingTracking(bodyPart);
        return Quaternion.identity;
    }

    private Transform GetTransform(TrackedBodyPart bodyPart)
    {
        switch (bodyPart)
        {
            case TrackedBodyPart.Head:
                return headTransform;

            case TrackedBodyPart.LeftHand:
                return leftHandTransform;

            case TrackedBodyPart.RightHand:
                return rightHandTransform;

            default:
                return null;
        }
    }

    private void LogMissingTracking(TrackedBodyPart bodyPart)
    {
        switch (bodyPart)
        {
            case TrackedBodyPart.Head:
                Debug.LogError("PlayerTracker no tiene headTransform configurado.", this);
                break;

            case TrackedBodyPart.LeftHand:
                Debug.LogError("PlayerTracker no tiene leftHandTransform configurado.", this);
                break;

            case TrackedBodyPart.RightHand:
                Debug.LogError("PlayerTracker no tiene rightHandTransform configurado.", this);
                break;
        }
    }
}