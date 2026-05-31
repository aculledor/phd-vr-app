using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class StepProgression : MonoBehaviour
{
    private int totalSteps;
    private int currentStep;
    public TextMeshProUGUI textMesh;

    public void StartSteps(int steps)
    {
        this.gameObject.SetActive(true);
        totalSteps = steps;
        currentStep = 1;
        textMesh.text = $"Paso 1 de {steps}";
    }

    public void NextStep()
    {
        currentStep++;
        textMesh.text = $"Paso {currentStep} de {totalSteps}";
    }

    public void HideSteps()
    {
        this.gameObject.SetActive(false);
    }

}
