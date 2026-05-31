using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServiceLocator : MonoBehaviour
{

    [SerializeField]
    private RoutineManager routineManager;

    [SerializeField]
    private ObstacleSpawner obstacleSpawner;

    [SerializeField]
    private PlayerTracker playerTracker;

    public static ServiceLocator Instance { get; private set; }



    private void Awake()
    {
        Instance = this;
    }

    public RoutineManager RoutineManager
    {
        get { return this.routineManager; }
    }

    public ObstacleSpawner ObstacleSpawner
    {
        get { return this.obstacleSpawner; }
    }

    public PlayerTracker PlayerTracker
    {
        get { return this.playerTracker; }
    }
}
