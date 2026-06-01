using System.Collections;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Collections;
using OVRSimpleJSON;
using System.Text;

public class RehabLogger : MonoBehaviour, IBusEventCallback
{
    //Inverbis token used to get the API access token (for the following upload,
    //dataset update...).
    private readonly string refreshToken = "eyJhbGciOiJIUzI1NiIsInR5cCIgOiAiSldUIiwia2lkIiA6ICJkY2Q3ZDRmZS02Y2U3LTQ2ODktOGUwYi0xMDJhMDQyMGQ2YWEifQ.eyJpYXQiOjE3NDkyMTkzMDgsImp0aSI6ImQwMzZkOGIyLTAyZDYtNDg1YS1iYTBmLTk5ZGJkMDNlMzI4OCIsImlzcyI6Imh0dHBzOi8vaWRlbnRpdHkuaW52ZXJiaXNhbmFseXRpY3MuY29tL2F1dGgvcmVhbG1zL3Byb2Nlc3NtaW5pbmciLCJhdWQiOiJodHRwczovL2lkZW50aXR5LmludmVyYmlzYW5hbHl0aWNzLmNvbS9hdXRoL3JlYWxtcy9wcm9jZXNzbWluaW5nIiwic3ViIjoiYmI3MWUxMzctZTM3MC00MTM0LWIyYzQtNGQ1MzAxMjY2ZmM1IiwidHlwIjoiT2ZmbGluZSIsImF6cCI6InByb2Nlc3NtaW5pbmctZnJvbnRlbmQiLCJzZXNzaW9uX3N0YXRlIjoiYjhhZWY2MjMtY2M5Yi00N2Q3LTgxZTgtZjc1ZTM0MzlmZTY1Iiwic2NvcGUiOiJwcm9maWxlIG9mZmxpbmVfYWNjZXNzIGVtYWlsIn0.4WviAMD0b95sdmaSY40Mx9U1YhW5j_nJUlaqR7igM_s";

    private string accessToken;
    private string uuid;
    private string fileToken;

    public UserManager userManager;
    public ScoreSystem scoreSystem;
    public RoutineManager routineManager;
    private List<EventLogItem> eventList;
    public PlayerTracker playerTracker;

    private DateTime startTime;

    private void Start()
    {

        uuid = PlayerPrefs.HasKey("datasetUuid") ? PlayerPrefs.GetString("datasetUuid") : null;
        this.eventList = new List<EventLogItem>();

        // If the app is not synced with the API and there exists a log file, then it'll
        // attempt to upload it.
        if (!PlayerPrefs.HasKey("synced") && StorageSystem.ExistsLogFile()) {
            StartCoroutine(GetAccessToken());
        }
    }

    private void OnEnable()
    {
        EventBus.CaptureEvents(this);
        EventBus.Subscribe(HandleRehabStart, GameEvent.START_REHAB);
        EventBus.Subscribe(HandleRehabEnd, GameEvent.END_REHAB);
    }

    private void OnDisable()
    {
        EventBus.StopCapturingEvents(this);
        EventBus.Unsubscribe(HandleRehabEnd, GameEvent.END_REHAB);
        EventBus.Subscribe(HandleRehabStart, GameEvent.START_REHAB);
    }

    public void HandleEvent(GameEvent gameEvent)
    {
        EventLogItem item;
        //In case of a successful squat it'll include the data related to the lowest height
        //obtained during the squat process.
        switch (gameEvent)
        {
            case GameEvent.SQUAT_LEFT_SUCCESS:
            case GameEvent.SQUAT_MID_SUCCESS:
            case GameEvent.SQUAT_RIGHT_SUCCESS:
                item = new EventLogItem(gameEvent, DateTime.UtcNow, playerTracker.LeftHitDistance,
                    playerTracker.RightHitDistance, playerTracker.SquatMinHeight, playerTracker.HorizontalMovement);
                break;
            default:
                item = new EventLogItem(gameEvent, DateTime.UtcNow, playerTracker.LeftHitDistance,
                    playerTracker.RightHitDistance, playerTracker.CurrentSquatHeight, playerTracker.HorizontalMovement);
                break;
        }

        this.eventList.Add(item);
        playerTracker.ResetTrackingData();
    }

    private void HandleRehabStart()
    {
        startTime = DateTime.UtcNow;
    }
    private void HandleRehabEnd()
    {
        PlayerPrefs.DeleteKey("synced");
        // TODO : Actualizar esto

        userManager.AddScoreInformation(new ScoreRecords(routineManager.selectedRoutine.routineDuration,
            scoreSystem.comboMax, scoreSystem.score, scoreSystem.squatSuccess, scoreSystem.movementSuccess,
            scoreSystem.punchSuccess, scoreSystem.squatTotal, scoreSystem.movementTotal,
            scoreSystem.punchTotal,startTime));

        SendLiveData();

        eventList.Clear();
    }

