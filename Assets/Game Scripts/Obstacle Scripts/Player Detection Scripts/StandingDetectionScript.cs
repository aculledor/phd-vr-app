using UnityEngine;

public class StandingDetectionScript : MonoBehaviour
{
    [SerializeField]
    private PlayerTracker playerTracker;

    private void Start()
    {
        ResolvePlayerTracker();
    }

    private void OnTriggerEnter(Collider other)
    {
        ResolvePlayerTracker();

        if (playerTracker == null)
        {
            Debug.LogError("StandingDetectionScript necesita PlayerTracker.", this);
            return;
        }

        if (other.CompareTag("MainCamera") || other.transform == playerTracker.transform)
        {
            playerTracker.ResetStandingState();
        }
    }

    private PlayerTracker ResolvePlayerTracker()
    {
        if (playerTracker != null)
        {
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

        return playerTracker;
    }
}