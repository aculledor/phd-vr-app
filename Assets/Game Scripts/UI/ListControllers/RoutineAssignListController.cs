using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoutineAssignListController : InteractableListController
{
    [System.NonSerialized]
    public List<FullRoutine> fullRoutines;
    [System.NonSerialized]
    public string[] routines;

    public override void RenderData()
    {
        foreach(FullRoutine routine in fullRoutines){
            UserListItemScript item = Instantiate(itemPrefab, listView).GetComponent<UserListItemScript>();
            item.InitializeButton(this, routine.name);
            listElements.Add(item);
        }
    }

    private void OnDisable()
    {
        selectedIndex = -1;
        HandleSelection();
        NotifyObservers();
    }

    // Start is called before the first frame update
    void Start()
    {
        fullRoutines = new List<FullRoutine>();
        StorageSystem.GetStoredRoutines(out routines);
        foreach(string routine in routines)
        {
            FullRoutine routineInstance;
            if(StorageSystem.LoadRoutine(routine, out routineInstance))
            {
                fullRoutines.Add(routineInstance);
            }
        }

        RenderData();
    }

}
