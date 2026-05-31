using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[System.Serializable]
public class UserData
{
    //Callibration Info
    public float standingHeight;
    public float squattingHeight;
    public float targetXOffset;
    public float targetYOffset;
    public float punchZOffset;

    //Patient General Info
    public string userName;
    public string identifier;


    //Patient History
    public List<ScoreRecords> scoreHistory;
    public List<string> selectedRoutines;
    public int highestScore;

 

    public UserData(string userName, string identifier)
    {
        this.userName = userName;
        this.identifier = identifier;
        highestScore = 0;
        scoreHistory = new List<ScoreRecords>();
        selectedRoutines = new List<string>();
    }
}

[Serializable]
public struct ScoreRecords
{
    public int sessionDuration;
    public int maxCombo;
    public int highScore;
    public int squatCount;
    public int movementCount;
    public int punchCount;
    public int squatTotal;
    public int movementTotal;
    public int punchTotal;
    public DateTime time;

    public ScoreRecords(int sessionDuration, int maxCombo, int highScore, 
        int squatCount, int movementCount, int punchCount,
        int squatTotal, int movementTotal, int punchTotal, DateTime time)
    {
        this.sessionDuration = sessionDuration;
        this.maxCombo = maxCombo;
        this.highScore = highScore;
        this.squatCount = squatCount;
        this.movementCount = movementCount;
        this.punchCount = punchCount;
        this.squatTotal = squatTotal;
        this.movementTotal = movementTotal;
        this.punchTotal = punchTotal;
        this.time = time;
    }
}