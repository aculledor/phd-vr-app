using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetScript : MonoBehaviour, IObstacleHandling
{
    protected enum RelativePosition{
        RIGHT, LEFT
    } 
    
    public ObstacleType obstacle;
    public HitType acceptedHit;

    [SerializeField]
    protected RelativePosition targetOffset;

    public GameEvent successEvent;
    public GameEvent failureEvent;
    private float meshDisappearence;
    
    protected ObstacleSpawner obstacleSpawner;
    protected PlayerTracker playerTracker;
    private Material meshMaterial;

    protected bool hit;
    protected bool correctHit;
    protected void Start()
    {
        this.obstacleSpawner = ServiceLocator.Instance.ObstacleSpawner;
        this.playerTracker = ServiceLocator.Instance.PlayerTracker;
        this.meshDisappearence = 0.0f;
        this.hit = false;
        this.correctHit = true;
        this.meshMaterial = this.gameObject.GetComponentInChildren<MeshRenderer>().material;
        InitPosition();
    }

    private void Update()
    {
        if (hit && correctHit)
        {
            this.meshDisappearence += Time.deltaTime * 1.5f;
            this.meshMaterial.SetFloat("_Cutoff", this.meshDisappearence);
        }
    }

    public void DestroyObstacle()
    {
        if(!hit)
            PublishResultEvent(failureEvent);
        this.obstacleSpawner.RemoveObstacle(this.gameObject);
    }

    public void TakeHit(HitType playerHit)
    {

        if (!hit) {
            HandleHit(playerHit);
        }
    }

    public virtual void HandleHit(HitType playerHit) {

        if (this.acceptedHit.Equals(playerHit) && 
            (this.acceptedHit == HitType.RIGHT_HIT && playerTracker.ValidRightPunch ||
                this.acceptedHit == HitType.LEFT_HIT && playerTracker.ValidLeftPunch))
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

    protected void PublishResultEvent(GameEvent gameEvent)
    {
        ServerExerciseContext context = GetComponentInParent<ServerExerciseContext>();
        EventBus.PublishEvent(gameEvent, context != null ? context.RoutineItem : null);
    }

    protected virtual void InitPosition()
    {
        this.transform.position += new Vector3(targetOffset == RelativePosition.RIGHT
            ? this.obstacleSpawner.targetOffset
            : -this.obstacleSpawner.targetOffset
            , this.obstacleSpawner.targetHeight, 0);
    }

}
