using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CallibrationDisplayManager : MonoBehaviour
{

    public CallibrationSystem system;
    private PlayerInput input;
    private float progress;
    public int secondsRequired;
    private bool detectInput;

    public GameObject messageWindow;
    public GameObject successWindow;

    public TextMeshProUGUI successText;
    public TextMeshProUGUI dialogText;
    public Image bar;

    private void Start()
    {
        input = new PlayerInput();
        input.Enable();
    }

    private void Update()
    {
        if (detectInput && input.PlayerDefault.Press.IsPressed())
        {
            progress += Time.deltaTime / secondsRequired;
            if(progress >= 1.0f)
            {
                progress = 0;
                system.HandleCallibration();
                detectInput = false;
            }

        } else
        {
            progress = 0;
        }

        bar.fillAmount = progress;
    }

    public void ShowMessagePanel(string text)
    {
        detectInput = true;
        this.successWindow.SetActive(false);
        this.messageWindow.SetActive(true);
        this.dialogText.text = text;
    }

    public void ShowSuccessPanel(string text)
    {
        detectInput = false;
        this.messageWindow.SetActive(false);
        this.successWindow.SetActive(true);
        this.successText.text = text;
    }

    private void OnDestroy()
    {
        input.Disable();
    }

    private void OnEnable()
    {
        detectInput = true;
        input?.Enable();
        bar.fillAmount = 0;
        progress = 0;
        this.system.StartCallibration();
    }

}
