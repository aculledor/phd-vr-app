using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StandingDetectionScript : MonoBehaviour
{
    // TODO : Rework del player tracker
    [SerializeField]
    private PlayerTracker playerTracker;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            playerTracker.ResetStandingState();
        }
    }
}
