using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserRoutinesListController : InteractableListController
{
    public UserManager userManager;


    [System.NonSerialized]
    public List<FullRoutine> fullRoutines;

    public override void RenderData()
    {
        fullRoutines = new List<FullRoutine>();

        FullRoutine loadedRoutine;
        List<string> selectedRoutines = userManager.activeUser.selectedRoutines;
        for (int i = selectedRoutines.Count - 1; i > -1; i--)
        {
            if (StorageSystem.LoadRoutine(selectedRoutines[i], out loadedRoutine))
            {
                fullRoutines.Add(loadedRoutine);
            }
            else
            {
                selectedRoutines.RemoveAt(i);
            }

        }

        foreach (FullRoutine routine in fullRoutines) { 
            UserListItemScript item = Instantiate(itemPrefab, listView).GetComponent<UserListItemScript>();
            item.InitializeButton(this, routine.name);
            listElements.Add(item);
        }
    }

    private void OnEnable()
    {
        RenderData();
    }

    private void OnDisable()
    {
        selectedIndex = -1;
        RefreshListContent();
    }

}
