using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Rutina recibida desde Drupal por GET /api/glass. Implementa IRoutineData para que el
/// spawner use exactamente los ejercicios remotos en vez de generar una plantilla local.
/// </summary>
[Serializable]
public class ServerRoutineData : IRoutineData
{
    public string routineId;
    public string userId;
    public float userHeightMeters;

    [SerializeField]
    private FullRoutineItem[] items;

    public bool allowSquat { get; private set; }
    public bool allowMovement { get; private set; }
    public bool allowPunch { get; private set; }
    public bool allowCrossPunch { get; private set; }

    public int routineDuration { get; private set; }
    public FullRoutineItem[] generatedRoutine => items;

    public FullRoutineItem[] GenerateRoutine()
    {
        return items ?? new FullRoutineItem[0];
    }

    public static bool TryCreateFromGlassPayload(JObject payload, out ServerRoutineData routine, out string error)
    {
        routine = null;
        error = null;

        JToken routineToken = payload?["unity_session_routines"]?.First;
        if (routineToken == null || routineToken.Type == JTokenType.Null)
        {
            error = "La respuesta de /api/glass no contiene unity_session_routines.";
            return false;
        }

        JArray exercises = routineToken["exercises"] as JArray;
        if (exercises == null || exercises.Count == 0)
        {
            error = "La rutina recibida no contiene ejercicios.";
            return false;
        }

        ServerRoutineData result = new ServerRoutineData
        {
            routineId = ReadString(routineToken, "routine_id"),
            userId = ReadString(routineToken, "user_id"),
            userHeightMeters = CmToMeters(ReadFloat(routineToken, "height"))
        };

        List<FullRoutineItem> routineItems = new List<FullRoutineItem>();
        foreach (JToken exerciseToken in exercises)
        {
            AddExerciseItems(result, routineItems, exerciseToken);
        }

        result.items = routineItems.ToArray();
        result.routineDuration = Mathf.CeilToInt(TotalRoutineSeconds(result.items));
        routine = result;
        return true;
    }

    private static void AddExerciseItems(ServerRoutineData routine, List<FullRoutineItem> routineItems, JToken exerciseToken)
    {
        string code = ReadString(exerciseToken, "exercise_type_code").Trim().ToUpperInvariant();
        int expectedResponses = Mathf.Max(1, ReadInt(exerciseToken, "expected_responses", 1));
        float timeBetweenEvents = Mathf.Max(0.1f, ReadFloat(exerciseToken, "time_between_events", 1.0f));
        float duration = Mathf.Max(timeBetweenEvents, ReadFloat(exerciseToken, "duration", timeBetweenEvents));
        float heightMeters = ExerciseHeightOrUserHeight(exerciseToken, routine.userHeightMeters);
        float distanceMeters = CmToMeters(ReadFloat(exerciseToken, "distance"));
        string exerciseId = ReadString(exerciseToken, "exercise_id");

        if (code == "REST")
        {
            routineItems.Add(FullRoutineItem.CreateWait(duration, exerciseId, code));
            return;
        }

        for (int index = 0; index < expectedResponses; index++)
        {
            FullRoutineItem item;
            switch (code)
            {
                case "HITS":
                    item = CreateTarget(index % 2 == 0 ? ObstacleType.LEFT_HIT : ObstacleType.RIGHT_HIT, code, exerciseId, heightMeters, distanceMeters);
                    routine.allowPunch = true;
                    break;
                case "LEFT_HIT":
                    item = CreateTarget(ObstacleType.LEFT_HIT, code, exerciseId, heightMeters, distanceMeters);
                    routine.allowPunch = true;
                    break;
                case "RIGHT_HIT":
                    item = CreateTarget(ObstacleType.RIGHT_HIT, code, exerciseId, heightMeters, distanceMeters);
                    routine.allowPunch = true;
                    break;
                case "CROSS_HITS":
                    item = CreateTarget(index % 2 == 0 ? ObstacleType.CROSS_LEFT_HIT : ObstacleType.CROSS_RIGHT_HIT, code, exerciseId, heightMeters, distanceMeters);
                    routine.allowCrossPunch = true;
                    break;
                case "CROSS_LEFT_HIT":
                    item = CreateTarget(ObstacleType.CROSS_LEFT_HIT, code, exerciseId, heightMeters, distanceMeters);
                    routine.allowCrossPunch = true;
                    break;
                case "CROSS_RIGHT_HIT":
                    item = CreateTarget(ObstacleType.CROSS_RIGHT_HIT, code, exerciseId, heightMeters, distanceMeters);
                    routine.allowCrossPunch = true;
                    break;
                case "MOVEMENTS_LATERAL":
                    item = CreateWall(ObstacleType.STAND_WALL, index % 2 == 0 ? ObstacleLane.LEFT_LANE : ObstacleLane.RIGHT_LANE, code, exerciseId, heightMeters, distanceMeters);
                    routine.allowMovement = true;
                    break;
                case "MOVEMENT_LEFT":
                    item = CreateWall(ObstacleType.STAND_WALL, ObstacleLane.LEFT_LANE, code, exerciseId, heightMeters, distanceMeters);
                    routine.allowMovement = true;
                    break;
                case "MOVEMENT_RIGHT":
                    item = CreateWall(ObstacleType.STAND_WALL, ObstacleLane.RIGHT_LANE, code, exerciseId, heightMeters, distanceMeters);
                    routine.allowMovement = true;
                    break;
                case "SQUAT_MID":
                    item = CreateWall(ObstacleType.CROUCH_WALL, ObstacleLane.MID_LANE, code, exerciseId, heightMeters, distanceMeters);
                    routine.allowSquat = true;
                    break;
                case "MOVEMENT_SQUATS":
                    item = CreateWall(ObstacleType.CROUCH_WALL, index % 2 == 0 ? ObstacleLane.LEFT_LANE : ObstacleLane.RIGHT_LANE, code, exerciseId, heightMeters, distanceMeters);
                    routine.allowSquat = true;
                    routine.allowMovement = true;
                    break;
                default:
                    Debug.LogWarning("Código de ejercicio recibido desde Drupal no soportado: " + code + ". Se omite manteniendo su duración como espera.");
                    item = FullRoutineItem.CreateWait(timeBetweenEvents, exerciseId, code);
                    break;
            }

            item.timeUntilNext = index == expectedResponses - 1
                ? Mathf.Max(0.1f, duration - timeBetweenEvents * (expectedResponses - 1))
                : timeBetweenEvents;
            routineItems.Add(item);
        }
    }

