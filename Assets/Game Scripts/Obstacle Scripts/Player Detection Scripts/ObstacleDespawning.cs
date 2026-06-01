using UnityEngine;

public class ObstacleDespawning : MonoBehaviour
{
    [SerializeField]
    private PlayerTracker playerTracker;
    public Transform playerTransform;

    private void Start()
    {
        if (playerTracker == null && ServiceLocator.Instance != null)
        {
            playerTracker = ServiceLocator.Instance.PlayerTracker;
        }
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
        if (playerTracker != null)
        {
            transform.position = playerTracker.PlayerPosition;
            return;
        }

        if (playerTransform == null)
        {
            Debug.LogError("ObstacleDespawning necesita PlayerTracker o playerTransform para seguir la cabeza del rig Auto Hand/OpenXR.", this);
            return;
        }

        transform.position = playerTransform.position;
    }
}
