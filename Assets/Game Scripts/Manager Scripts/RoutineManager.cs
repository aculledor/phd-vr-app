using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public class RoutineManager : MonoBehaviour
{

    public ObstacleSpawner obstacleSpawner;
    public int startTime;

    [System.NonSerialized]
    public IRoutineData selectedRoutine;

    [SerializeField]
    private RoutinePresets defaultRoutine;

    public float minGuardianWidth;
    private bool pausedByServer;

    private void Start()
    {
        selectedRoutine = defaultRoutine;
    }

    public void HandleStart()
    {
        if (!selectedRoutine.allowMovement
            || OVRManager.boundary?.GetDimensions(OVRBoundary.BoundaryType.PlayArea).x>= minGuardianWidth)
        {
            if (RoutineHasSelectedParameter())
            {
                StartCoroutine(InitRoutine());

            } else
            {
                UIManager.Instance.SwitchDialogue("no-routine-dialogue");
            }
        } else if(selectedRoutine is RoutinePresets)
        {
            UIManager.Instance.SwitchDialogue("no-space-warning");
        }
        else
        {
            UIManager.Instance.SwitchDialogue("no-space-custom");
        }
    }


    public void SetActiveServerRoutine(ServerRoutineData serverRoutine)
    {
        if (serverRoutine == null)
        {
            Debug.LogWarning("No se puede seleccionar una rutina remota nula.");
            return;
        }

        selectedRoutine = serverRoutine;
    }

    public void StartSelectedRoutineFromServer()
    {
        if (selectedRoutine == null)
        {
            Debug.LogWarning("El servidor pidió iniciar, pero no hay una rutina cargada.");
            return;
        }

        pausedByServer = false;
        Time.timeScale = 1.0f;
        StartCoroutine(InitRoutine());
    }

    public void PauseRoutineFromServer()
    {
        pausedByServer = true;
        Time.timeScale = 0.0f;
    }

    public void ResumeRoutineFromServer()
    {
        pausedByServer = false;
        Time.timeScale = 1.0f;
    }

    public void StopRoutineFromServer(bool showResults = true)
    {
        pausedByServer = false;
        Time.timeScale = 1.0f;
        obstacleSpawner.StopSpawning(true, showResults);
    }

    private IEnumerator InitRoutine()
    {
        UIManager.Instance.SwitchCanvas();
        UIManager.Instance.SwitchWindow("countdown-timer");

        //Wait until the preparation counter finishes.
        yield return new WaitForSeconds(startTime + 1.0f);
        UIManager.Instance.HideCurrenMenu();
        this.obstacleSpawner.StartSpawning();
    }

    private bool RoutineHasSelectedParameter()
    {
        return selectedRoutine.allowCrossPunch || selectedRoutine.allowMovement
            || selectedRoutine.allowPunch || selectedRoutine.allowSquat;

    }

    public void HandleReducedSpaceStart()
    {
        UIManager.Instance.HideCurrentDialogue();
        ((RoutinePresets) this.selectedRoutine).allowMovement = false;
        this.HandleStart();
    }

    public void handleEnd()
    {
        UIManager.Instance.SwitchCanvas();
        UIManager.Instance.SwitchWindow("results-menu");
    }

    private void OnApplicationFocus(bool focus)
    {
        if (!focus)
        {
            Time.timeScale = 0.0f;
        }
        else if (!pausedByServer)
        {
            Time.timeScale = 1.0f;
        }
    }


    public void SetActiveRoutinePresets(RoutinePresets gameConfig)
    {
        this.selectedRoutine = gameConfig.Clone();
    }

    //We currently work with routines based on a series of presets,
    //this method should be enabled when we want to work with both types.
    public void SetActiveRoutineProgramme(FullRoutine fullRoutine)
    {
        //this.selectedRoutine = fullRoutine;
    }

}
