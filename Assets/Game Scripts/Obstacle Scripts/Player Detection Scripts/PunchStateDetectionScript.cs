using System.Collections;
using System.Collections.Generic;
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
	[SerializeField]
	private Transform userTransform;
	
	private bool leftColliding;
    private bool rightColliding;
	private float offset;
	
	//Verifies the correctness of the action.
	public bool ValidRightPunch {
		get
		{
			return (!leftColliding && rightColliding);
		}
	}

	public bool ValidLeftPunch
    {
        get
        {
			return (leftColliding && !rightColliding);
        }
    }

	//Repositions every frame the collider to a given distance in front of the player
	private void Update()
	{

		if (playerTracker == null && ServiceLocator.Instance != null)
		{
			playerTracker = ServiceLocator.Instance.PlayerTracker;
		}

		Vector3 userPosition;

		if (playerTracker != null)
		{
			userPosition = playerTracker.PlayerPosition;
		}
		else if (userTransform != null)
		{
			userPosition = userTransform.position;
		}
		else
		{
			Debug.LogError("PunchStateDetectionScript necesita PlayerTracker o userTransform para seguir la cabeza del rig Auto Hand/OpenXR.", this);
			return;
		}

		this.transform.position = new Vector3(userPosition.x, 0.0f, userPosition.z + offset);
	}


	private void Start()
	{
		if (playerTracker == null && ServiceLocator.Instance != null)
		{
			playerTracker = ServiceLocator.Instance.PlayerTracker;
		}

		offset = 0.0f;
		leftColliding = false;
		rightColliding = false;
		EventBus.Subscribe(UpdateColliderOffset, GameEvent.START_REHAB);
	}

	private void OnDestroy()
	{
		EventBus.Unsubscribe(UpdateColliderOffset, GameEvent.START_REHAB);
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

	//Detects which element collided with the box
	private void OnTriggerEnter(Collider other)
	{

		if (other == leftCollider)
		{
			leftColliding = true;
		}
		else if(other == rightCollider)
		{
			rightColliding = true;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other == leftCollider)
		{
			leftColliding = false;
		}
		else if (other == rightCollider)
		{
			rightColliding = false;
		}
	}

}
