using UnityEngine;

/// <summary>
/// Metadata attached to obstacles spawned from a server routine so any success/failure
/// event can be reported to Drupal with the exact exercise_id and measurements.
/// </summary>
public class ServerExerciseContext : MonoBehaviour
{
    public FullRoutineItem RoutineItem { get; private set; }

    public void Initialize(FullRoutineItem routineItem)
    {
        RoutineItem = routineItem;
    }
}
