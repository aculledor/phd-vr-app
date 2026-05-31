using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DifficultyListController : InteractableListController
{

    public List<DifficultyLevel> difficultyList;
    public override void RenderData()
    {
        foreach (DifficultyLevel level in difficultyList)
        {
            UserListItemScript item = Instantiate(itemPrefab, listView).GetComponent<UserListItemScript>();
            item.InitializeButton(this, ResolveEnum(level));
            listElements.Add(item);
        }
    }

    private string ResolveEnum(DifficultyLevel level) {

        switch (level)
        {
            case DifficultyLevel.EASY: return "Suave";
            case DifficultyLevel.MODERATE: return "Moderada";
            case DifficultyLevel.HARD: 
            default: return "Intensa";
        }
    }

    // Start is called before the first frame update
    void OnEnable()
    {
        RefreshListContent();
        RenderData();
    }

    
}
