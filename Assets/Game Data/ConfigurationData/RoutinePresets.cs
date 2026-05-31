using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Routine Preset", menuName = "Scriptable Objects/RoutinePresets", order = 1)]
public class RoutinePresets : ScriptableObject, IRoutineData
{
    public string identifier;
    public bool allowSquat;
    public bool allowMovement;
    public bool allowPunch;
    public bool allowCrossPunch;
    //number of seconds
    public int routineDuration;
    public DifficultyLevel difficultyLevel;

    private FullRoutineItem[] items;
    
    //Dictionary where we establish a correspondance between the difficulty level and the time between
    //obstacles.
    private static Dictionary<DifficultyLevel, float> spawnTime = new Dictionary<DifficultyLevel, float>() 
        {
            { DifficultyLevel.EASY, 7.0f}, { DifficultyLevel.MODERATE, 5.0f},
            { DifficultyLevel.HARD, 3.0f},
        };

    bool IRoutineData.allowSquat { get => allowSquat;}
    bool IRoutineData.allowMovement { get => allowMovement; }
    bool IRoutineData.allowPunch { get => allowPunch;}
    bool IRoutineData.allowCrossPunch { get => allowCrossPunch;}
    int IRoutineData.routineDuration { get => routineDuration;}

    public FullRoutineItem[] generatedRoutine => items;

    public RoutinePresets Clone()
    {
        return (RoutinePresets) this.MemberwiseClone();
    }

    public FullRoutineItem[] GenerateRoutine()
    {

        float accumulatedTime = 0;
        List<FullRoutineItem> routine = new List<FullRoutineItem>();
        //Filter all the subroutines which use unselected obstacles.
        List<SubRoutines> unusedRoutines = ServiceLocator.Instance.ObstacleSpawner
            .subroutines.FindAll((elem) =>
             ValidExercise(elem.hasMovement, allowMovement) &&
             ValidExercise(elem.hasPunch, allowPunch) &&
             ValidExercise(elem.hasCrouch, allowSquat) &&
             ValidExercise(elem.hasCrossPunch, allowCrossPunch));


        List<SubRoutines> alreadyUsed = new List<SubRoutines>();
        float durationComparison = routineDuration;

        //The time spent exercising, the obstacles should fill the whole session time, even if it exceeds
        //the session time.
        while (accumulatedTime <= durationComparison) {

            SubRoutines selected;

            if (unusedRoutines.Count == 0)
            {
                List<SubRoutines> temp;
                temp = unusedRoutines;
                unusedRoutines = alreadyUsed;
                alreadyUsed = temp;
            }

            int index = Random.Range(0, unusedRoutines.Count);

            selected = unusedRoutines[index];

            //Add each subroutine item to the session list
            foreach (RoutineItem item in selected.subroutine)
            {
                FullRoutineItem routineComponent = new FullRoutineItem();
                routineComponent.obstacleElement = item.obstacleType;
                routineComponent.obstacleLane = item.obstacleLane;
                routineComponent.timeUntilNext = spawnTime[difficultyLevel];

                routine.Add(routineComponent);
            }

            //Add the time which will be spent dodging the obstacles.
            accumulatedTime += spawnTime[difficultyLevel] * selected.subroutine.Count;
            alreadyUsed.Add(selected);
            unusedRoutines.RemoveAt(index);
        }


        //Transform the list into an array and return
        items = routine.ToArray();
        return items;
    }

    private bool ValidExercise(bool cond_1, bool cond_2)
    {
        return (!cond_1 || cond_2);
    }
}

[System.Serializable]
public struct RoutineItem
{
    public ObstacleType obstacleType;
    public ObstacleLane obstacleLane;
}