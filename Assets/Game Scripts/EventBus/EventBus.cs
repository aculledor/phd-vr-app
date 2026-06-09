using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Runtime.Serialization;


//INCLUIR MOVIMIENTO, LEVANTARSE,

public enum GameEvent
{
    START_REHAB,END_REHAB,
    RIGHT_HIT_SUCCESS, RIGHT_HIT_FAILED,
    LEFT_HIT_SUCCESS, LEFT_HIT_FAILED,
    CROSS_RIGHT_HIT_SUCCESS, CROSS_RIGHT_HIT_FAILED,
    CROSS_LEFT_HIT_SUCCESS, CROSS_LEFT_HIT_FAILED,
    SQUAT_LEFT_SUCCESS, SQUAT_MID_SUCCESS, SQUAT_RIGHT_SUCCESS,
    SQUAT_LEFT_FAILED, SQUAT_MID_FAILED, SQUAT_RIGHT_FAILED,
    MOVEMENT_LEFT_SUCCESS, MOVEMENT_MID_SUCCESS, MOVEMENT_RIGHT_SUCCESS,
    MOVEMENT_LEFT_FAILED, MOVEMENT_MID_FAILED, MOVEMENT_RIGHT_FAILED
}

public class EventBus
{
    public static FullRoutineItem CurrentExerciseItem { get; private set; }
    public static FullRoutineItem ActiveExerciseItem { get; private set; }

    public static void SetActiveExerciseItem(FullRoutineItem exerciseItem)
    {
        ActiveExerciseItem = exerciseItem;
    }

    public static void ClearActiveExerciseItem(FullRoutineItem exerciseItem = null)
    {
        if (exerciseItem == null || ActiveExerciseItem == exerciseItem)
        {
            ActiveExerciseItem = null;
        }
    }

    private static readonly IDictionary<GameEvent, UnityEvent>
        EventsDictionary = new Dictionary<GameEvent, UnityEvent>();
    private static readonly List<IBusEventCallback> listeners = new List<IBusEventCallback>();
    public static void Subscribe(UnityAction call, params GameEvent[] sub_events)
    {

        UnityEvent stored_event;

        //Subscribe method to event. The method will be invoked when an event from the
        //sub_events array is published.
        foreach(GameEvent gameEvent in sub_events) { 
            if(EventsDictionary.TryGetValue(gameEvent, out stored_event))
            {
                stored_event.AddListener(call);
            } else
            {
                stored_event = new UnityEvent();
                stored_event.AddListener(call);
                EventsDictionary.Add(gameEvent, stored_event);
            }
        }
    }

    public static void Unsubscribe(UnityAction call, params GameEvent[] unsub_events)
    {
        UnityEvent retrieved_event;

        //Remove method subscription from list
        foreach(GameEvent gameEvent in unsub_events) { 
            if(EventsDictionary.TryGetValue(gameEvent, out retrieved_event))
            {
                retrieved_event.RemoveListener(call);
            }
        }
    }

    public static void CaptureEvents(IBusEventCallback listener)
    {
        //Check if listener is already subscribed before registering.
        if (!listeners.Contains(listener))
        {
            listeners.Add(listener);
        }
    }

    public static void StopCapturingEvents(IBusEventCallback listener)
    {
        listeners.Remove(listener);
    }

    public static void PublishEvent(GameEvent gameEvent)
    {
        PublishEvent(gameEvent, null);
    }

    public static void PublishEvent(GameEvent gameEvent, FullRoutineItem exerciseItem)
    {
        UnityEvent invoked_event;

        Debug.Log(gameEvent.ToString());

        FullRoutineItem previousExerciseItem = CurrentExerciseItem;
        CurrentExerciseItem = exerciseItem;

        try
        {
            //Notify all the subscribers who process the game event.
            foreach(IBusEventCallback listener in listeners)
            {
                listener.HandleEvent(gameEvent);
            }

            //Invoke the registered methods.
            if (EventsDictionary.TryGetValue(gameEvent, out invoked_event))
            {
                invoked_event.Invoke();
            }
        }
        finally
        {
            CurrentExerciseItem = previousExerciseItem;
        }
    }

}
