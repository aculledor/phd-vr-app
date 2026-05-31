using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public enum ObstacleType
{
    CROUCH_WALL,
    STAND_WALL,
    LEFT_HIT,
    RIGHT_HIT,
    CROSS_LEFT_HIT,
    CROSS_RIGHT_HIT
}
public enum ObstacleLane
{
    LEFT_LANE,
    MID_LANE,
    RIGHT_LANE
}
public interface IObstacleHandling
{
    public abstract void DestroyObstacle();
}


