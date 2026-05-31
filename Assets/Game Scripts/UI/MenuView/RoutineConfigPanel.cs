using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoutineConfigPanel : MonoBehaviour
{
    public RoutineManager routineManager;
    private RoutinePresets activePresets;

    public TextMeshProUGUI timeText;


    public Toggle squatToggle;
    public Toggle punchToggle;
    public Toggle crossPunchToggle;
    public Toggle moveToggle;
    // Start is called before the first frame update
    private void OnEnable()
    {
        activePresets = (RoutinePresets) routineManager.selectedRoutine;
        this.squatToggle.isOn = activePresets.allowSquat;
        this.punchToggle.isOn = activePresets.allowPunch;
        this.moveToggle.isOn = activePresets.allowMovement;
        this.crossPunchToggle.isOn = activePresets.allowCrossPunch;
        FormatTimerText();
    }


    public void HandleAddTimer()
    {
        activePresets.routineDuration += 10;
        FormatTimerText();
    }

    public void HandleSubstractTimer()
    {
        if (activePresets.routineDuration > 10)
            activePresets.routineDuration -= 10;
        FormatTimerText();
    }

    private void FormatTimerText()
    {
        this.timeText.text = $"{activePresets.routineDuration / 60 }:" +
            $"{(activePresets.routineDuration % 60).ToString("D2")}";
    }

    public void StartRoutine()
    {
        activePresets.allowSquat = this.squatToggle.isOn;
        activePresets.allowPunch = this.punchToggle.isOn ;
        activePresets.allowMovement= this.moveToggle.isOn;
        activePresets.allowCrossPunch = this.crossPunchToggle.isOn;
        routineManager.HandleStart();
    }

}
