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

		Vector3 userPosition = userTransform.position;
		this.transform.position = new Vector3(userPosition.x, 0.0f, userPosition.z + offset);
	}


	private void Start()
	{
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
