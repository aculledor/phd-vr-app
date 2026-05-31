using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RankingListController : GenericListController
{
    public UserManager userManager;

    protected void Start()
    {
        userManager.Register(this);
        RenderData();
    }

    private void OnDestroy()
    {
        userManager.Unregister(this);
    }

    public override void RenderData()
    {
        List<UserData> localList = new List<UserData>(userManager.users);
        localList.RemoveAll((user) => user.highestScore == 0);
        localList.Sort(delegate (UserData x, UserData y)
        {
            return -x.highestScore.CompareTo(y.highestScore);
        });

        for (int i = 0; i < localList.Count; i++)
        {
            RankingItemScript item = Instantiate(itemPrefab, listView).GetComponent<RankingItemScript>();
            item.SetTextData(localList[i].userName, i + 1, localList[i].highestScore);
        }
    }
}

