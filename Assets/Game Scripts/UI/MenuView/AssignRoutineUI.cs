using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class AssignRoutineUI : MonoBehaviour, IObserver
{
    public RoutineAssignListController routineListController;
    public UserManager userManager;

    public GameObject descriptionCanvas;

    public TextMeshProUGUI nameMesh;
    public TextMeshProUGUI idMesh;
    public TextMeshProUGUI descriptionMesh;

    public GameObject selectRoutine;
    public GameObject removeRoutine;

    private string selectedRoutineId
    {
        get
        {
            return routineListController.routines[routineListController.selectedIndex];
        }
    }

    private FullRoutine selectedRoutine
    {
        get
        {
            return routineListController.fullRoutines[routineListController.selectedIndex];
        }
    }

    public void OnNotify()
    {
        if (routineListController.isSelected)
        {
            descriptionCanvas.SetActive(true);

            FullRoutine routine = selectedRoutine;
            nameMesh.text = routine.name;
            descriptionMesh.text = routine.description;
            idMesh.text = routine.routineId;

            CheckButtonsInteract();
        } else
        {
            descriptionCanvas.SetActive(false);
            selectRoutine.SetActive(false);
            removeRoutine.SetActive(false);
        }
    }

    private void CheckButtonsInteract()
    {
        bool hasRoutine = userManager.activeUser.selectedRoutines.Contains(selectedRoutineId);

        selectRoutine.SetActive(!hasRoutine);
        removeRoutine.SetActive(hasRoutine);
    }

    public void HandleSelect()
    {
        userManager.AddRoutineToActiveUser(selectedRoutineId);
        CheckButtonsInteract();
    }

    public void HandleDeselect()
    {
        userManager.activeUser.selectedRoutines.Remove(selectedRoutineId);
        CheckButtonsInteract();
    }

    public void Start()
    {
        routineListController.Register(this);
    }

    public void OnDestroy()
    {
        routineListController.Unregister(this);
    }
}
