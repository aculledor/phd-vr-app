using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class RehabTimerScript : MonoBehaviour
{
    public TextMeshProUGUI text;
    [SerializeField]
    private RoutineManager routineManager;
    private int seconds_left;
    private bool isActive;


    private IEnumerator Countdown()
    {
        while(seconds_left >= 0)
        {
            this.text.text = TimeString;
            this.seconds_left--;
            yield return new WaitForSeconds(1.0f);
        }
    }
    
    private int secondsToSS(int time)
    {
        return time % 60;
    }

    private int secondsToMM(int time)
    {
        return time / 60;
    }
    
    private string TimeString
    { 
        get { return $"{secondsToMM(seconds_left)}:{secondsToSS(seconds_left).ToString("D2")}"; }
    }

    private void Start()
    {
        EventBus.Subscribe(EnableGameObject, GameEvent.START_REHAB);
        EventBus.Subscribe(DisableGameObject, GameEvent.END_REHAB);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe(EnableGameObject, GameEvent.START_REHAB);
        EventBus.Unsubscribe(DisableGameObject, GameEvent.END_REHAB);
    }

    private void EnableGameObject()
    {
        isActive = true;
        text.gameObject.SetActive(true);
        seconds_left = this.routineManager.selectedRoutine.routineDuration;
        this.text.text = TimeString;
        StartCoroutine(Countdown());
    }

    private void DisableGameObject()
    {
        isActive = false;
        text.gameObject.SetActive(false);
    }
    private void OnApplicationFocus(bool focus)
    {
        if (!focus)
        {
            text.gameObject.SetActive(false);
        }
        else
        {
            text.gameObject.SetActive(isActive);
        }
    }
}
