using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSquatDetection : MonoBehaviour
{

    [SerializeField]
    private SquatScript script;

    //If the player collides with the invisible box, the variable with the user information is updated.
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {

            if (ServiceLocator.Instance.PlayerTracker.standing)
            {
                ServiceLocator.Instance.PlayerTracker.UpdateToSquatPerformedState();
            } else
            {
                this.script.NotifyIncorrectSquat();
            }
        }
    }
}
