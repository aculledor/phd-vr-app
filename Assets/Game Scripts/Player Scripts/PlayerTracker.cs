using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
public class PlayerTracker : Subject
{
    //Player Transforms
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

    public Vector3 PlayerPosition
    {
        get { return player.position; }
    }

    public Vector3 LControllerPosition
    {
        get { return leftController.position; }
    }

    public Vector3 RControllerPosition
    {
        get { return rightController.position; }
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

        if (Input.GetKeyDown(KeyCode.DownArrow)) {
            player.position = new Vector3(0, 0.5f, 3);
        } else if (Input.GetKeyDown(KeyCode.UpArrow)) {
            player.position = new Vector3(0, 1.5f, 3);
        } else if (Input.GetKeyDown(KeyCode.RightArrow)) {
            player.position = new Vector3(-1.285f, 1.5f, 3);
        } else if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            player.position = new Vector3(+1.285f, 1.5f, 3);
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
}
