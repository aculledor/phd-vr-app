using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquatScript : WallScript
{
    private const float OBSTACLE_OFFSET = 1.2f;

    [SerializeField]
    private GameObject squatDetection;
    [SerializeField]
    private GameObject rootElement;

    protected override void Start()
    {
        base.Start();
        transform.Translate(0, this.obstacleSpawner.squattingHeight - OBSTACLE_OFFSET, 0);
    }

    public override void WallFailed()
    {
        failFlag = true;
        triggerFailAnimation = true;
        squatDetection.SetActive(false);
        EventBus.PublishEvent(failureEvent);
    }

    public void NotifyIncorrectSquat()
    {
        failFlag = true;
        squatDetection.SetActive(false);
        EventBus.PublishEvent(failureEvent);
    }

    public override void DestroyObstacle()
    {
        if (!is_destroyed)
        {
            is_destroyed = true;
            if (!this.failFlag)
            {
                EventBus.PublishEvent(successEvent);
            }

            this.obstacleSpawner.RemoveObstacle(this.rootElement);
        }
    }
}
