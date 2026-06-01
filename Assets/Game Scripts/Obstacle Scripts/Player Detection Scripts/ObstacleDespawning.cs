using UnityEngine;

public class ObstacleDespawning : MonoBehaviour
{
    public Transform playerTransform;

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
        if (playerTransform == null)
        {
            Debug.LogWarning("ObstacleDespawning no tiene playerTransform asignado.");
            return;
        }

        transform.position = playerTransform.position;
    }
}