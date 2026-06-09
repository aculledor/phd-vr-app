using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Cliente Unity para las gafas VR:
/// - Registra el dispositivo si no hay device_id/secret guardados.
/// - Autoriza el dispositivo y obtiene token Drupal.
/// - Descarga sesiones/rutinas con GET /api/glass.
/// - Conecta a MQTT por WebSockets.
/// - Publica heartbeat.
/// - Recibe órdenes start/pause/resume/reboot/stop.
/// - Envía resultados reales a POST /api/exercise.
/// - Cierra conexión limpiamente.
/// 
/// Dependencias:
/// 1) com.unity.nuget.newtonsoft-json
/// 2) MQTTnet compatible con Unity/Android, importado como DLL o por NuGetForUnity.
/// </summary>
public class VrDrupalClient : MonoBehaviour, IBusEventCallback
{
    [Header("Drupal API")]
    [SerializeField] private string drupalBaseUrl = "https://rehabilitacion.app.citius.gal";
    [SerializeField] private bool allowInsecureCertificates = false;

    [Header("MQTT")]
    [SerializeField] private string mqttHost = "rehabilitacion.app.citius.gal";
    [SerializeField] private int mqttPort = 443;
    [SerializeField] private string mqttWebSocketPath = "/mqtt";
    [SerializeField] private bool mqttUseTls = true;
    [SerializeField] private string mqttUsername = "vr_device";
    [SerializeField] private string mqttPassword = "1234";
    [SerializeField] private int mqttKeepAliveSeconds = 60;
    [SerializeField] private int heartbeatIntervalSeconds = 30;

    [Header("Runtime state")]
    [SerializeField] private bool connectOnStart = true;

    [Header("Exercise event reporting")]
    [SerializeField] private bool sendExerciseEventsFromEventBus = true;
    [SerializeField] private UserManager userManager;
    [SerializeField] private ScoreSystem scoreSystem;
    [SerializeField] private RoutineManager routineManager;
    [SerializeField] private PlayerTracker playerTracker;
    [SerializeField] private float movementTrackingIntervalSeconds = 0.5f;

    public event Action<JObject> SessionsReceived;
    public event Action<ServerCommand> CommandReceived;
    public event Action<ServerCommand> StartReceived;
    public event Action<ServerCommand> PauseReceived;
    public event Action<ServerCommand> ResumeReceived;
    public event Action<ServerCommand> RebootReceived;
    public event Action<ServerCommand> StopReceived;
    public event Action<string> ErrorReceived;

    public string DeviceId { get; private set; }
    public string DeviceSecret { get; private set; }
    public string DrupalToken { get; private set; }
    public JObject SessionsPayload { get; private set; }
    public SessionState CurrentSession { get; private set; } = new SessionState();

    private const string DeviceIdPrefKey = "vr_device_id";
    private const string DeviceSecretPrefKey = "vr_device_secret";

    private IMqttClient mqttClient;
    private CancellationTokenSource heartbeatCancellation;
    private readonly ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();
    private bool isDisconnecting;
    private bool eventBusSubscribed;
    private DateTimeOffset? sessionStartTime;
    private Coroutine movementTrackingCoroutine;
    private bool movementTrackingSendInFlight;
    private FullRoutineItem lastExerciseItem;
    private GameEvent? lastGameEvent;

    private void OnEnable()
    {
        if (!sendExerciseEventsFromEventBus || eventBusSubscribed)
        {
            return;
        }

        EventBus.CaptureEvents(this);
        EventBus.Subscribe(HandleRehabStart, GameEvent.START_REHAB);
        EventBus.Subscribe(HandleRehabEnd, GameEvent.END_REHAB);
        eventBusSubscribed = true;
    }

    private void OnDisable()
    {
        if (!eventBusSubscribed)
        {
            return;
        }

        EventBus.StopCapturingEvents(this);
        EventBus.Unsubscribe(HandleRehabStart, GameEvent.START_REHAB);
        EventBus.Unsubscribe(HandleRehabEnd, GameEvent.END_REHAB);
        StopMovementTracking();
        eventBusSubscribed = false;
    }

    private void Start()
    {
        if (connectOnStart)
        {
            _ = InitializeAsync();
        }
    }

    private void Update()
    {
        while (mainThreadQueue.TryDequeue(out Action action))
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }

    private async void OnApplicationQuit()
    {
        StopMovementTracking();
        await DisconnectAsync();
    }

    private async void OnDestroy()
    {
        StopMovementTracking();
        await DisconnectAsync();
    }

    public void HandleEvent(GameEvent gameEvent)
    {
        if (!sendExerciseEventsFromEventBus || !IsExerciseResultEvent(gameEvent))
        {
            return;
        }

        lastGameEvent = gameEvent;
        if (EventBus.CurrentExerciseItem != null)
        {
            lastExerciseItem = EventBus.CurrentExerciseItem;
        }

        _ = SendExerciseResultForGameEventAsync(gameEvent, EventBus.CurrentExerciseItem);
    }

    private void HandleRehabStart()
    {
        sessionStartTime = DateTimeOffset.UtcNow;
        StartMovementTracking();
    }

    private void HandleRehabEnd()
    {
        StopMovementTracking();
        StoreLocalScoreSummary();
        sessionStartTime = null;
        lastExerciseItem = null;
        lastGameEvent = null;
    }

    private void StoreLocalScoreSummary()
    {
        if (userManager == null || userManager.activeUser == null || scoreSystem == null || routineManager == null || routineManager.selectedRoutine == null)
        {
            return;
        }

        DateTime startTime = sessionStartTime.HasValue
            ? sessionStartTime.Value.UtcDateTime
            : DateTime.UtcNow;

        userManager.AddScoreInformation(new ScoreRecords(
            routineManager.selectedRoutine.routineDuration,
            scoreSystem.comboMax,
            scoreSystem.score,
            scoreSystem.squatSuccess,
            scoreSystem.movementSuccess,
            scoreSystem.punchSuccess,
            scoreSystem.squatTotal,
            scoreSystem.movementTotal,
            scoreSystem.punchTotal,
            startTime));
    }

