using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class RankingItemScript : MonoBehaviour
{
    public TextMeshProUGUI nameMesh;
    public TextMeshProUGUI positionMesh;
    public TextMeshProUGUI scoreMesh;

    public void SetTextData(string name, int rankingPosition, int score)
    {
        this.nameMesh.text = name;
        this.positionMesh.text = $"{rankingPosition}.";
        this.scoreMesh.text = string.Format("{0:#,0} pts.", score);
    }
}
