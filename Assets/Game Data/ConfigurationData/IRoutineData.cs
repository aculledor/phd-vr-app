using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRoutineData
{
    public bool allowSquat { get; }
    public bool allowMovement { get;}
    public bool allowPunch { get;}
    public bool allowCrossPunch { get;}
    public int routineDuration { get;}
    public FullRoutineItem[] GenerateRoutine();
    public FullRoutineItem[] generatedRoutine { get; }
}
