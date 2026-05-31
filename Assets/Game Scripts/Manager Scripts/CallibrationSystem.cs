using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CallibrationSystem : MonoBehaviour
{
    public PlayerTracker playerTracker;
    public UserManager userManager;

    [NonSerialized]
    public float targetXOffset;
    [NonSerialized]
    public float targetYOffset;
    [NonSerialized]
    public float punchZOffset;

    [NonSerialized]
    public float squattingHeight;
    [NonSerialized]
    public float standingHeight;

    public int successShowcaseTime;
    private CallibrationState state;
    public CallibrationDisplayManager callibrationDisplay;
    public StepProgression stepProgression;

    public void changeState(CallibrationState newState)
    {
        stepProgression.NextStep();
        this.state = newState;
        state.Init();
    }

    public void StartCallibration()
    {
        this.state = new CallibrateStandingHeight(this);
        state.Init();
    }

    public void HandleCallibration()
    {
        this.state.HandleCallibration();
    }

    public void HandleStateChange()
    {
        StartCoroutine(this.state.HandleStateChange());
    }

    public void finishCallibration()
    {
        UpdateActiveUserData();
        this.userManager.HandleCallibrationEnd();
        stepProgression.HideSteps();
        UIManager.Instance.SwitchWindow("user-management");
    }

    private void UpdateActiveUserData()
    {
        userManager.CallibrateActiveUser(squattingHeight, standingHeight, targetXOffset, targetYOffset, punchZOffset);
    }

}
