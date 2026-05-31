using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ProfileListController : InteractableListController
{

    public List<RoutinePresets> availableRoutines;

    private void Start()
    {
        RenderData();
    }
    public override void RenderData()
    {
        foreach(RoutinePresets preset in availableRoutines)
        {
            ProfileListItemScript item = Instantiate(itemPrefab, listView).GetComponent<ProfileListItemScript>();
            item.InitializeButton(this, preset);
            listElements.Add(item);
        }
    }
}
