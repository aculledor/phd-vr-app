using System.Collections;
using UnityEngine;

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
    private Coroutine initRoutineCoroutine;

    private void Start()
    {
        selectedRoutine = defaultRoutine;
    }

    public void HandleStart()
    {
        if (selectedRoutine == null)
        {
            Debug.LogWarning("No hay rutina seleccionada para iniciar.");
            return;
        }

        if (!RoutineHasSelectedParameter())
        {
            Debug.LogWarning("La rutina seleccionada no contiene ejercicios activos.");
            return;
        }

        if (selectedRoutine.allowMovement && OVRManager.boundary != null
            && OVRManager.boundary.GetDimensions(OVRBoundary.BoundaryType.PlayArea).x < minGuardianWidth)
        {
            Debug.LogWarning("No hay espacio suficiente para iniciar una rutina con movimiento lateral.");
            return;
        }

        StartRoutineAfterPreparation();
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
        StartRoutineAfterPreparation();
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

        if (initRoutineCoroutine != null)
        {
            StopCoroutine(initRoutineCoroutine);
            initRoutineCoroutine = null;
        }

        obstacleSpawner.StopSpawning(true, showResults);
    }

    private void StartRoutineAfterPreparation()
    {
        if (initRoutineCoroutine != null)
        {
            StopCoroutine(initRoutineCoroutine);
        }

        initRoutineCoroutine = StartCoroutine(InitRoutine());
    }

    private IEnumerator InitRoutine()
    {
        // The server-driven flow owns all visible windows. RoutineManager only waits
        // the configured preparation time and starts spawning obstacles.
        yield return new WaitForSeconds(startTime + 1.0f);
        initRoutineCoroutine = null;
        this.obstacleSpawner.StartSpawning();
    }

    private bool RoutineHasSelectedParameter()
    {
        return selectedRoutine.allowCrossPunch || selectedRoutine.allowMovement
            || selectedRoutine.allowPunch || selectedRoutine.allowSquat;

    }

    public void HandleReducedSpaceStart()
    {
        if (this.selectedRoutine is RoutinePresets preset)
        {
            preset.allowMovement = false;
        }

        this.HandleStart();
    }

    public void handleEnd()
    {
        Debug.Log("Rutina finalizada.");
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
