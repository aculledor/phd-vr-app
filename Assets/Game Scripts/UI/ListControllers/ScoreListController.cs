using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreListController : InteractableListController
{

    public UserManager userManager;
    
    private void OnEnable()
    {
        RefreshListContent();
        RenderData();
    }

    public override void RenderData()
    {
        List<ScoreRecords> scoreList = this.userManager.activeUser.scoreHistory;

        for (int i = scoreList.Count - 1; i >= 0; i--)
        {
            ScoreListItemScript item = Instantiate(itemPrefab, listView).GetComponent<ScoreListItemScript>();
            item.InitializeButton(this, string.Format("{0:g}", scoreList[i].time.ToLocalTime()),
                string.Format("{0:#,0} pts.", scoreList[i].highScore));
            listElements.Add(item);
        }
    }
}
