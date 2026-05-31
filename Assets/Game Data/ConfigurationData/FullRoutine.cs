using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class FullRoutine : IRoutineData
{
    public string routineId;
    public string name;
    public string description;
    public bool hasSquat;
    public bool hasMovement;
    public bool hasPunch;
    public bool hasCrossPunch;
    [SerializeField]
    private FullRoutineItem[] items;

    public int routineDuration
    {
        get
        {
            float obstacleArrival =  ServiceLocator.Instance.ObstacleSpawner.ObstacleArrivalTime;
            float totalTimeObstacles = 0.0f;

            foreach (FullRoutineItem item in items)
            {
                totalTimeObstacles += item.timeUntilNext;
            }

            return (int) Mathf.Ceil(obstacleArrival + totalTimeObstacles);
        }
    }

    public FullRoutineItem[] generatedRoutine => items;

    bool IRoutineData.allowSquat => hasSquat;

    bool IRoutineData.allowMovement => hasMovement;

    bool IRoutineData.allowPunch => hasPunch;

    bool IRoutineData.allowCrossPunch => hasCrossPunch;

    public static FullRoutine CreateFromJSON(string json)
    {
        return JsonUtility.FromJson<FullRoutine>(json);
    }

    // override object.Equals
    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        return routineId.Equals(((FullRoutine) obj).routineId);
    }

    public FullRoutineItem[] GenerateRoutine()
    {
        return this.items;
    }

    // override object.GetHashCode
    public override int GetHashCode()
    {
        return routineId.GetHashCode();
    }

}

[System.Serializable]
public class FullRoutineItem
{
    public ObstacleType obstacleElement;
    public ObstacleLane obstacleLane;
    public float timeUntilNext;
}