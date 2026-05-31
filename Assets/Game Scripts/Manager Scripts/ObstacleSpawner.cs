using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ObstacleSpawner : MonoBehaviour
{
    [SerializeField]
    public RoutineManager routineManager;
    public UserManager userManager;
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


    public OVRScreenFade screenFade;
    
    [NonSerialized]
    public float timeDifference;


    public float movementSpeed;

    ///Retrieve the time it'll take the obstacle to arrive to the user's position.
    public float ObstacleArrivalTime
    {
        get
        {
            float playerDistance = Mathf.Abs(playerTracker.PlayerPosition.z - 
                positionsDictionary[ObstacleLane.MID_LANE].transform.position.z);

            return playerDistance / movementSpeed;
        }
    }


    private void Start()
    {
        this.wallObstacles = new Dictionary<ObstacleType, Dictionary<ObstacleLane, GameObject>>();
        this.targetDictionary = new Dictionary<ObstacleType, GameObject>();
        this.positionsDictionary = new Dictionary<ObstacleLane, Transform>();
        this.difficultyDictionary = new Dictionary<DifficultyLevel, float>();
        this.spawnedObstacles = new List<GameObject>();
        InitializeDictionaries();
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
        UserData user = userManager.activeUser;
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
            CreateObstacle(item.obstacleElement, item.obstacleLane);
            yield return new WaitForSeconds(item.timeUntilNext);
        }
    }

    public void StartSpawning()
    {

        InitializeCallibrationParameters();
        FullRoutineItem[] patientRoutine = routineManager.
            selectedRoutine.GenerateRoutine();

        EventBus.PublishEvent(GameEvent.START_REHAB);
        Coroutine spawnCoroutine =  StartCoroutine(SpawnObstacleList(patientRoutine));
        StartCoroutine(EndSpawning(spawnCoroutine));
    }

    //Remove an obstacle from the scene and the instanced elements list.
    public void RemoveObstacle(GameObject obj)
    {
        this.spawnedObstacles.Remove(obj);
        Destroy(obj);
    }


    //Instance an obstacle in the game world
    private void CreateObstacle(ObstacleType obstacle, ObstacleLane lane)
    {
        GameObject obj;
        Dictionary<ObstacleLane, GameObject> element_dictionary;

        if (wallObstacles.TryGetValue(obstacle, out element_dictionary)
            && element_dictionary.TryGetValue(lane, out obj))
        {
            obj = Instantiate(obj, positionsDictionary[ObstacleLane.MID_LANE]);
        }
        else if (targetDictionary.TryGetValue(obstacle, out obj))
        {
            obj = Instantiate(obj, positionsDictionary[lane]);
        }

        this.spawnedObstacles.Add(obj);
    }

    //Finish the routine session and delete the obstacles present in the scene.
    private IEnumerator EndSpawning(Coroutine spawnCoroutine)
    {
        yield return new WaitForSeconds(routineManager.selectedRoutine.routineDuration);
        StopCoroutine(spawnCoroutine);
        this.screenFade.FadeOut();

        yield return new WaitForSeconds(screenFade.fadeTime);

        //Delete all the instanced elements in the scene
        foreach (GameObject obs in this.spawnedObstacles)
        {
            Destroy(obs);
        }

        EventBus.PublishEvent(GameEvent.END_REHAB);
        routineManager.handleEnd();
        this.screenFade.FadeIn();
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