    //Performs the dataset logging in memory and the subsequent upload.
    private void SendLiveData()
    {

        IRoutineData routineData = routineManager.selectedRoutine;

        string saveData = "";
        foreach (EventLogItem item in eventList)
        {
            saveData += $"{userManager.activeUser.identifier+startTime.GetHashCode()};" +
                $"{item.gameEvent};{item.timeStamp.ToString("dd/MM/yyyy HH:mm:ss")};{routineData.allowSquat};" +
                $"{routineData.allowMovement};{routineData.allowPunch};" +
                $"{routineData.allowCrossPunch};{userManager.activeUser.identifier};" +
                $"{item.leftHandDistance.ToString(System.Globalization.CultureInfo.InvariantCulture)};" +
                $"{item.rightHandDistance.ToString(System.Globalization.CultureInfo.InvariantCulture)};" +
                $"{item.relativeHeight.ToString(System.Globalization.CultureInfo.InvariantCulture)};" +
                $"{item.lateralMovement.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                $"{Environment.NewLine}";
        }

        StorageSystem.SaveResults(saveData);

        StartCoroutine(GetAccessToken());
    }

    private IEnumerator GetAccessToken()
    { 
        WWWForm genTokenForm = new WWWForm();
        genTokenForm.AddField("grant_type", "refresh_token");
        genTokenForm.AddField("refresh_token", this.refreshToken);
        genTokenForm.AddField("client_id", "processmining-frontend");
        UnityWebRequest genTokenCall = UnityWebRequest.Post("https://identity.inverbisanalytics.com/auth/realms" +
            "/processmining/protocol/openid-connect/token", genTokenForm);
        genTokenCall.certificateHandler = new InverbisCertificateHandler();
        genTokenCall.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
        
        yield return genTokenCall.SendWebRequest();
        
        genTokenCall.uploadHandler.Dispose();
        genTokenCall.certificateHandler.Dispose();
        
        if(genTokenCall.result == UnityWebRequest.Result.Success)
        {
            string accessJson = genTokenCall.downloadHandler.text;
            JSONNode accessTokenResponse = JSON.Parse(accessJson);
            accessToken = accessTokenResponse["access_token"];


            if(uuid == null)
            {
                yield return CreateDataset();

            } else
            {
                yield return GetFileToken();
            }

        }
    }

