using UnityEngine;

public class ObstacleDespawning : MonoBehaviour
{
    [SerializeField]
    private PlayerTracker playerTracker;
    private bool missingPlayerTrackerLogged;

    private void Start()
    {
        ResolvePlayerTracker();
    }


    private void OnTriggerExit(Collider other)
    {
        IObstacleHandling handle = other.GetComponentInParent<IObstacleHandling>();

        if (handle == null)
        {
            Debug.LogWarning($"El objeto {other.name} salió del trigger, pero no tiene IObstacleHandling.");
            return;
        }

        handle.DestroyObstacle();
    }

    private void Update()
    {
        ResolvePlayerTracker();

        if (playerTracker == null)
        {
            if (!missingPlayerTrackerLogged)
            {
                Debug.LogError("ObstacleDespawning necesita PlayerTracker para seguir la cabeza del rig Auto Hand/OpenXR.", this);
                missingPlayerTrackerLogged = true;
            }

            return;
        }

        transform.position = playerTracker.PlayerPosition;
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
