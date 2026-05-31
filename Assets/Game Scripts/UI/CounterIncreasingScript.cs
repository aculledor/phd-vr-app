using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CounterIncreasingScript : MonoBehaviour
{

    private int dynamicValue;
    private int currentValue;

    private string _additionalText;
    public string additionalText {
        get { return _additionalText; }
        set
        {
            this.textMesh.text = $"0{value}";
            this._additionalText = value;
        }
    }

    public int scoreStep;
    public TextMeshProUGUI textMesh;

    // Update is called once per frame
    void Update()
    {
        if (dynamicValue != currentValue)
        {
            dynamicValue += System.Math.Min(scoreStep, currentValue - dynamicValue);
            this.textMesh.text = string.Format("{0:#,0}", dynamicValue) + additionalText;
        }
    }

    private void OnDisable()
    {
        this.currentValue = 0;
        this.dynamicValue = 0;
        this.textMesh.text = "0";
    }
    public void SetCounterValue(int value)
    {
        this.currentValue = value;
    }
}
