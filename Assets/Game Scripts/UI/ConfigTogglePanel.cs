using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConfigTogglePanel : MonoBehaviour
{
    public RoutineManager routineManager;
    private RoutinePresets routinePresets;

    public Toggle squatToggle;
    public Toggle punchToggle;
    public Toggle crossPunchToggle;
    public Toggle moveToggle;
    
    void OnEnable()
    {
        routinePresets = (RoutinePresets) routineManager.selectedRoutine;

        this.squatToggle.isOn = routinePresets.allowSquat;
        this.punchToggle.isOn = routinePresets.allowPunch;
        this.moveToggle.isOn = routinePresets.allowMovement;
        this.crossPunchToggle.isOn = routinePresets.allowCrossPunch;
    }

    public void SaveChanges()
    {
        routinePresets.allowSquat = squatToggle.isOn;
        routinePresets.allowPunch = punchToggle.isOn;
        routinePresets.allowMovement = moveToggle.isOn;
        routinePresets.allowCrossPunch = crossPunchToggle.isOn;
    }

}
