using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PlayerTracker : Subject
{
    [SerializeField]
    private AutoHandPlayerTracker bodyTracker;

    // Optional fallback transforms kept to support existing scenes while they are migrated
    // to assign the AutoHandPlayerTracker reference directly.
    public Transform player;
    public Transform leftController;
    public Transform rightController;

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
        get { return GetRequiredTransform(TrackedBodyPart.Head).position; }
    }

    public Vector3 LControllerPosition
    {
        get { return GetRequiredTransform(TrackedBodyPart.LeftHand).position; }
    }

    public Vector3 RControllerPosition
    {
        get { return GetRequiredTransform(TrackedBodyPart.RightHand).position; }
    }

    public Quaternion HeadRotation
    {
        get { return GetRequiredTransform(TrackedBodyPart.Head).rotation; }
    }

    public Quaternion LeftHandRotation
    {
        get { return GetRequiredTransform(TrackedBodyPart.LeftHand).rotation; }
    }

    public Quaternion RightHandRotation
    {
        get { return GetRequiredTransform(TrackedBodyPart.RightHand).rotation; }
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

        Transform headTransform = GetOptionalHeadTransform();

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

    private bool ValidateTrackingConfiguration()
    {
        bool valid = true;

        if (bodyTracker != null)
        {
            valid &= bodyTracker.ValidateConfiguration();
        }

        if (GetTransform(TrackedBodyPart.Head) == null)
        {
            Debug.LogError("PlayerTracker no tiene tracking de cabeza configurado. Asigna un AutoHandPlayerTracker con headTransform o el campo legacy player.", this);
            valid = false;
        }

        if (GetTransform(TrackedBodyPart.LeftHand) == null)
        {
            Debug.LogError("PlayerTracker no tiene tracking de mano izquierda configurado. Asigna un AutoHandPlayerTracker con leftHandTransform o el campo legacy leftController.", this);
            valid = false;
        }

        if (GetTransform(TrackedBodyPart.RightHand) == null)
        {
            Debug.LogError("PlayerTracker no tiene tracking de mano derecha configurado. Asigna un AutoHandPlayerTracker con rightHandTransform o el campo legacy rightController.", this);
            valid = false;
        }

        return valid;
    }

    private Transform GetRequiredTransform(TrackedBodyPart bodyPart)
    {
        Transform trackedTransform = GetTransform(bodyPart);

        if (trackedTransform != null)
        {
            return trackedTransform;
        }

        ValidateTrackingConfiguration();
        return transform;
    }

    private Transform GetOptionalHeadTransform()
    {
        Transform headTransform = GetTransform(TrackedBodyPart.Head);

        if (headTransform == null)
        {
            Debug.LogError("PlayerTracker no puede actualizar la posición de depuración porque falta el headTransform.", this);
        }

        return headTransform;
    }

    private Transform GetTransform(TrackedBodyPart bodyPart)
    {
        if (bodyTracker != null)
        {
            switch (bodyPart)
            {
                case TrackedBodyPart.Head:
                    if (bodyTracker.headTransform != null)
                    {
                        return bodyTracker.headTransform;
                    }
                    break;
                case TrackedBodyPart.LeftHand:
                    if (bodyTracker.leftHandTransform != null)
                    {
                        return bodyTracker.leftHandTransform;
                    }
                    break;
                case TrackedBodyPart.RightHand:
                    if (bodyTracker.rightHandTransform != null)
                    {
                        return bodyTracker.rightHandTransform;
                    }
                    break;
            }
        }

        switch (bodyPart)
        {
            case TrackedBodyPart.Head:
                return player;
            case TrackedBodyPart.LeftHand:
                return leftController;
            case TrackedBodyPart.RightHand:
                return rightController;
            default:
                return null;
        }
    }

}
