using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class HistoryUIManager : MonoBehaviour, IObserver
{

    public UserManager userManager;
    public ScoreListController scoreListController;

    public GameObject dataPanel;

    public TextMeshProUGUI dateMesh;
    public TextMeshProUGUI squatMesh;
    public TextMeshProUGUI movementMesh;
    public TextMeshProUGUI punchMesh;
    public TextMeshProUGUI comboMesh;
    public TextMeshProUGUI scoreMesh;



    private void Start()
    {
        scoreListController.Register(this);
    }

    private void OnDisable()
    {
        dataPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        scoreListController.Unregister(this);
    }

    public void OnNotify()
    {
        if (scoreListController.isSelected)
        {
            dataPanel.SetActive(true);
            List<ScoreRecords> dataList = userManager.activeUser.scoreHistory;
            ScoreRecords data = dataList[dataList.Count - 1 - scoreListController.selectedIndex];
            dateMesh.text = string.Format("{0:g}", data.time.ToLocalTime());
            squatMesh.text = $"{data.squatCount} de {data.squatTotal}";
            movementMesh.text = $"{data.movementCount} de {data.movementTotal}";
            punchMesh.text = $"{data.punchCount} de {data.punchTotal}";
            comboMesh.text = data.maxCombo.ToString();
            scoreMesh.text = string.Format("{0:#,0} pts.", data.highScore);
        }
    }
}