    /// <summary>
    /// Flujo completo:
    /// 1. Carga credenciales locales.
    /// 2. Si no existen, registra la gafa.
    /// 3. Autoriza y obtiene token.
    /// 4. Descarga sesiones/rutinas.
    /// 5. Conecta MQTT y arranca heartbeat.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            LoadSavedCredentials();

            if (string.IsNullOrWhiteSpace(DeviceId) || string.IsNullOrWhiteSpace(DeviceSecret))
            {
                await RegisterDeviceAsync();
            }

            try
            {
                await AuthorizeDeviceAsync();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Invalid device credentials") || ex.Message.Contains("403"))
                {
                    Debug.LogError(
                        "Las credenciales guardadas no son válidas para Drupal. " +
                        "No se registra automáticamente porque el dispositivo puede estar ya claimed. " +
                        "Borra las credenciales locales y crea/resetea un dispositivo nuevo en Drupal."
                    );

                    throw;
                }

                throw;
            }

            await GetGlassSessionsAsync();
            await ConnectMqttAsync();
        }
        catch (Exception ex)
        {
            RaiseError("Error inicializando cliente VR: " + ex.Message);
            Debug.LogException(ex);
        }
    }

    public void ForgetSavedDeviceCredentials()
    {
        PlayerPrefs.DeleteKey(DeviceIdPrefKey);
        PlayerPrefs.DeleteKey(DeviceSecretPrefKey);
        PlayerPrefs.Save();

        DeviceId = null;
        DeviceSecret = null;
        DrupalToken = null;
        SessionsPayload = null;
        CurrentSession = new SessionState();
    }

    private void LoadSavedCredentials()
    {
        DeviceId = PlayerPrefs.GetString(DeviceIdPrefKey, null);
        DeviceSecret = PlayerPrefs.GetString(DeviceSecretPrefKey, null);

        Debug.Log("Credenciales cargadas. DeviceId=" + DeviceId + 
                " Secret existe=" + !string.IsNullOrWhiteSpace(DeviceSecret));
    }

    private void SaveCredentials(string deviceId, string secret)
    {
        DeviceId = deviceId;
        DeviceSecret = secret;

        PlayerPrefs.SetString(DeviceIdPrefKey, DeviceId);
        PlayerPrefs.SetString(DeviceSecretPrefKey, DeviceSecret);
        PlayerPrefs.Save();

        Debug.Log("Credenciales guardadas. DeviceId=" + DeviceId + 
                " Secret existe=" + !string.IsNullOrWhiteSpace(DeviceSecret));
    }

    /// <summary>
    /// POST /api/glass/register
    /// Cuerpo: {}
    /// Respuesta esperada: { "id": "...", "secret": "..." }
    /// En Drupal, el registro solo está activo unos minutos tras abrir /node/add/device.
    /// </summary>
    public async Task RegisterDeviceAsync()
    {
        string url = CombineUrl(drupalBaseUrl, "/api/glass/register");

        JObject response;

        try
        {
            response = await PostJsonAsync(url, new JObject());
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Device ID is already claimed") || ex.Message.Contains("already claimed"))
            {
                throw new InvalidOperationException(
                    "Drupal dice que este Device ID ya está reclamado. " +
                    "Borra/resetéalo en Drupal o crea un dispositivo nuevo antes de volver a registrar desde Unity.",
                    ex
                );
            }

            throw;
        }

        string id = response.Value<string>("id");
        string secret = response.Value<string>("secret");

        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("La respuesta de registro no contiene id/secret.");
        }

        SaveCredentials(id, secret);
        Debug.Log("Dispositivo VR registrado: " + DeviceId);
    }

    /// <summary>
    /// POST /api/glass/authorize
    /// Cuerpo: { "id": device_id, "secret": device_secret }
    /// Respuesta esperada: { "token": "..." }
    /// </summary>
    public async Task AuthorizeDeviceAsync()
    {
        if (string.IsNullOrWhiteSpace(DeviceId) || string.IsNullOrWhiteSpace(DeviceSecret))
        {
            throw new InvalidOperationException("No hay device_id/secret para autorizar.");
        }

        string url = CombineUrl(drupalBaseUrl, "/api/glass/authorize");

        JObject body = new JObject
        {
            ["id"] = DeviceId,
            ["secret"] = DeviceSecret
        };

        Debug.Log("Autorizando DeviceId=" + DeviceId + 
          " Secret existe=" + !string.IsNullOrWhiteSpace(DeviceSecret));

        JObject response = await PostJsonAsync(url, body);
        string token = response.Value<string>("token");

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("La respuesta de autorización no contiene token.");
        }

        DrupalToken = token;
        Debug.Log("Dispositivo VR autorizado.");
    }

    /// <summary>
    /// GET /api/glass?id=DEVICE_ID
    /// Header: Authorization: Bearer TOKEN
    /// Devuelve sesiones/rutinas asociadas a la gafa.
    /// </summary>
    public async Task<JObject> GetGlassSessionsAsync()
    {
        RequireAuthorized();

        string url = CombineUrl(drupalBaseUrl, "/api/glass") + "?id=" + UnityWebRequest.EscapeURL(DeviceId);

        Debug.Log("Solicitando sesiones/rutinas a Drupal: " + url);

        SessionsPayload = await GetJsonAsync(url, DrupalToken);

        Debug.Log("Sesiones/rutinas recibidas desde Drupal:");
        Debug.Log(SessionsPayload.ToString(Formatting.Indented));

        UpdateCurrentSessionFromPayload(SessionsPayload);

        Debug.Log(
            "Sesión actual detectada. user_id=" + CurrentSession.user_id +
            " routine_id=" + CurrentSession.routine_id +
            " running=" + CurrentSession.running +
            " paused=" + CurrentSession.paused
        );

        EnqueueMainThread(() => SessionsReceived?.Invoke(SessionsPayload));

        return SessionsPayload;
    }

    /// <summary>
    /// POST /api/exercise
    /// Enviar este método desde tu lógica real de Unity cuando detectes un ejercicio,
    /// golpe, sentadilla, movimiento lateral, etc.
    /// </summary>
    public async Task<JObject> SendExerciseResultAsync(
        string routineId,
        string userId,
        string exerciseId,
        string outcome,
        MovementData movementData,
        string eventType = "execution",
        string eventId = null,
        DateTimeOffset? timestamp = null,
        JObject additionalMetadata = null)
    {
        RequireAuthorized();

        if (string.IsNullOrWhiteSpace(routineId)) throw new ArgumentException("routineId vacío.");
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("userId vacío.");
        if (string.IsNullOrWhiteSpace(exerciseId)) throw new ArgumentException("exerciseId vacío.");
        if (string.IsNullOrWhiteSpace(outcome)) throw new ArgumentException("outcome vacío.");
        outcome = outcome.Trim().ToLowerInvariant();
        if (!IsKnownOutcome(outcome)) throw new ArgumentException("outcome debe ser success, failure o missed.");
        if (movementData == null) throw new ArgumentNullException(nameof(movementData), "movementData debe contener datos reales del ejercicio.");

        DateTimeOffset ts = timestamp ?? DateTimeOffset.UtcNow;

        string metadataTimestamp = ts.ToString("o", CultureInfo.InvariantCulture);

        // Drupal DateTime field espera formato: Y-m-d\TH:i:s
        // Ejemplo: 2026-06-01T06:24:00
        string exerciseTimestamp = ts.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);

        JObject metadata = new JObject
        {
            ["version"] = "v1",
            ["timestamp"] = metadataTimestamp,
            ["source"] = "CiTIUS",
            ["routine_id"] = routineId,
            ["user_id"] = userId
        };

        if (additionalMetadata != null)
        {
            metadata.Merge(additionalMetadata, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Replace,
                MergeNullValueHandling = MergeNullValueHandling.Ignore
            });
        }

        JObject payload = new JObject
        {
            ["metadata"] = metadata,
            ["exercise_event"] = new JObject
            {
                ["event_type"] = eventType,
                ["event_id"] = string.IsNullOrWhiteSpace(eventId) ? Guid.NewGuid().ToString() : eventId,
                ["exercise_id"] = exerciseId,
                ["outcome"] = outcome,
                ["timestamp"] = exerciseTimestamp
            },
            ["movement_data"] = MovementDataToJson(movementData)
        };

        Debug.Log("POST /api/exercise payload: " + payload.ToString(Formatting.None));

        string url = CombineUrl(drupalBaseUrl, "/api/exercise");
        return await PostJsonAsync(url, payload, DrupalToken);
    }

    public async Task<JObject> SendMovementTrackingSampleAsync(
        string routineId,
        string userId,
        string exerciseId,
        string exerciseTypeCode,
        GameEvent? gameEvent,
        MovementData movementData,
        DateTimeOffset? timestamp = null)
    {
        RequireAuthorized();

        if (string.IsNullOrWhiteSpace(routineId)) throw new ArgumentException("routineId vacío.");
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("userId vacío.");
        if (movementData == null) throw new ArgumentNullException(nameof(movementData), "movementData debe contener datos reales del tracking.");

        DateTimeOffset ts = timestamp ?? DateTimeOffset.UtcNow;
        string metadataTimestamp = ts.ToString("o", CultureInfo.InvariantCulture);
        string sampleTimestamp = ts.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);

        JObject trackingSample = new JObject
        {
            ["sample_id"] = Guid.NewGuid().ToString(),
            ["timestamp"] = sampleTimestamp,
            ["exercise_id"] = SafeString(exerciseId),
            ["sample_interval_seconds"] = movementTrackingIntervalSeconds
        };

        if (!string.IsNullOrWhiteSpace(exerciseTypeCode))
        {
            trackingSample["exercise_type_code"] = exerciseTypeCode;
        }

        if (gameEvent.HasValue)
        {
            trackingSample["legacy_game_event"] = gameEvent.Value.ToString();
        }

        JObject payload = new JObject
        {
            ["metadata"] = new JObject
            {
                ["version"] = "v1",
                ["timestamp"] = metadataTimestamp,
                ["source"] = "CiTIUS",
                ["routine_id"] = routineId,
                ["user_id"] = userId,
                ["payload_type"] = "movement_tracking_sample"
            },
            ["tracking_sample"] = trackingSample,
            ["movement_data"] = MovementDataToJson(movementData)
        };

        Debug.Log("POST /api/exercise tracking payload: " + payload.ToString(Formatting.None));

        string url = CombineUrl(drupalBaseUrl, "/api/exercise");
        return await PostJsonAsync(url, payload, DrupalToken);
    }

    private static JObject MovementDataToJson(MovementData movementData)
    {
        return new JObject
        {
            ["left_controller_x"] = SafeString(movementData.left_controller_x),
            ["left_controller_y"] = SafeString(movementData.left_controller_y),
            ["left_controller_z"] = SafeString(movementData.left_controller_z),
            ["left_controller_rot_x"] = SafeString(movementData.left_controller_rot_x),
            ["left_controller_rot_y"] = SafeString(movementData.left_controller_rot_y),
            ["left_controller_rot_z"] = SafeString(movementData.left_controller_rot_z),
            ["left_controller_rot_w"] = SafeString(movementData.left_controller_rot_w),

            ["right_controller_x"] = SafeString(movementData.right_controller_x),
            ["right_controller_y"] = SafeString(movementData.right_controller_y),
            ["right_controller_z"] = SafeString(movementData.right_controller_z),
            ["right_controller_rot_x"] = SafeString(movementData.right_controller_rot_x),
            ["right_controller_rot_y"] = SafeString(movementData.right_controller_rot_y),
            ["right_controller_rot_z"] = SafeString(movementData.right_controller_rot_z),
            ["right_controller_rot_w"] = SafeString(movementData.right_controller_rot_w),

            ["head_x"] = SafeString(movementData.head_x),
            ["head_y"] = SafeString(movementData.head_y),
            ["head_z"] = SafeString(movementData.head_z),
            ["head_rot_x"] = SafeString(movementData.head_rot_x),
            ["head_rot_y"] = SafeString(movementData.head_rot_y),
            ["head_rot_z"] = SafeString(movementData.head_rot_z),
            ["head_rot_w"] = SafeString(movementData.head_rot_w)
        };
    }

    private static string SafeString(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "0" : value;
    }


    private void StartMovementTracking()
    {
        if (movementTrackingCoroutine != null)
        {
            return;
        }

        movementTrackingCoroutine = StartCoroutine(MovementTrackingRoutine());
    }

    private void StopMovementTracking()
    {
        if (movementTrackingCoroutine != null)
        {
            StopCoroutine(movementTrackingCoroutine);
            movementTrackingCoroutine = null;
        }

        movementTrackingSendInFlight = false;
    }

    private IEnumerator MovementTrackingRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(Mathf.Max(0.1f, movementTrackingIntervalSeconds));

        while (true)
        {
            TrySendMovementTrackingSample();
            yield return wait;
        }
    }

    private void TrySendMovementTrackingSample()
    {
        if (movementTrackingSendInFlight)
        {
            Debug.LogWarning("Se omite muestra de tracking: el envío anterior todavía está en curso.");
            return;
        }

        if (string.IsNullOrWhiteSpace(DrupalToken))
        {
            Debug.LogWarning("No se envía tracking a Drupal: el dispositivo todavía no está autorizado.");
            return;
        }

        if (!CurrentSession.running || CurrentSession.paused)
        {
            Debug.LogWarning("No se envía tracking a Drupal: la sesión no está activa o está pausada.");
            return;
        }

        string userId = GetActiveUserId();
        string routineId = GetActiveRoutineId();

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(routineId))
        {
            Debug.LogWarning("No se envía tracking a Drupal: falta user_id o routine_id.");
            return;
        }

        FullRoutineItem exerciseItem = EventBus.ActiveExerciseItem ?? EventBus.CurrentExerciseItem ?? lastExerciseItem;
        string exerciseId = exerciseItem != null && !string.IsNullOrWhiteSpace(exerciseItem.serverExerciseId)
            ? exerciseItem.serverExerciseId
            : (lastGameEvent.HasValue ? ToExerciseId(lastGameEvent.Value) : null);
        string exerciseTypeCode = exerciseItem != null && !string.IsNullOrWhiteSpace(exerciseItem.serverExerciseTypeCode)
            ? exerciseItem.serverExerciseTypeCode
            : (lastGameEvent.HasValue ? ToExerciseId(lastGameEvent.Value).ToUpperInvariant() : null);
        GameEvent? gameEvent = lastGameEvent;
        MovementData movementData = MovementData.FromPlayerTracker(playerTracker, gameEvent, false);

        movementTrackingSendInFlight = true;
        _ = SendMovementTrackingSampleFireAndForgetAsync(
            routineId,
            userId,
            exerciseId,
            exerciseTypeCode,
            gameEvent,
            movementData,
            DateTimeOffset.UtcNow);
    }

    private async Task SendMovementTrackingSampleFireAndForgetAsync(
        string routineId,
        string userId,
        string exerciseId,
        string exerciseTypeCode,
        GameEvent? gameEvent,
        MovementData movementData,
        DateTimeOffset timestamp)
    {
        try
        {
            await SendMovementTrackingSampleAsync(
                routineId,
                userId,
                exerciseId,
                exerciseTypeCode,
                gameEvent,
                movementData,
                timestamp);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Error enviando tracking de movimiento a Drupal: " + ex.Message);
        }
        finally
        {
            movementTrackingSendInFlight = false;
        }
    }

    private async Task SendExerciseResultForGameEventAsync(GameEvent gameEvent, FullRoutineItem exerciseItem)
    {
        if (string.IsNullOrWhiteSpace(DrupalToken))
        {
            Debug.LogWarning("No se envía resultado a Drupal: el dispositivo todavía no está autorizado.");
            return;
        }

        string userId = GetActiveUserId();
        string routineId = GetActiveRoutineId();

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(routineId))
        {
            Debug.LogWarning("No se envía resultado a Drupal: falta user_id o routine_id.");
            return;
        }

        try
        {
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            MovementData movementData = MovementData.FromPlayerTracker(playerTracker, gameEvent);
            string exerciseId = GetExerciseIdForEvent(gameEvent, exerciseItem);

            Debug.Log("Enviando medición de ejercicio a Drupal. exercise_id=" + exerciseId + ", event=" + gameEvent);

            await SendExerciseResultAsync(
                routineId,
                userId,
                exerciseId,
                ToExerciseOutcome(gameEvent),
                movementData,
                "execution",
                Guid.NewGuid().ToString(),
                timestamp,
                BuildExerciseEventMetadata(gameEvent, timestamp, exerciseItem));
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Error enviando resultado real a Drupal: " + ex.Message);
        }
    }

    private JObject BuildExerciseEventMetadata(GameEvent gameEvent, DateTimeOffset timestamp, FullRoutineItem exerciseItem)
    {
        JObject metadata = new JObject
        {
            ["legacy_game_event"] = gameEvent.ToString(),
            ["event_timestamp"] = timestamp.ToString("o")
        };

        if (sessionStartTime.HasValue)
        {
            metadata["session_start_time"] = sessionStartTime.Value.ToString("o");
        }

        if (exerciseItem != null)
        {
            metadata["server_exercise"] = new JObject
            {
                ["exercise_id"] = exerciseItem.serverExerciseId,
                ["exercise_type_code"] = exerciseItem.serverExerciseTypeCode,
                ["height_meters"] = exerciseItem.heightOverrideMeters,
                ["distance_meters"] = exerciseItem.distanceOverrideMeters
            };
        }

        IRoutineData routineData = routineManager != null ? routineManager.selectedRoutine : null;
        if (routineData != null)
        {
            metadata["routine_flags"] = new JObject
            {
                ["allow_squat"] = routineData.allowSquat,
                ["allow_movement"] = routineData.allowMovement,
                ["allow_punch"] = routineData.allowPunch,
                ["allow_cross_punch"] = routineData.allowCrossPunch,
                ["routine_duration"] = routineData.routineDuration
            };
        }

        if (scoreSystem != null)
        {
            metadata["score"] = new JObject
            {
                ["current_score"] = scoreSystem.score,
                ["max_combo"] = scoreSystem.comboMax,
                ["squat_success"] = scoreSystem.squatSuccess,
                ["movement_success"] = scoreSystem.movementSuccess,
                ["punch_success"] = scoreSystem.punchSuccess,
                ["squat_total"] = scoreSystem.squatTotal,
                ["movement_total"] = scoreSystem.movementTotal,
                ["punch_total"] = scoreSystem.punchTotal
            };
        }

        return metadata;
    }

    private string GetActiveUserId()
    {
        if (!string.IsNullOrWhiteSpace(CurrentSession.user_id))
        {
            return CurrentSession.user_id;
        }

        if (routineManager != null && routineManager.selectedRoutine is ServerRoutineData serverRoutine
            && !string.IsNullOrWhiteSpace(serverRoutine.userId))
        {
            return serverRoutine.userId;
        }

        return userManager != null && userManager.activeUser != null
            ? userManager.activeUser.identifier
            : null;
    }

    private string GetActiveRoutineId()
    {
        if (!string.IsNullOrWhiteSpace(CurrentSession.routine_id))
        {
            return CurrentSession.routine_id;
        }

        if (routineManager == null || routineManager.selectedRoutine == null)
        {
            return null;
        }

        if (routineManager.selectedRoutine is ServerRoutineData serverRoutine && !string.IsNullOrWhiteSpace(serverRoutine.routineId))
        {
            return serverRoutine.routineId;
        }

        if (routineManager.selectedRoutine is RoutinePresets preset && !string.IsNullOrWhiteSpace(preset.identifier))
        {
            return preset.identifier;
        }

        return routineManager.selectedRoutine.GetType().Name;
    }

    public async Task ConnectMqttAsync()
    {
        RequireAuthorized();
        isDisconnecting = false;

        if (mqttClient != null && mqttClient.IsConnected)
        {
            return;
        }

        string scheme = mqttUseTls ? "wss" : "ws";
        string wsPath = string.IsNullOrWhiteSpace(mqttWebSocketPath) ? "/" : mqttWebSocketPath;
        string websocketUri = $"{scheme}://{mqttHost}:{mqttPort}{wsPath}";

        string clientId = "vr-unity-" + DeviceId;

        MqttFactory factory = new MqttFactory();
        mqttClient = factory.CreateMqttClient();

        mqttClient.ApplicationMessageReceivedAsync += args =>
        {
            string topic = args.ApplicationMessage.Topic;
            string payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);

            Debug.Log("MQTT mensaje recibido.");
            Debug.Log("Topic: " + topic);
            Debug.Log("Payload: " + payload);

            if (topic == GetCommandTopic())
            {
                try
                {
                    Debug.Log("Orden recibida desde Drupal en topic de comandos: " + GetCommandTopic());

                    JObject json = JObject.Parse(payload);

                    Debug.Log("Orden JSON parseada:");
                    Debug.Log(json.ToString(Formatting.Indented));

                    ServerCommand command = json.ToObject<ServerCommand>();

                    if (command == null)
                    {
                        RaiseError("Mensaje MQTT inválido: comando vacío.");
                        return Task.CompletedTask;
                    }

                    command.Raw = json;

                    Debug.Log(
                        "Comando recibido. " +
                        "status=" + command.status +
                        ", action=" + command.action +
                        ", user_id=" + command.user_id +
                        ", routine_id=" + command.routine_id +
                        ", timestamp=" + command.timestamp
                    );

                    EnqueueMainThread(() => HandleServerCommand(command));
                }
                catch (Exception ex)
                {
                    RaiseError("Mensaje MQTT inválido: " + ex.Message);
                }
            }
            else
            {
                Debug.Log("Mensaje MQTT ignorado porque no pertenece al topic de comandos esperado: " + GetCommandTopic());
            }

            return Task.CompletedTask;
        };

        mqttClient.ConnectedAsync += async args =>
        {
            Debug.Log("MQTT conectado.");

            string commandTopic = GetCommandTopic();

            Debug.Log("Suscribiéndose a topic de comandos: " + commandTopic);

            await mqttClient.SubscribeAsync(commandTopic, MqttQualityOfServiceLevel.AtLeastOnce);

            Debug.Log("Suscripción MQTT completada: " + commandTopic);

            await PublishHeartbeatAsync("online");
        };

        mqttClient.DisconnectedAsync += args =>
        {
            Debug.Log("MQTT desconectado.");
            return Task.CompletedTask;
        };

        MqttClientOptionsBuilder builder = new MqttClientOptionsBuilder()
            .WithClientId(clientId)
            .WithCredentials(mqttUsername, mqttPassword)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(mqttKeepAliveSeconds))
            .WithCleanSession();

        builder.WithWebSocketServer(options =>
        {
            options.WithUri(websocketUri);
        });

        if (mqttUseTls)
        {
            builder.WithTlsOptions(options =>
            {
                options.UseTls();
            });
        }

        await mqttClient.ConnectAsync(builder.Build(), CancellationToken.None);
        StartHeartbeatLoop();
    }

    private void HandleServerCommand(ServerCommand command)
    {
        if (command == null)
        {
            Debug.LogWarning("HandleServerCommand llamado con command null.");
            return;
        }

        Debug.Log("Procesando orden del servidor Drupal.");

        CommandReceived?.Invoke(command);

        string action = NormalizeCommandAction(command);
        string userId = string.IsNullOrWhiteSpace(command.user_id) ? CurrentSession.user_id : command.user_id;
        string routineId = string.IsNullOrWhiteSpace(command.routine_id) ? CurrentSession.routine_id : command.routine_id;

        Debug.Log(
            "Orden normalizada: " + action +
            ". user_id=" + userId +
            ", routine_id=" + routineId +
            ", status_original=" + command.status +
            ", action_original=" + command.action
        );

        switch (action)
        {
            case "start":
                Debug.Log("Orden START recibida: iniciar rutina VR.");
                CurrentSession.running = true;
                CurrentSession.paused = false;
                CurrentSession.user_id = userId;
                CurrentSession.routine_id = routineId;
                CurrentSession.last_action = "start";
                StartReceived?.Invoke(command);
                break;

            case "pause":
                Debug.Log("Orden PAUSE recibida: pausar rutina VR.");
                CurrentSession.paused = true;
                CurrentSession.last_action = "pause";
                StopMovementTracking();
                PauseReceived?.Invoke(command);
                break;

            case "resume":
                Debug.Log("Orden RESUME recibida: reanudar rutina VR.");
                CurrentSession.running = true;
                CurrentSession.paused = false;
                CurrentSession.last_action = "resume";
                StartMovementTracking();
                ResumeReceived?.Invoke(command);
                break;

            case "reboot":
                Debug.Log("Orden REBOOT recibida: reiniciar rutina VR.");
                CurrentSession.running = true;
                CurrentSession.paused = false;
                CurrentSession.user_id = userId;
                CurrentSession.routine_id = routineId;
                CurrentSession.last_action = "reboot";
                RebootReceived?.Invoke(command);
                break;

            case "stop":
                Debug.Log("Orden STOP recibida: detener rutina VR.");
                CurrentSession.running = false;
                CurrentSession.paused = false;
                CurrentSession.last_action = "stop";
                StopMovementTracking();
                StopReceived?.Invoke(command);
                break;

            default:
                RaiseError("Acción MQTT desconocida: status=" + command.status + ", action=" + command.action);
                break;
        }

        _ = PublishHeartbeatAsync("online");
    }

    public async Task PublishHeartbeatAsync(string status = "online")
    {
        if (mqttClient == null || !mqttClient.IsConnected)
        {
            return;
        }

        JObject payload = new JObject
        {
            ["status"] = status,
            ["device_id"] = DeviceId,
            ["timestamp"] = DateTimeOffset.UtcNow.ToString("o"),
            ["session"] = JObject.FromObject(CurrentSession)
        };

        Debug.Log("Publicando heartbeat MQTT en " + GetStatusTopic() + ": " + payload.ToString(Formatting.None));

        MqttApplicationMessage message = new MqttApplicationMessageBuilder()
            .WithTopic(GetStatusTopic())
            .WithPayload(payload.ToString(Formatting.None))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(false)
            .Build();

        await mqttClient.PublishAsync(message, CancellationToken.None);
    }

    private void StartHeartbeatLoop()
    {
        StopHeartbeatLoop();

        heartbeatCancellation = new CancellationTokenSource();
        CancellationToken token = heartbeatCancellation.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await PublishHeartbeatAsync("online");
                }
                catch (Exception ex)
                {
                    RaiseError("Error enviando heartbeat: " + ex.Message);
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(heartbeatIntervalSeconds), token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }, token);
    }

    private void StopHeartbeatLoop()
    {
        if (heartbeatCancellation != null)
        {
            heartbeatCancellation.Cancel();
            heartbeatCancellation.Dispose();
            heartbeatCancellation = null;
        }
    }

    public async Task DisconnectAsync()
    {
        if (isDisconnecting)
        {
            return;
        }

        isDisconnecting = true;
        StopHeartbeatLoop();

        try
        {
            if (mqttClient != null && mqttClient.IsConnected)
            {
                await PublishHeartbeatAsync("disconnected");
                await Task.Delay(300);
                await mqttClient.DisconnectAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("No se pudo cerrar MQTT limpiamente: " + ex.Message);
        }
        finally
        {
            mqttClient?.Dispose();
            mqttClient = null;
            isDisconnecting = false;
        }
    }

    public string GetStatusTopic()
    {
        return $"vr/{DeviceId}/status";
    }

    public string GetCommandTopic()
    {
        return $"vr/{DeviceId}/command";
    }

    private async Task<JObject> GetJsonAsync(string url, string bearerToken = null)
    {
        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Accept", "application/json");

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.SetRequestHeader("Authorization", "Bearer " + bearerToken);
        }

        if (allowInsecureCertificates)
        {
            request.certificateHandler = new AcceptAllCertificates();
        }

        await SendWebRequestAsync(request);
        return ParseJsonObject(request.downloadHandler.text);
    }

    private async Task<JObject> PostJsonAsync(string url, JObject body, string bearerToken = null)
    {
        string json = body?.ToString(Formatting.None) ?? "{}";
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(bytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json");

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.SetRequestHeader("Authorization", "Bearer " + bearerToken);
        }

        if (allowInsecureCertificates)
        {
            request.certificateHandler = new AcceptAllCertificates();
        }

        await SendWebRequestAsync(request);

        string text = request.downloadHandler.text;
        if (string.IsNullOrWhiteSpace(text))
        {
            return new JObject
            {
                ["status_code"] = (int)request.responseCode
            };
        }

        return ParseJsonObject(text);
    }

    private static Task SendWebRequestAsync(UnityWebRequest request)
    {
        TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();

        operation.completed += _ =>
        {
#if UNITY_2020_2_OR_NEWER
            bool failed = request.result == UnityWebRequest.Result.ConnectionError
                          || request.result == UnityWebRequest.Result.ProtocolError
                          || request.result == UnityWebRequest.Result.DataProcessingError;
#else
            bool failed = request.isNetworkError || request.isHttpError;
#endif

            if (failed)
            {
                completion.SetException(new InvalidOperationException(
                    $"HTTP {request.responseCode}: {request.error}\n{request.downloadHandler?.text}"
                ));
            }
            else
            {
                completion.SetResult(true);
            }
        };

        return completion.Task;
    }

    private static JObject ParseJsonObject(string text)
    {
        try
        {
            JToken token = JToken.Parse(text);
            if (token is JObject obj)
            {
                return obj;
            }

            return new JObject
            {
                ["data"] = token
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Respuesta no JSON: " + text, ex);
        }
    }

    private void RequireAuthorized()
    {
        if (string.IsNullOrWhiteSpace(DeviceId))
        {
            throw new InvalidOperationException("DeviceId no definido.");
        }

        if (string.IsNullOrWhiteSpace(DrupalToken))
        {
            throw new InvalidOperationException("DrupalToken no definido. Ejecuta AuthorizeDeviceAsync primero.");
        }
    }


    private static bool IsExerciseResultEvent(GameEvent gameEvent)
    {
        switch (gameEvent)
        {
            case GameEvent.RIGHT_HIT_SUCCESS:
            case GameEvent.RIGHT_HIT_FAILED:
            case GameEvent.LEFT_HIT_SUCCESS:
            case GameEvent.LEFT_HIT_FAILED:
            case GameEvent.CROSS_RIGHT_HIT_SUCCESS:
            case GameEvent.CROSS_RIGHT_HIT_FAILED:
            case GameEvent.CROSS_LEFT_HIT_SUCCESS:
            case GameEvent.CROSS_LEFT_HIT_FAILED:
            case GameEvent.SQUAT_LEFT_SUCCESS:
            case GameEvent.SQUAT_LEFT_FAILED:
            case GameEvent.SQUAT_MID_SUCCESS:
            case GameEvent.SQUAT_MID_FAILED:
            case GameEvent.SQUAT_RIGHT_SUCCESS:
            case GameEvent.SQUAT_RIGHT_FAILED:
            case GameEvent.MOVEMENT_LEFT_SUCCESS:
            case GameEvent.MOVEMENT_LEFT_FAILED:
            case GameEvent.MOVEMENT_MID_SUCCESS:
            case GameEvent.MOVEMENT_MID_FAILED:
            case GameEvent.MOVEMENT_RIGHT_SUCCESS:
            case GameEvent.MOVEMENT_RIGHT_FAILED:
                return true;
            default:
                return false;
        }
    }

    private static string GetExerciseIdForEvent(GameEvent gameEvent, FullRoutineItem exerciseItem)
    {
        return exerciseItem != null && !string.IsNullOrWhiteSpace(exerciseItem.serverExerciseId)
            ? exerciseItem.serverExerciseId
            : ToExerciseId(gameEvent);
    }

    private static string ToExerciseOutcome(GameEvent gameEvent)
    {
        return gameEvent.ToString().EndsWith("_SUCCESS", StringComparison.Ordinal)
            ? "success"
            : "failure";
    }

    private static string ToExerciseId(GameEvent gameEvent)
    {
        string value = gameEvent.ToString().ToLowerInvariant();

        if (value.EndsWith("_success", StringComparison.Ordinal))
        {
            value = value.Substring(0, value.Length - "_success".Length);
        }
        else if (value.EndsWith("_failed", StringComparison.Ordinal))
        {
            value = value.Substring(0, value.Length - "_failed".Length);
        }

        return value;
    }

    private static bool IsKnownOutcome(string outcome)
    {
        string value = outcome.Trim().ToLowerInvariant();
        return value == "success" || value == "failure" || value == "missed";
    }

    private static string NormalizeCommandAction(ServerCommand command)
    {
        string status = command.status?.Trim().ToLowerInvariant();
        string action = command.action?.Trim().ToLowerInvariant();

        if (status == "execution" && action == "start") return "start";
        if (status == "finished" && action == "stop") return "stop";
        if (status == "execution" && action == "reboot") return "reboot";
        if (status == "pause" && action == "pause") return "pause";
        if (status == "execution" && action == "resume") return "resume";
        if (status == "scheduled" && action == "stop") return "stop";

        return action;
    }

    private void UpdateCurrentSessionFromPayload(JObject payload)
    {
        if (payload == null)
        {
            Debug.LogWarning("No se puede actualizar CurrentSession: payload de sesiones vacío.");
            return;
        }

        string userId = FindFirstStringProperty(payload, "user_id");
        string routineId = FindFirstStringProperty(payload, "routine_id");

        if (!string.IsNullOrWhiteSpace(userId))
        {
            CurrentSession.user_id = userId;
        }
        else
        {
            Debug.LogWarning("No se encontró user_id en la respuesta de /api/glass.");
        }

        if (!string.IsNullOrWhiteSpace(routineId))
        {
            CurrentSession.routine_id = routineId;
        }
        else
        {
            Debug.LogWarning("No se encontró routine_id en la respuesta de /api/glass.");
        }

        Debug.Log(
            "CurrentSession actualizada desde /api/glass: user_id=" + CurrentSession.user_id +
            ", routine_id=" + CurrentSession.routine_id
        );
    }

    private static string FindFirstStringProperty(JToken token, string propertyName)
    {
        foreach (JToken descendant in GetDescendantsAndSelfSafe(token))
        {
            if (descendant is JProperty property &&
                string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                string value = property.Value?.Type == JTokenType.String
                    ? property.Value.Value<string>()
                    : property.Value?.ToString(Formatting.None);

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }

        return null;
    }

    private static IEnumerable<JToken> GetDescendantsAndSelfSafe(JToken token)
    {
        if (token == null)
        {
            yield break;
        }

        yield return token;

        JContainer container = token as JContainer;

        if (container == null)
        {
            yield break;
        }

        foreach (JToken child in container.Children())
        {
            foreach (JToken descendant in GetDescendantsAndSelfSafe(child))
            {
                yield return descendant;
            }
        }
    }

    private void RaiseError(string message)
    {
        Debug.LogWarning(message);
        EnqueueMainThread(() => ErrorReceived?.Invoke(message));
    }

    private void EnqueueMainThread(Action action)
    {
        mainThreadQueue.Enqueue(action);
    }

    private static string CombineUrl(string baseUrl, string path)
    {
        return baseUrl.TrimEnd('/') + "/" + path.TrimStart('/');
    }

    private sealed class AcceptAllCertificates : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
}

[Serializable]
public class SessionState
{
    public bool running;
    public bool paused;
    public string user_id;
    public string routine_id;
    public string last_action;
}

[Serializable]
public class ServerCommand
{
    public string user_id;
    public string routine_id;
    public string status;
    public string action;
    public string timestamp;

    [JsonIgnore] public JObject Raw;
}

[Serializable]
public class MovementData
{
    public string left_controller_x;
    public string left_controller_y;
    public string left_controller_z;
    public string left_controller_rot_x;
    public string left_controller_rot_y;
    public string left_controller_rot_z;
    public string left_controller_rot_w;

    public string right_controller_x;
    public string right_controller_y;
    public string right_controller_z;
    public string right_controller_rot_x;
    public string right_controller_rot_y;
    public string right_controller_rot_z;
    public string right_controller_rot_w;

    public string head_x;
    public string head_y;
    public string head_z;
    public string head_rot_x;
    public string head_rot_y;
    public string head_rot_z;
    public string head_rot_w;

    public string game_event;
    public string left_controller_distance;
    public string right_controller_distance;
    public string squat_height;
    public string horizontal_movement;

    public static MovementData FromUnityPositions(Vector3 leftController, Vector3 rightController, Vector3 head)
    {
        return FromUnityPose(
            leftController,
            Quaternion.identity,
            rightController,
            Quaternion.identity,
            head,
            Quaternion.identity);
    }

    public static MovementData FromUnityPose(
        Vector3 leftController,
        Quaternion leftControllerRotation,
        Vector3 rightController,
        Quaternion rightControllerRotation,
        Vector3 head,
        Quaternion headRotation)
    {
        return new MovementData
        {
            left_controller_x = FormatFloat(leftController.x),
            left_controller_y = FormatFloat(leftController.y),
            left_controller_z = FormatFloat(leftController.z),
            left_controller_rot_x = FormatFloat(leftControllerRotation.x),
            left_controller_rot_y = FormatFloat(leftControllerRotation.y),
            left_controller_rot_z = FormatFloat(leftControllerRotation.z),
            left_controller_rot_w = FormatFloat(leftControllerRotation.w),

            right_controller_x = FormatFloat(rightController.x),
            right_controller_y = FormatFloat(rightController.y),
            right_controller_z = FormatFloat(rightController.z),
            right_controller_rot_x = FormatFloat(rightControllerRotation.x),
            right_controller_rot_y = FormatFloat(rightControllerRotation.y),
            right_controller_rot_z = FormatFloat(rightControllerRotation.z),
            right_controller_rot_w = FormatFloat(rightControllerRotation.w),

            head_x = FormatFloat(head.x),
            head_y = FormatFloat(head.y),
            head_z = FormatFloat(head.z),
            head_rot_x = FormatFloat(headRotation.x),
            head_rot_y = FormatFloat(headRotation.y),
            head_rot_z = FormatFloat(headRotation.z),
            head_rot_w = FormatFloat(headRotation.w)
        };
    }

    public static MovementData FromPlayerTracker(PlayerTracker tracker, GameEvent? gameEvent, bool resetTrackerAfterRead = true)
    {
        MovementData data = tracker != null
            ? FromUnityPose(
                tracker.LControllerPosition,
                tracker.LeftHandRotation,
                tracker.RControllerPosition,
                tracker.RightHandRotation,
                tracker.PlayerPosition,
                tracker.HeadRotation)
            : new MovementData();

        data.game_event = gameEvent.HasValue ? gameEvent.Value.ToString() : null;

        if (tracker != null)
        {
            data.left_controller_distance = FormatFloat(tracker.LeftHitDistance);
            data.right_controller_distance = FormatFloat(tracker.RightHitDistance);
            data.squat_height = FormatFloat(gameEvent.HasValue && IsSquatSuccess(gameEvent.Value) ? tracker.SquatMinHeight : tracker.CurrentSquatHeight);
            data.horizontal_movement = FormatFloat(tracker.HorizontalMovement);

            if (resetTrackerAfterRead)
            {
                tracker.ResetTrackingData();
            }
        }

        return data;
    }

    private static bool IsSquatSuccess(GameEvent gameEvent)
    {
        return gameEvent == GameEvent.SQUAT_LEFT_SUCCESS
            || gameEvent == GameEvent.SQUAT_MID_SUCCESS
            || gameEvent == GameEvent.SQUAT_RIGHT_SUCCESS;
    }

    private static string FormatFloat(float value)
    {
        return value.ToString("F4", CultureInfo.InvariantCulture);
    }
}
