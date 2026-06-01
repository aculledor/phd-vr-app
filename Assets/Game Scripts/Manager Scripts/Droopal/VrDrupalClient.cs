using System;
using System.Collections;
using System.Collections.Concurrent;
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
public class VrDrupalClient : MonoBehaviour
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
        await DisconnectAsync();
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

            await AuthorizeDeviceAsync();
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
    }

    private void LoadSavedCredentials()
    {
        DeviceId = PlayerPrefs.GetString(DeviceIdPrefKey, null);
        DeviceSecret = PlayerPrefs.GetString(DeviceSecretPrefKey, null);
    }

    private void SaveCredentials(string deviceId, string secret)
    {
        DeviceId = deviceId;
        DeviceSecret = secret;

        PlayerPrefs.SetString(DeviceIdPrefKey, DeviceId);
        PlayerPrefs.SetString(DeviceSecretPrefKey, DeviceSecret);
        PlayerPrefs.Save();
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
        JObject response = await PostJsonAsync(url, new JObject());

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
        SessionsPayload = await GetJsonAsync(url, DrupalToken);

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
        DateTimeOffset? timestamp = null)
    {
        RequireAuthorized();

        if (string.IsNullOrWhiteSpace(routineId)) throw new ArgumentException("routineId vacío.");
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("userId vacío.");
        if (string.IsNullOrWhiteSpace(exerciseId)) throw new ArgumentException("exerciseId vacío.");
        if (string.IsNullOrWhiteSpace(outcome)) throw new ArgumentException("outcome vacío.");

        DateTimeOffset ts = timestamp ?? DateTimeOffset.UtcNow;
        string timestampIso = ts.ToString("o");

        JObject payload = new JObject
        {
            ["metadata"] = new JObject
            {
                ["version"] = "v1",
                ["timestamp"] = timestampIso,
                ["source"] = "CiTIUS",
                ["routine_id"] = routineId,
                ["user_id"] = userId
            },
            ["exercise_event"] = new JObject
            {
                ["event_type"] = eventType,
                ["event_id"] = string.IsNullOrWhiteSpace(eventId) ? Guid.NewGuid().ToString() : eventId,
                ["exercise_id"] = exerciseId,
                ["outcome"] = outcome,
                ["timestamp"] = timestampIso
            },
            ["movement_data"] = movementData != null
                ? JObject.FromObject(movementData)
                : new JObject()
        };

        string url = CombineUrl(drupalBaseUrl, "/api/exercise");
        return await PostJsonAsync(url, payload, DrupalToken);
    }

    public async Task ConnectMqttAsync()
    {
        RequireAuthorized();

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

            if (topic == GetCommandTopic())
            {
                try
                {
                    JObject json = JObject.Parse(payload);
                    ServerCommand command = json.ToObject<ServerCommand>();
                    command.Raw = json;
                    EnqueueMainThread(() => HandleServerCommand(command));
                }
                catch (Exception ex)
                {
                    RaiseError("Mensaje MQTT inválido: " + ex.Message);
                }
            }

            return Task.CompletedTask;
        };

        mqttClient.ConnectedAsync += async args =>
        {
            Debug.Log("MQTT conectado.");
            await mqttClient.SubscribeAsync(GetCommandTopic(), MqttQualityOfServiceLevel.AtLeastOnce);
            await PublishHeartbeatAsync("connected");
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
            return;
        }

        CommandReceived?.Invoke(command);

        string action = command.action?.Trim().ToLowerInvariant();
        string userId = command.user_id;
        string routineId = command.routine_id;

        switch (action)
        {
            case "start":
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(routineId))
                {
                    RaiseError("Start ignorado: falta user_id o routine_id.");
                    return;
                }

                CurrentSession.running = true;
                CurrentSession.paused = false;
                CurrentSession.user_id = userId;
                CurrentSession.routine_id = routineId;
                CurrentSession.last_action = "start";
                StartReceived?.Invoke(command);
                break;

            case "pause":
                if (CurrentSession.running)
                {
                    CurrentSession.paused = true;
                    CurrentSession.last_action = "pause";
                    PauseReceived?.Invoke(command);
                }
                break;

            case "resume":
                if (CurrentSession.running)
                {
                    CurrentSession.paused = false;
                    CurrentSession.last_action = "resume";
                    ResumeReceived?.Invoke(command);
                }
                break;

            case "reboot":
                if (string.IsNullOrWhiteSpace(userId)) userId = CurrentSession.user_id;
                if (string.IsNullOrWhiteSpace(routineId)) routineId = CurrentSession.routine_id;

                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(routineId))
                {
                    RaiseError("Reboot ignorado: falta user_id/routine_id y no hay sesión previa.");
                    return;
                }

                CurrentSession.running = true;
                CurrentSession.paused = false;
                CurrentSession.user_id = userId;
                CurrentSession.routine_id = routineId;
                CurrentSession.last_action = "reboot";
                RebootReceived?.Invoke(command);
                break;

            case "stop":
                CurrentSession.running = false;
                CurrentSession.paused = false;
                CurrentSession.last_action = "stop";
                StopReceived?.Invoke(command);
                break;

            default:
                RaiseError("Acción MQTT desconocida: " + command.action);
                break;
        }

        _ = PublishHeartbeatAsync("connected");
    }

    public async Task PublishHeartbeatAsync(string status = "connected")
    {
        if (mqttClient == null || !mqttClient.IsConnected)
        {
            return;
        }

        JObject payload = new JObject
        {
            ["device_id"] = DeviceId,
            ["token"] = DrupalToken,
            ["status"] = status,
            ["timestamp"] = DateTimeOffset.UtcNow.ToString("o"),
            ["session"] = JObject.FromObject(CurrentSession)
        };

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
                    await PublishHeartbeatAsync("connected");
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

    public string right_controller_x;
    public string right_controller_y;
    public string right_controller_z;

    public string head_x;
    public string head_y;
    public string head_z;

    public static MovementData FromUnityPositions(Vector3 leftController, Vector3 rightController, Vector3 head)
    {
        return new MovementData
        {
            left_controller_x = leftController.x.ToString("F4"),
            left_controller_y = leftController.y.ToString("F4"),
            left_controller_z = leftController.z.ToString("F4"),

            right_controller_x = rightController.x.ToString("F4"),
            right_controller_y = rightController.y.ToString("F4"),
            right_controller_z = rightController.z.ToString("F4"),

            head_x = head.x.ToString("F4"),
            head_y = head.y.ToString("F4"),
            head_z = head.z.ToString("F4")
        };
    }
}
