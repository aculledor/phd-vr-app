using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Subroutine", menuName = "Scriptable Objects/Subroutine")]
public class SubRoutines : ScriptableObject
{
    public bool hasMovement;
    public bool hasPunch;
    public bool hasCrouch;
    public bool hasCrossPunch;

    public List<RoutineItem> subroutine;

}


public enum DifficultyLevel
{
    EASY,
    NORMAL,
    MODERATE,
    HARD
}