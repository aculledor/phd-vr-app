using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ObstacleSpawner : MonoBehaviour
{
    [SerializeField]
    public RoutineManager routineManager;
    public UserManager userManager;
    [SerializeField]
    private float serverFallbackHeight = 1.65f;
    [SerializeField]
    private float serverDefaultTargetOffset = 0.35f;
    //Routine obstacles
    [SerializeField]
    private List<WallItem> wallList;
    private Dictionary<ObstacleType, Dictionary<ObstacleLane, GameObject>> wallObstacles;

    //Target obstacles
    [SerializeField]
    private List<TargetItem> targetObstacles;
    private Dictionary<ObstacleType, GameObject> targetDictionary;

    //Transform positions
    [SerializeField]
    private List<LaneItem> positionsList;
    private Dictionary<ObstacleLane, Transform> positionsDictionary;

    [SerializeField]
    private List<DifficultyItem> difficultyList;
    private Dictionary<DifficultyLevel, float> difficultyDictionary;


    //Possible routines
    public List<SubRoutines> subroutines;
    public Transform standTrackerTransform;
    public PlayerTracker playerTracker;
    //User parameters
    [NonSerialized]
    public float standingHeight;
    [NonSerialized]
    public float squattingHeight;
    [NonSerialized]
    public float targetHeight;
    [NonSerialized]
    public float targetOffset;

    //Spawned obstacles list
    private List<GameObject> spawnedObstacles;
    private Coroutine activeSpawnCoroutine;
    private Coroutine activeEndCoroutine;
    private bool isSpawning;


    [SerializeField]
    private CanvasGroup screenFadeCanvas;
    [SerializeField]
    private float screenFadeTime = 1.0f;

    [NonSerialized]
    public float timeDifference;


    public float movementSpeed;

    ///Retrieve the time it'll take the obstacle to arrive to the user's position.
    public float ObstacleArrivalTime
    {
        get
        {
            ResolvePlayerTracker();

            if (playerTracker == null)
            {
                Debug.LogError("ObstacleSpawner necesita PlayerTracker para calcular ObstacleArrivalTime.", this);
                return 0.0f;
            }

            if (positionsDictionary == null)
            {
                Debug.LogError("ObstacleSpawner positionsDictionary todavía no está inicializado.", this);
                return 0.0f;
            }

            if (!positionsDictionary.TryGetValue(ObstacleLane.MID_LANE, out Transform midLaneTransform))
            {
                Debug.LogError("ObstacleSpawner no tiene una entrada MID_LANE en positionsDictionary.", this);
                return 0.0f;
            }

            if (midLaneTransform == null)
            {
                Debug.LogError("ObstacleSpawner tiene MID_LANE en positionsDictionary, pero su Transform está vacío/null. Revisa Positions List en el Inspector.", this);
                return 0.0f;
            }

            if (movementSpeed <= 0.0f)
            {
                Debug.LogError("ObstacleSpawner tiene movementSpeed <= 0. No se puede calcular ObstacleArrivalTime.", this);
                return 0.0f;
            }

            float playerZ = playerTracker.PlayerPosition.z;
            float obstacleStartZ = midLaneTransform.position.z;

            float playerDistance = Mathf.Abs(playerZ - obstacleStartZ);

            return playerDistance / movementSpeed;
        }
    }


    private void Start()
    {
        ResolvePlayerTracker();

        this.wallObstacles = new Dictionary<ObstacleType, Dictionary<ObstacleLane, GameObject>>();
        this.targetDictionary = new Dictionary<ObstacleType, GameObject>();
        this.positionsDictionary = new Dictionary<ObstacleLane, Transform>();
        this.difficultyDictionary = new Dictionary<DifficultyLevel, float>();
        this.spawnedObstacles = new List<GameObject>();
        InitializeDictionaries();
    }

    private PlayerTracker ResolvePlayerTracker()
    {
        if (playerTracker != null)
        {
            return playerTracker;
        }

        if (ServiceLocator.Instance != null && ServiceLocator.Instance.PlayerTracker != null)
        {
            playerTracker = ServiceLocator.Instance.PlayerTracker;
        }

        if (playerTracker == null)
        {
            playerTracker = FindObjectOfType<PlayerTracker>();
        }

        return playerTracker;
    }

    //Initialize the obstacles and elements defined in the editor.
    private void InitializeDictionaries()
    {
        foreach (WallItem elem in wallList)
        {
            Dictionary<ObstacleLane, GameObject> dictionary;

            if (wallObstacles.TryGetValue(elem.type, out dictionary))
            {
                dictionary.Add(elem.lane, elem.obj);
            }
            else
            {
                dictionary = new Dictionary<ObstacleLane, GameObject>();
                dictionary.Add(elem.lane, elem.obj);
                wallObstacles.Add(elem.type, dictionary);
            }
        }

        foreach (TargetItem elem in targetObstacles)
        {
            targetDictionary.Add(elem.type, elem.obj);
        }

        foreach(LaneItem elem in positionsList)
        {
            positionsDictionary.Add(elem.lane, elem.position);
        }

        foreach (DifficultyItem item in difficultyList)
        {
            difficultyDictionary.Add(item.difficultyLevel, item.timeInterval);
        }

        wallList.Clear();
        targetObstacles.Clear();
        positionsList.Clear();
        difficultyList.Clear();
    }

    private void InitializeCallibrationParameters()
    {
        if (routineManager.selectedRoutine is ServerRoutineData serverRoutine)
        {
            float serverHeight = serverRoutine.userHeightMeters > 0.0f
                ? serverRoutine.userHeightMeters
                : serverFallbackHeight;

            standingHeight = serverHeight;
            squattingHeight = serverHeight;
            targetHeight = serverHeight;
            targetOffset = serverDefaultTargetOffset;
            standTrackerTransform.position = new Vector3(0, standingHeight + 0.075f, 0);
            return;
        }

        UserData user = userManager != null ? userManager.activeUser : null;
        if (user == null)
        {
            Debug.LogWarning("No hay usuario local activo para una rutina local; se usan parámetros de calibración por defecto.");
            standingHeight = serverFallbackHeight;
            squattingHeight = serverFallbackHeight;
            targetHeight = serverFallbackHeight;
            targetOffset = serverDefaultTargetOffset;
            standTrackerTransform.position = new Vector3(0, standingHeight + 0.075f, 0);
            return;
        }

        standingHeight = user.standingHeight;
        squattingHeight = user.squattingHeight;
        targetHeight = user.targetYOffset;
        targetOffset = user.targetXOffset;
        standTrackerTransform.position = new Vector3(0, standingHeight + 0.075f, 0);
    }

    private IEnumerator SpawnObstacleList(FullRoutineItem[] items)
    {
        foreach (FullRoutineItem item in items)
        {
            EventBus.SetActiveExerciseItem(item);

            if (item.spawnObstacle)
            {
                ApplyExerciseOverrides(item);
                CreateObstacle(item);
            }

            yield return new WaitForSeconds(item.timeUntilNext);
            EventBus.ClearActiveExerciseItem(item);
        }
    }

    public void StartSpawning()
    {
        ResolvePlayerTracker();

        if (playerTracker == null)
        {
            Debug.LogError("No se puede iniciar la rutina: ObstacleSpawner no tiene PlayerTracker.", this);
            return;
        }

        StopActiveCoroutines();
        EventBus.ClearActiveExerciseItem();
        ClearSpawnedObstacles();
        InitializeCallibrationParameters();

        FullRoutineItem[] patientRoutine = routineManager.selectedRoutine.GenerateRoutine();

        isSpawning = true;
        EventBus.PublishEvent(GameEvent.START_REHAB);
        activeSpawnCoroutine = StartCoroutine(SpawnObstacleList(patientRoutine));
        activeEndCoroutine = StartCoroutine(EndSpawning(activeSpawnCoroutine));
    }

    public void StopSpawning(bool publishEndEvent = true, bool showResults = true)
    {
        if (!isSpawning && spawnedObstacles.Count == 0)
        {
            return;
        }

        StopActiveCoroutines();
        EventBus.ClearActiveExerciseItem();
        ClearSpawnedObstacles();
        isSpawning = false;
        Time.timeScale = 1.0f;

        if (publishEndEvent)
        {
            EventBus.PublishEvent(GameEvent.END_REHAB);
        }

        if (showResults)
        {
            routineManager.handleEnd();
        }
    }

    private void StopActiveCoroutines()
    {
        if (activeSpawnCoroutine != null)
        {
            StopCoroutine(activeSpawnCoroutine);
            activeSpawnCoroutine = null;
        }

        if (activeEndCoroutine != null)
        {
            StopCoroutine(activeEndCoroutine);
            activeEndCoroutine = null;
        }
    }

    private void ClearSpawnedObstacles()
    {
        foreach (GameObject obs in this.spawnedObstacles)
        {
            if (obs != null)
            {
                Destroy(obs);
            }
        }

        spawnedObstacles.Clear();
    }

    private void ApplyExerciseOverrides(FullRoutineItem item)
    {
        if (item.heightOverrideMeters <= 0.0f)
        {
            return;
        }

        if (item.obstacleElement == ObstacleType.LEFT_HIT || item.obstacleElement == ObstacleType.RIGHT_HIT
            || item.obstacleElement == ObstacleType.CROSS_LEFT_HIT || item.obstacleElement == ObstacleType.CROSS_RIGHT_HIT)
        {
            targetHeight = item.heightOverrideMeters;
        }
        else if (item.obstacleElement == ObstacleType.CROUCH_WALL)
        {
            squattingHeight = item.heightOverrideMeters;
        }
        else if (item.obstacleElement == ObstacleType.STAND_WALL)
        {
            standingHeight = item.heightOverrideMeters;
        }
    }

    //Remove an obstacle from the scene and the instanced elements list.
    public void RemoveObstacle(GameObject obj)
    {
        this.spawnedObstacles.Remove(obj);
        Destroy(obj);
    }


    //Instance an obstacle in the game world
    private void CreateObstacle(FullRoutineItem item)
    {
        GameObject obj = null;
        Dictionary<ObstacleLane, GameObject> element_dictionary;

        if (wallObstacles.TryGetValue(item.obstacleElement, out element_dictionary)
            && element_dictionary.TryGetValue(item.obstacleLane, out obj))
        {
            obj = Instantiate(obj, positionsDictionary[ObstacleLane.MID_LANE]);
        }
        else if (targetDictionary.TryGetValue(item.obstacleElement, out obj))
        {
            obj = Instantiate(obj, positionsDictionary[item.obstacleLane]);
        }

        if (obj != null)
        {
            AttachServerExerciseContext(obj, item);
            this.spawnedObstacles.Add(obj);
        }
    }

    private void AttachServerExerciseContext(GameObject obj, FullRoutineItem item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.serverExerciseId))
        {
            return;
        }

        ServerExerciseContext context = obj.GetComponent<ServerExerciseContext>();
        if (context == null)
        {
            context = obj.AddComponent<ServerExerciseContext>();
        }

        context.Initialize(item);
    }

    //Finish the routine session and delete the obstacles present in the scene.
    private IEnumerator EndSpawning(Coroutine spawnCoroutine)
    {
        yield return new WaitForSeconds(routineManager.selectedRoutine.routineDuration);
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        yield return FadeScreen(1.0f);

        EventBus.ClearActiveExerciseItem();
        ClearSpawnedObstacles();
        isSpawning = false;
        activeSpawnCoroutine = null;
        activeEndCoroutine = null;

        EventBus.PublishEvent(GameEvent.END_REHAB);
        routineManager.handleEnd();
        yield return FadeScreen(0.0f);
    }
    private IEnumerator FadeScreen(float targetAlpha)
    {
        if (screenFadeCanvas == null)
        {
            Debug.LogWarning("ObstacleSpawner no tiene screenFadeCanvas asignado. Se omite el fundido de pantalla.", this);
            yield break;
        }

        float fadeDuration = Mathf.Max(0.0f, screenFadeTime);

        if (fadeDuration <= 0.0f)
        {
            screenFadeCanvas.alpha = targetAlpha;
            yield break;
        }

        float startAlpha = screenFadeCanvas.alpha;
        float elapsed = 0.0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            screenFadeCanvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        screenFadeCanvas.alpha = targetAlpha;
    }

}

[System.Serializable]
struct WallItem
{
    public ObstacleLane lane;
    public ObstacleType type;
    public GameObject obj;
}

[System.Serializable]
struct TargetItem
{
    public ObstacleType type;
    public GameObject obj;
}

[System.Serializable]
struct LaneItem
{
    public ObstacleLane lane;
    public Transform position;
}

[System.Serializable]
struct DifficultyItem
{
    public DifficultyLevel difficultyLevel;
    public float timeInterval;
}