    private IEnumerator CreateDataset()
    {
        JSONObject jsonObject = new JSONObject();
        jsonObject.Add("name", "Rehabilitation Dataset");
        jsonObject.Add("description", "Dataset with stored information.");
        jsonObject.Add("projectUid", "60ccf19d-5e01-49a4-aecf-45860ae72b46");

        UnityWebRequest createDatasetCall = new UnityWebRequest("https://api.inverbisanalytics.com/v0/processmining/datasets",
            "POST");

        createDatasetCall.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonObject.ToString()));
        createDatasetCall.downloadHandler = new DownloadHandlerBuffer();
        createDatasetCall.certificateHandler = new InverbisCertificateHandler();
        createDatasetCall.SetRequestHeader("Content-Type", "application/json");
        createDatasetCall.SetRequestHeader("Accept", " */*");
        createDatasetCall.SetRequestHeader("Authorization", "Bearer " + accessToken);
        
        yield return createDatasetCall.SendWebRequest();
        
        createDatasetCall.uploadHandler.Dispose();
        createDatasetCall.certificateHandler.Dispose();
        
        if (createDatasetCall.result == UnityWebRequest.Result.Success)
        {
            uuid = createDatasetCall.GetResponseHeader("X-Location-uid");
            StartCoroutine(GetFileToken());

        }
    }

    private IEnumerator GetFileToken()
    {

        UnityWebRequest uploadTokenCall = UnityWebRequest.PostWwwForm($"https://api.inverbisanalytics.com/v0/processmining/" +
            $"datasets/{uuid}/uploads/initiate", "");
        uploadTokenCall.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        uploadTokenCall.SetRequestHeader("Content-Type", "application/json");
        uploadTokenCall.SetRequestHeader("Accept", " */*");
        uploadTokenCall.certificateHandler = new InverbisCertificateHandler();
        
        yield return uploadTokenCall.SendWebRequest();
        
        uploadTokenCall.certificateHandler.Dispose();
        
        if(uploadTokenCall.result == UnityWebRequest.Result.Success) {
            JSONNode uploadTokenParsed = JSON.Parse(uploadTokenCall.downloadHandler.text);
            fileToken = uploadTokenParsed["token"];
            yield return FileUpload();

        }
    }


    private IEnumerator FileUpload()
    {

        byte[] csvData = StorageSystem.LoadLogFileBinary();

        UnityWebRequest uploadFileCall = new UnityWebRequest("https://api.inverbisanalytics.com/v0" +
            $"/processmining/uploads/datasets/{uuid}/{fileToken}", "POST");
        uploadFileCall.uploadHandler = new UploadHandlerRaw(csvData);
        uploadFileCall.downloadHandler = new DownloadHandlerBuffer();
        uploadFileCall.certificateHandler = new InverbisCertificateHandler();
        uploadFileCall.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        uploadFileCall.SetRequestHeader("Content-Type", "application/octet-stream");
        uploadFileCall.SetRequestHeader("Accept", " */*");
        uploadFileCall.SetRequestHeader("X-File-Name", "event_log.csv");

        yield return uploadFileCall.SendWebRequest();
        uploadFileCall.uploadHandler.Dispose();
        uploadFileCall.certificateHandler.Dispose();
        
        if(uploadFileCall.result == UnityWebRequest.Result.Success)
        {
            if (!PlayerPrefs.HasKey("datasetUuid"))
            {
                yield return ConfigDataset();

            }else
            {
                yield return ProcessEventsData();
            }

        } 
    }
    
    private IEnumerator ConfigDataset()
    {
        JSONObject datasetConfigBody = new JSONObject();
        datasetConfigBody.Add("traceColumn", "Identifier");
        datasetConfigBody.Add("activityColumn", "GameEvent");
        datasetConfigBody.Add("startTimeColumn", "EventTime");

        JSONArray columns = new JSONArray();
        columns.Add(CreateColumnObject("Identifier", "STRING", null));
        columns.Add(CreateColumnObject("GameEvent", "STRING", null));
        columns.Add(CreateColumnObject("EventTime", "TIMESTAMP", "dd/MM/yyyy[ HH][:m][:s][.][,][SSSSSS][SSS][SS][S][XXX]"));
        columns.Add(CreateColumnObject("SquatsEnabled", "BOOLEAN", null));
        columns.Add(CreateColumnObject("MovementEnabled", "BOOLEAN", null));
        columns.Add(CreateColumnObject("TargetsEnabled", "BOOLEAN", null));
        columns.Add(CreateColumnObject("CrossPunchEnabled", "BOOLEAN", null));
        columns.Add(CreateColumnObject("PatientIdentifier", "STRING", null));
        columns.Add(CreateColumnObject("LeftControllerDistance", "DECIMAL", null));
        columns.Add(CreateColumnObject("RightControllerDistance", "DECIMAL", null));
        columns.Add(CreateColumnObject("SquatHeight", "DECIMAL", null));
        columns.Add(CreateColumnObject("HorizontalMovement", "DECIMAL", null));

        datasetConfigBody.Add("columns", columns);
        UnityWebRequest configDatasetCall = new UnityWebRequest($"https://api.inverbisanalytics.com/v0/processmining/" +
            $"datasets/{uuid}/configurations", "POST");
        configDatasetCall.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(datasetConfigBody.ToString()));
        configDatasetCall.downloadHandler = new DownloadHandlerBuffer();
        configDatasetCall.certificateHandler = new InverbisCertificateHandler();
        configDatasetCall.SetRequestHeader("Content-Type", "application/json;charset=utf-8");
        configDatasetCall.SetRequestHeader("Authorization", "Bearer " + accessToken);
        configDatasetCall.SetRequestHeader("Accept", "*/*");
        configDatasetCall.certificateHandler.Dispose();
        
        yield return configDatasetCall.SendWebRequest();
        
        configDatasetCall.uploadHandler.Dispose();
        configDatasetCall.certificateHandler.Dispose();
        
        if(configDatasetCall.result == UnityWebRequest.Result.Success)
        {
            yield return ProcessEventsData();
        }
    }

    private IEnumerator ProcessEventsData()
    {
        UnityWebRequest processWebRequest = new UnityWebRequest($"https://api.inverbisanalytics.com/v0/processmining/" +
                                $"datasets/{uuid}/uploads/finish", "POST");
        processWebRequest.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        processWebRequest.SetRequestHeader("Content-Type", "application/json");
        processWebRequest.certificateHandler = new InverbisCertificateHandler();
        
        yield return processWebRequest.SendWebRequest();
        
        processWebRequest.certificateHandler.Dispose();
        
        if (processWebRequest.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Process Call.");
            PlayerPrefs.SetInt("synced", 0);
            PlayerPrefs.SetString("datasetUuid", uuid);

        }
    }

    private JSONObject CreateColumnObject(string columnName, string columnType, string format)
    {
        JSONObject obj = new JSONObject();
        obj.Add("name", columnName);
        obj.Add("type", columnType);
        if (format != null) { 
            obj.Add("format", format);
        }
        else
        {
            obj.Add("format",  JSONNull.CreateOrGet() );
        }
        return obj;
    }


    private void OnDestroy()
    {
        PlayerPrefs.Save();
    }
}


//Data extracted during the process of performing an exercise
[Serializable]
public class EventLogItem
{
    public GameEvent gameEvent;
    public DateTime timeStamp;
    public float leftHandDistance;
    public float rightHandDistance;
    public float relativeHeight;
    public float lateralMovement;

    public EventLogItem(GameEvent gameEvent, DateTime timeStamp, 
        float leftHandDistance, float rightHandDistance, float relativeHeight, 
        float lateralMovement)
    {
        this.gameEvent = gameEvent;
        this.timeStamp = timeStamp;
        this.leftHandDistance = leftHandDistance;
        this.rightHandDistance = rightHandDistance;
        this.relativeHeight = relativeHeight;
        this.lateralMovement = lateralMovement;
    }
}