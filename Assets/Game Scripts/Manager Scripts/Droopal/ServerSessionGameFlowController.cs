using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// Puente entre Drupal/MQTT y el flujo jugable de Unity:
/// - Al cargar, muestra únicamente la pantalla de conexión.
/// - Cuando /api/glass llega correctamente, convierte sus ejercicios en la rutina activa.
/// - No inicia el ejercicio hasta recibir start/reboot por MQTT.
/// - Pausa, reanuda y detiene según las órdenes del servidor.
/// </summary>
public class ServerSessionGameFlowController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private VrDrupalClient drupalClient;
    [SerializeField] private RoutineManager routineManager;

    [Header("Connection UI")]
    [SerializeField] private GameObject connectingWindow;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject[] objectsToHideWhileConnecting;
    [SerializeField] private GameObject[] objectsToShowWhenExerciseStarts;

    private ServerRoutineData loadedRoutine;
    private bool routineRunning;
    private bool suppressNextRoutineEndWindow;

    private void Awake()
    {
        ShowConnectingState("Conectando al servidor...");
    }

    private void Start()
    {
        ShowConnectingState("Conectando al servidor...");
    }

    private void OnEnable()
    {
        EventBus.Subscribe(HandleRoutineEnded, GameEvent.END_REHAB);

        if (drupalClient == null)
        {
            return;
        }

        drupalClient.SessionsReceived += HandleSessionsReceived;
        drupalClient.StartReceived += HandleStartReceived;
        drupalClient.PauseReceived += HandlePauseReceived;
        drupalClient.ResumeReceived += HandleResumeReceived;
        drupalClient.RebootReceived += HandleRebootReceived;
        drupalClient.StopReceived += HandleStopReceived;
        drupalClient.ErrorReceived += HandleErrorReceived;
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(HandleRoutineEnded, GameEvent.END_REHAB);

        if (drupalClient == null)
        {
            return;
        }

        drupalClient.SessionsReceived -= HandleSessionsReceived;
        drupalClient.StartReceived -= HandleStartReceived;
        drupalClient.PauseReceived -= HandlePauseReceived;
        drupalClient.ResumeReceived -= HandleResumeReceived;
        drupalClient.RebootReceived -= HandleRebootReceived;
        drupalClient.StopReceived -= HandleStopReceived;
        drupalClient.ErrorReceived -= HandleErrorReceived;
    }

    private void HandleSessionsReceived(JObject payload)
    {
        if (!ServerRoutineData.TryCreateFromGlassPayload(payload, out loadedRoutine, out string error))
        {
            HandleErrorReceived(error);
            return;
        }

        routineManager.SetActiveServerRoutine(loadedRoutine);
        ShowWaitingForStartState(loadedRoutine);
    }

    private void HandleStartReceived(ServerCommand command)
    {
        if (!CanRunServerCommand("START"))
        {
            return;
        }

        HideConnectionUi();
        routineRunning = true;
        routineManager.StartSelectedRoutineFromServer();
    }

    private void HandlePauseReceived(ServerCommand command)
    {
        if (!routineRunning)
        {
            return;
        }

        routineManager.PauseRoutineFromServer();
        ShowConnectingState("Rutina pausada desde el servidor.");
    }

    private void HandleResumeReceived(ServerCommand command)
    {
        if (!routineRunning)
        {
            return;
        }

        routineManager.ResumeRoutineFromServer();
        HideConnectionUi();
        SetStatus("Rutina reanudada desde el servidor.");
    }

    private void HandleRebootReceived(ServerCommand command)
    {
        if (!CanRunServerCommand("REBOOT"))
        {
            return;
        }

        suppressNextRoutineEndWindow = true;
        routineManager.StopRoutineFromServer(false);
        suppressNextRoutineEndWindow = false;
        HideConnectionUi();
        routineRunning = true;
        routineManager.StartSelectedRoutineFromServer();
    }

    private void HandleStopReceived(ServerCommand command)
    {
        routineRunning = false;
        routineManager.StopRoutineFromServer(true);
        ShowWaitingForStartState(loadedRoutine);
    }


    private void HandleRoutineEnded()
    {
        if (suppressNextRoutineEndWindow)
        {
            suppressNextRoutineEndWindow = false;
            return;
        }

        if (!routineRunning)
        {
            return;
        }

        routineRunning = false;
        ShowWaitingForStartState(loadedRoutine);
    }

    private void HandleErrorReceived(string message)
    {
        ShowConnectingState("Error conectando con el servidor: " + message);
    }

    private bool CanRunServerCommand(string commandName)
    {
        if (loadedRoutine != null)
        {
            return true;
        }

        HandleErrorReceived("El servidor envió " + commandName + " antes de descargar una rutina válida.");
        return false;
    }

    private void ShowConnectingState(string message)
    {
        if (connectingWindow != null)
        {
            connectingWindow.SetActive(true);
        }

        if (objectsToHideWhileConnecting != null)
        {
            foreach (GameObject obj in objectsToHideWhileConnecting)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }

        SetStatus(message);
    }

    private void ShowWaitingForStartState(ServerRoutineData routine)
    {
        string routineId = routine != null ? routine.routineId : "-";
        string userId = routine != null ? routine.userId : "-";
        ShowConnectingState("Conectado. Rutina " + routineId + " descargada para usuario " + userId + ". Esperando señal de inicio...");
    }

    private void HideConnectionUi()
    {
        if (connectingWindow != null)
        {
            connectingWindow.SetActive(false);
        }

        if (objectsToShowWhenExerciseStarts != null)
        {
            foreach (GameObject obj in objectsToShowWhenExerciseStarts)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
        }
    }

    private void SetStatus(string message)
    {
        Debug.Log(message);
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

}
