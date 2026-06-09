using UnityEngine;

public class PunchStateDetectionScript : MonoBehaviour
{
    [SerializeField]
    private Collider leftCollider;

    [SerializeField]
    private Collider rightCollider;

    [SerializeField]
    private UserManager userManager;

    [SerializeField]
    private PlayerTracker playerTracker;

    [Header("VR positioning")]
    [SerializeField]
    private bool followHeadHeight = false;

    [SerializeField]
    private float heightOffsetFromHead = -0.35f;

    private bool leftColliding;
    private bool rightColliding;
    private float offset;
    private bool missingPlayerTrackerLogged;

    public bool ValidRightPunch
    {
        get { return !leftColliding && rightColliding; }
    }

    public bool ValidLeftPunch
    {
        get { return leftColliding && !rightColliding; }
    }

    private void Start()
    {
        ResolvePlayerTracker();

        offset = 0.0f;
        leftColliding = false;
        rightColliding = false;

        EventBus.Subscribe(UpdateColliderOffset, GameEvent.START_REHAB);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe(UpdateColliderOffset, GameEvent.START_REHAB);
    }

    private void Update()
    {
        ResolvePlayerTracker();

        if (playerTracker == null)
        {
            if (!missingPlayerTrackerLogged)
            {
                Debug.LogError("PunchStateDetectionScript necesita PlayerTracker para seguir la cabeza del rig Auto Hand/OpenXR.", this);
                missingPlayerTrackerLogged = true;
            }

            return;
        }

        Vector3 userPosition = playerTracker.PlayerPosition;

        float targetY = followHeadHeight
            ? userPosition.y + heightOffsetFromHead
            : transform.position.y;

        transform.position = new Vector3(
            userPosition.x,
            targetY,
            userPosition.z + offset
        );
    }

    private void UpdateColliderOffset()
    {
        RoutineManager routineManager = ServiceLocator.Instance != null
            ? ServiceLocator.Instance.RoutineManager
            : null;

        if (routineManager != null && routineManager.selectedRoutine is ServerRoutineData)
        {
            offset = 0.0f;
            return;
        }

        offset = userManager != null && userManager.activeUser != null
            ? userManager.activeUser.punchZOffset
            : 0.0f;
    }

    private PlayerTracker ResolvePlayerTracker()
    {
        if (playerTracker != null)
        {
            missingPlayerTrackerLogged = false;
            return playerTracker;
        }

        if (ServiceLocator.Instance != null && ServiceLocator.Instance.PlayerTracker != null)
        {
            playerTracker = ServiceLocator.Instance.PlayerTracker;
        }

        if (playerTracker == null)
        {
            playerTracker = FindObjectOfType<PlayerTracker>();
        }

        if (playerTracker != null)
        {
            missingPlayerTrackerLogged = false;
        }

        return playerTracker;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsLeftHandCollider(other))
        {
            leftColliding = true;
        }
        else if (IsRightHandCollider(other))
        {
            rightColliding = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsLeftHandCollider(other))
        {
            leftColliding = false;
        }
        else if (IsRightHandCollider(other))
        {
            rightColliding = false;
        }
    }

    private bool IsLeftHandCollider(Collider other)
    {
        return IsSameColliderOrChild(other, leftCollider);
    }

    private bool IsRightHandCollider(Collider other)
    {
        return IsSameColliderOrChild(other, rightCollider);
    }

    private bool IsSameColliderOrChild(Collider other, Collider expectedCollider)
    {
        if (other == null || expectedCollider == null)
        {
            return false;
        }

        if (other == expectedCollider)
        {
            return true;
        }

        if (other.transform.IsChildOf(expectedCollider.transform))
        {
            return true;
        }

        if (expectedCollider.transform.IsChildOf(other.transform))
        {
            return true;
        }

        return false;
    }
}