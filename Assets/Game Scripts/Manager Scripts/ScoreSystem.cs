using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class ScoreSystem : MonoBehaviour
{

    //Score Showcase
    public TextMeshProUGUI scoreTextMesh;

    ///Combo tracker
    public TextMeshProUGUI comboText;
    public Image progressBar;

    private int currentTier;

    public int scoreAdditionPerSuccess;

    [SerializeField]
    private List<int> comboThreshold;
    [SerializeField]
    private List<int> multiplierTiers;
    private int currentCombo;
    private int comboTotal;
    [System.NonSerialized]
    public int comboMax;

    [System.NonSerialized]
    public int squatSuccess;
    [System.NonSerialized]
    public int movementSuccess;
    [System.NonSerialized]
    public int punchSuccess;

    [System.NonSerialized]
    public int squatTotal;
    [System.NonSerialized]
    public int movementTotal;
    [System.NonSerialized]
    public int punchTotal;

    private float currentFill;
    [System.NonSerialized]
    public int score;
    public CounterIncreasingScript counterScript;

    private void OnEnable()
    {
        ResetStats();
        //Success Handling
        EventBus.Subscribe(HandlePunchSuccess, GameEvent.RIGHT_HIT_SUCCESS, GameEvent.LEFT_HIT_SUCCESS,
            GameEvent.CROSS_LEFT_HIT_SUCCESS, GameEvent.CROSS_RIGHT_HIT_SUCCESS);
        EventBus.Subscribe(HandleSquatSuccess, GameEvent.SQUAT_MID_SUCCESS, GameEvent.SQUAT_LEFT_SUCCESS, GameEvent.SQUAT_RIGHT_SUCCESS);
        EventBus.Subscribe(HandleMovementSuccess, GameEvent.MOVEMENT_LEFT_SUCCESS, GameEvent.MOVEMENT_MID_SUCCESS, GameEvent.MOVEMENT_RIGHT_SUCCESS);

        //Failure Handling
        EventBus.Subscribe(HandleSquatFailure, GameEvent.SQUAT_LEFT_FAILED, GameEvent.SQUAT_MID_FAILED,
            GameEvent.SQUAT_RIGHT_FAILED);
        EventBus.Subscribe(HandleMovementFailure, GameEvent.MOVEMENT_LEFT_FAILED, GameEvent.MOVEMENT_MID_FAILED,
            GameEvent.MOVEMENT_RIGHT_FAILED);
        EventBus.Subscribe(HandlePunchFailure, GameEvent.RIGHT_HIT_FAILED, GameEvent.LEFT_HIT_FAILED,
            GameEvent.CROSS_LEFT_HIT_FAILED, GameEvent.CROSS_RIGHT_HIT_FAILED);

    }
    private void Update()
    {

        if(currentTier < comboThreshold.Count)
        {
            float fillNeeded = (currentCombo +1.0f) / comboThreshold[currentTier];

            if(currentFill < 1.0f && currentFill < fillNeeded)
            {
                currentFill = Mathf.Clamp01(Time.deltaTime + currentFill);
                this.progressBar.fillAmount = currentFill;
            }
        }

    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(HandlePunchSuccess, GameEvent.RIGHT_HIT_SUCCESS, GameEvent.LEFT_HIT_SUCCESS,
            GameEvent.CROSS_LEFT_HIT_SUCCESS, GameEvent.CROSS_RIGHT_HIT_SUCCESS);
        EventBus.Unsubscribe(HandleSquatSuccess, GameEvent.SQUAT_MID_SUCCESS, GameEvent.SQUAT_LEFT_SUCCESS, GameEvent.SQUAT_RIGHT_SUCCESS);
        EventBus.Unsubscribe(HandleMovementSuccess, GameEvent.MOVEMENT_LEFT_SUCCESS, GameEvent.MOVEMENT_MID_SUCCESS, GameEvent.MOVEMENT_RIGHT_SUCCESS);

        EventBus.Unsubscribe(HandleSquatFailure, GameEvent.SQUAT_LEFT_FAILED, GameEvent.SQUAT_MID_FAILED,
            GameEvent.SQUAT_RIGHT_FAILED);
        EventBus.Unsubscribe(HandleMovementFailure, GameEvent.MOVEMENT_LEFT_FAILED, GameEvent.MOVEMENT_MID_FAILED,
            GameEvent.MOVEMENT_RIGHT_FAILED);
        EventBus.Unsubscribe(HandlePunchFailure, GameEvent.RIGHT_HIT_FAILED, GameEvent.LEFT_HIT_FAILED,
            GameEvent.CROSS_LEFT_HIT_FAILED, GameEvent.CROSS_RIGHT_HIT_FAILED);
    }
    private void HandlePunchSuccess()
    {
        this.punchSuccess+=1;
        this.punchTotal += 1;
        HandleExerciseSuccess();
    }

    private void HandlePunchFailure()
    {
        this.punchTotal += 1;
        HandleExerciseFailure();
    }

    private void HandleMovementSuccess()
    {
        this.movementSuccess+=1;
        this.movementTotal += 1;
        HandleExerciseSuccess();
    }

    public void HandleMovementFailure()
    {
        this.movementTotal += 1;
        HandleExerciseFailure();
    }

    private void HandleSquatSuccess()
    {
        this.squatSuccess+=1;
        this.squatTotal += 1;
        HandleExerciseSuccess();
    }

    private void HandleSquatFailure()
    {
        this.squatTotal += 1;
        HandleExerciseFailure();
    }

    private void HandleExerciseSuccess()
    {
        comboTotal += 1;
        currentCombo += 1;

        if (comboTotal > comboMax)
        {
            comboMax = comboTotal;
        }

        if (currentTier < comboThreshold.Count && currentCombo >= comboThreshold[currentTier])
        {

            if (currentTier != comboThreshold.Count -1)
            {
                currentCombo -= comboThreshold[currentTier];
            }

            currentTier++;
            currentFill = 0.0f;
            comboText.text = multiplierTiers[currentTier].ToString();
        }

        UpdateScore();
    }

    private void UpdateScore()
    {
        score += scoreAdditionPerSuccess * multiplierTiers[currentTier];
        this.counterScript.SetCounterValue(score);
    }

    private void HandleExerciseFailure()
    {

        comboTotal = 0;
        currentCombo = 0;
        currentTier = 0;
        currentFill = 0;
        comboText.text = "1";
    }

    private void ResetStats()
    {
        //System parameters
        currentTier = 0;
        currentCombo = 0;
        comboTotal = 0;
        currentFill = 0;
        
        //General Score
        score = 0;
        comboMax = 0;
        comboText.text = "1";
        squatSuccess = 0;
        movementSuccess = 0;
        punchSuccess = 0;

        squatTotal = 0;
        movementTotal = 0;
        punchTotal = 0;
    }
}
