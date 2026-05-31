using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class ResultsMenuManager : MonoBehaviour
{
    public ScoreSystem scoreSystem;
    
    public CounterIncreasingScript movementText;
    public CounterIncreasingScript punchText;
    public CounterIncreasingScript squatText;
    public CounterIncreasingScript comboText;
    public CounterIncreasingScript scoreText;


    public void OnEnable()
    {
        this.movementText.SetCounterValue(scoreSystem.movementSuccess);
        this.movementText.additionalText = $" de {scoreSystem.movementTotal}";
        this.punchText.SetCounterValue(scoreSystem.punchSuccess);
        this.punchText.additionalText = $" de {scoreSystem.punchTotal}";
        this.squatText.SetCounterValue(scoreSystem.squatSuccess);
        this.squatText.additionalText = $" de {scoreSystem.squatTotal}";
        this.comboText.SetCounterValue(scoreSystem.comboMax);
        this.scoreText.SetCounterValue(scoreSystem.score);
    }

}
