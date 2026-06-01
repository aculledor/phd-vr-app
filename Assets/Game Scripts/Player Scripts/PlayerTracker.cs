using UnityEngine;

public class PlayerTracker : Subject
{
    [SerializeField]
    private AutoHandPlayerTracker bodyTracker;

    private Vector3 startingPosition;
    private float minHeight;
    public UserManager userManager;

    [SerializeField]
    private PunchStateDetectionScript punchStateDetectionScript;

    [System.NonSerialized]
    public bool standing;

    public bool ValidRightPunch
    {
        get { return punchStateDetectionScript.ValidRightPunch; }
    }

    public bool ValidLeftPunch
    {
        get { return punchStateDetectionScript.ValidLeftPunch; }
    }

    private void Awake()
    {
        ResolveBodyTracker();
    }

    private void Start()
    {
        minHeight = 0.0f;
        ValidateTrackingConfiguration();
        EventBus.Subscribe(ResetTrackingData, GameEvent.START_REHAB);
        EventBus.Subscribe(EnableStanding, GameEvent.START_REHAB);
        //userManager.Register(this);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe(ResetTrackingData, GameEvent.START_REHAB);
        EventBus.Unsubscribe(EnableStanding, GameEvent.START_REHAB);
        //userManager.Unregister(this);
    }

    private void EnableStanding()
    {
        standing = true;
    }

    public AutoHandPlayerTracker BodyTracker
    {
        get { return bodyTracker; }
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

            if (routineManager != null && routineManager.selectedRoutine is ServerRoutineData serverRoutine
                && serverRoutine.userHeightMeters > 0.0f)
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

    public void ResetTrackingData() {

        minHeight = ReferenceStandingHeight;
        startingPosition = PlayerPosition;
    }
    // Update is called once per frame
    void Update()
    {
        if(minHeight > PlayerPosition.y && this.standing)
        {
            minHeight = PlayerPosition.y;
        }

        Transform headTransform = GetTransform(TrackedBodyPart.Head);

        if (headTransform == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow)) {
            headTransform.position = new Vector3(0, 0.5f, 3);
        } else if (Input.GetKeyDown(KeyCode.UpArrow)) {
            headTransform.position = new Vector3(0, 1.5f, 3);
        } else if (Input.GetKeyDown(KeyCode.RightArrow)) {
            headTransform.position = new Vector3(-1.285f, 1.5f, 3);
        } else if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            headTransform.position = new Vector3(+1.285f, 1.5f, 3);
        }

    }


    public void ResetStandingState()
    {
        this.standing = true;
        this.NotifyObservers();
    }

    public void UpdateToSquatPerformedState()
    {
        this.standing = false;
        this.NotifyObservers();
    }

    private enum TrackedBodyPart
    {
        Head,
        LeftHand,
        RightHand
    }

    private AutoHandPlayerTracker ResolveBodyTracker()
    {
        if (bodyTracker != null)
        {
            return bodyTracker;
        }

        bodyTracker = GetComponent<AutoHandPlayerTracker>();

        if (bodyTracker != null)
        {
            return bodyTracker;
        }

        bodyTracker = FindObjectOfType<AutoHandPlayerTracker>();
        return bodyTracker;
    }

    private bool ValidateTrackingConfiguration()
    {
        ResolveBodyTracker();

        if (bodyTracker == null)
        {
            Debug.LogError("PlayerTracker necesita un AutoHandPlayerTracker asignado para leer cabeza y manos del rig Auto Hand/OpenXR.", this);
            return false;
        }

        return bodyTracker.ValidateConfiguration();
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
        ResolveBodyTracker();

        if (bodyTracker == null)
        {
            return null;
        }

        switch (bodyPart)
        {
            case TrackedBodyPart.Head:
                return bodyTracker.headTransform;
            case TrackedBodyPart.LeftHand:
                return bodyTracker.leftHandTransform;
            case TrackedBodyPart.RightHand:
                return bodyTracker.rightHandTransform;
            default:
                return null;
        }
    }

    private void LogMissingTracking(TrackedBodyPart bodyPart)
    {
        switch (bodyPart)
        {
            case TrackedBodyPart.Head:
                Debug.LogError("PlayerTracker no tiene headTransform configurado en AutoHandPlayerTracker.", this);
                break;
            case TrackedBodyPart.LeftHand:
                Debug.LogError("PlayerTracker no tiene leftHandTransform configurado en AutoHandPlayerTracker.", this);
                break;
            case TrackedBodyPart.RightHand:
                Debug.LogError("PlayerTracker no tiene rightHandTransform configurado en AutoHandPlayerTracker.", this);
                break;
        }
    }
}
