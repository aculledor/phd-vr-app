using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrossTargetScript : TargetScript
{

    private float heightOffset = 0.2f;

    // Start is called before the first frame update
    public override void HandleHit(HitType playerHit)
    {
        if (this.acceptedHit.Equals(playerHit))
        {
            PublishResultEvent(successEvent);
        }
        else
        {
            PublishResultEvent(failureEvent);
            correctHit = false;
        }
        this.hit = true;
    }

	protected override void InitPosition()
	{

        this.transform.position += new Vector3(targetOffset == RelativePosition.RIGHT
            ? this.obstacleSpawner.targetOffset - 0.1f
            : -this.obstacleSpawner.targetOffset + 0.1f
            , this.obstacleSpawner.targetHeight + heightOffset , 0);


    }
}
