using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleMovementScript : MonoBehaviour
{

    private ObstacleSpawner obstacleSpawner;
    private float Speed
    {
        get { return this.obstacleSpawner.movementSpeed; }
    }

    void Start()
    {
        this.obstacleSpawner = ServiceLocator.Instance.ObstacleSpawner;
    }

    void FixedUpdate()
    {
        this.transform.Translate(this.transform.forward * Time.fixedDeltaTime * Speed);
    }
}
