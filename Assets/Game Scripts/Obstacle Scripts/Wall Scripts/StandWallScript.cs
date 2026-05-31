using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StandWallScript : WallScript
{
    private const float OBSTACLE_OFFSET = 1.7f;

    protected override void Start()
    {
        base.Start();
        transform.Translate(0, this.obstacleSpawner.standingHeight - OBSTACLE_OFFSET, 0);
    }

    public override void WallFailed()
    {
        triggerFailAnimation = true;
        EventBus.PublishEvent(failureEvent);
    }

    public override void DestroyObstacle()
    {
        if (!is_destroyed)
        {
            is_destroyed = true;
            if (!this.triggerFailAnimation)
            {
                EventBus.PublishEvent(successEvent);
            }

            this.obstacleSpawner.RemoveObstacle(this.gameObject);
        }
    }
}
