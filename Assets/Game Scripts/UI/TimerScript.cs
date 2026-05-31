using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class TimerScript : MonoBehaviour
{
    public TextMeshProUGUI text;
    public Image loading;
    public RoutineManager routineManager;

    private float time;
    private int seconds_left;

    public void SetRemainingTime(int time_left)
    {
        seconds_left = time_left;
        text.text = seconds_left.ToString();
    }
    private void OnEnable()
    {
        time = 0;
        seconds_left = routineManager.startTime;
        text.text = seconds_left.ToString();
    }
    // Update is called once per frame
    void Update()
    {

        if (seconds_left > 0) {
            time += Time.deltaTime;

            this.loading.fillAmount = Mathf.Clamp(time, 0.0f, 1.0f);

            if(time >= 1.0f)
            {
                time -= 1.0f;
                seconds_left -= 1;

                if(seconds_left == 0)
                {
                    text.text = "¡YA!";
                } else
                {
                    text.text = seconds_left.ToString();
                }

            }
        }
    }
    
}
