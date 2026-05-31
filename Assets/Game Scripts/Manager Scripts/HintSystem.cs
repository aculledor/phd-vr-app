using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class HintSystem : MonoBehaviour, IObserver
{
    public Sprite arrowUp;
    public Sprite arrowDown;
    public Sprite punch;
    public Sprite move;

    public Image leftIcon;
    public Image rightIcon;

    private ObstacleType currentObstacle;
    public RoutineManager routineManager;
    public ObstacleSpawner obstacleSpawner;
    public PlayerTracker playerTracker;

    private void OnEnable()
    {
        this.leftIcon.enabled = false;
        this.rightIcon.enabled = false;
        EventBus.Subscribe(HandleRehabStart, GameEvent.START_REHAB);
        playerTracker.Register(this);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(HandleRehabStart, GameEvent.START_REHAB);
        playerTracker.Unregister(this);
    }


    private void HandleRehabStart()
    {
        StartCoroutine(ObstacleRoutine());
    }

    //Iterate through the routine to indicate how to react to the following obstacle
    private IEnumerator ObstacleRoutine()
    {
        FullRoutineItem[] items = routineManager.selectedRoutine.generatedRoutine;

        NotifyNewObstacle(items[0].obstacleElement);

        yield return new WaitForSeconds(obstacleSpawner.ObstacleArrivalTime);

        for(int i = 1; i < items.Length; i++)
        {
            NotifyNewObstacle(items[i].obstacleElement);
            yield return new WaitForSeconds(items[i - 1].timeUntilNext);

        }
    }


    //Update the sprite of the hint system based on the following obstacle.
    private void NotifyNewObstacle(ObstacleType item)
    {
        this.currentObstacle = item;

        switch (currentObstacle)
        {
            case ObstacleType.CROUCH_WALL:
                UpdateSquatStatus();
                this.rightIcon.enabled = true;
                this.leftIcon.enabled = true;
                break;
            case ObstacleType.STAND_WALL:
                this.leftIcon.sprite = move;
                this.rightIcon.sprite = move;
                this.rightIcon.enabled = true;
                this.leftIcon.enabled = true;
                break;
            case ObstacleType.LEFT_HIT:
            case ObstacleType.CROSS_RIGHT_HIT:
                this.leftIcon.sprite = punch;
                this.leftIcon.enabled = true;
                this.rightIcon.enabled = false;
                break;
            case ObstacleType.RIGHT_HIT:
            case ObstacleType.CROSS_LEFT_HIT:
                this.rightIcon.sprite = punch;
                this.rightIcon.enabled = true;
                this.leftIcon.enabled = false;
                break;

            }     
    }

    //Based on the squat state (the player stood up or not) it updates
    //the arrow pointing out which movement to perform.
    public void UpdateSquatStatus()
    {
        bool standing = this.playerTracker.standing;

        if (this.currentObstacle == ObstacleType.CROUCH_WALL) { 
            if (standing)
            {
                this.leftIcon.sprite = arrowDown;
                this.rightIcon.sprite = arrowDown;
            } else {
                this.leftIcon.sprite = arrowUp;
                this.rightIcon.sprite = arrowUp;
            }
        }
    }

    public void OnNotify()
    {
        UpdateSquatStatus();
    }
}
