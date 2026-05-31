using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleDespawning : MonoBehaviour
{
    public Transform playerTransform;

    private void OnTriggerExit(Collider other)
    {
        IObstacleHandling handle = other.GetComponent<IObstacleHandling>();
        handle.DestroyObstacle();
    }

    //Updates the position of the collider to the position of the player.
    private void Update()
    {
        this.transform.position = this.playerTransform.position;
    }
}