    private static FullRoutineItem CreateTarget(ObstacleType obstacleType, string code, string exerciseId, float heightMeters, float distanceMeters)
    {
        return FullRoutineItem.CreateObstacle(obstacleType, TargetLaneFor(obstacleType), 1.0f, exerciseId, code, heightMeters, distanceMeters);
    }

    private static FullRoutineItem CreateWall(ObstacleType obstacleType, ObstacleLane lane, string code, string exerciseId, float heightMeters, float distanceMeters)
    {
        return FullRoutineItem.CreateObstacle(obstacleType, lane, 1.0f, exerciseId, code, heightMeters, distanceMeters);
    }

    private static ObstacleLane TargetLaneFor(ObstacleType obstacleType)
    {
        return obstacleType == ObstacleType.RIGHT_HIT || obstacleType == ObstacleType.CROSS_RIGHT_HIT
            ? ObstacleLane.RIGHT_LANE
            : ObstacleLane.LEFT_LANE;
    }

    private static float TotalRoutineSeconds(IEnumerable<FullRoutineItem> routineItems)
    {
        float total = 0;
        foreach (FullRoutineItem item in routineItems)
        {
            total += item.timeUntilNext;
        }
        return total;
    }

    private static string ReadString(JToken token, string propertyName)
    {
        JToken value = token?[propertyName];
        return value == null || value.Type == JTokenType.Null ? string.Empty : value.ToString();
    }

    private static int ReadInt(JToken token, string propertyName, int fallback = 0)
    {
        return int.TryParse(ReadString(token, propertyName), out int value) ? value : fallback;
    }

    private static float ReadFloat(JToken token, string propertyName, float fallback = 0.0f)
    {
        return float.TryParse(ReadString(token, propertyName), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value)
            ? value
            : fallback;
    }

    private static float ExerciseHeightOrUserHeight(JToken exerciseToken, float userHeightMeters)
    {
        float exerciseHeightMeters = CmToMeters(ReadFloat(exerciseToken, "height"));
        return exerciseHeightMeters > 0.0f ? exerciseHeightMeters : userHeightMeters;
    }

    private static float CmToMeters(float centimeters)
    {
        return centimeters > 0 ? centimeters / 100.0f : 0.0f;
    }
}
