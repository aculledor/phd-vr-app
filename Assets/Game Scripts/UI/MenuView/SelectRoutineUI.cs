using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectRoutineUI : MonoBehaviour, IObserver
{
    public UserRoutinesListController listController;
    public RoutineManager routineManager;
    public UserManager userManager;

    public GameObject descriptionCanvas;

    public TextMeshProUGUI nameMesh;
    public TextMeshProUGUI idMesh;
    public TextMeshProUGUI descriptionMesh;


    public Button startButton;
    
    public void OnNotify()
    {

        startButton.interactable = listController.isSelected;

        if (listController.isSelected)
        {
            descriptionCanvas.SetActive(true);

            FullRoutine routine = listController.fullRoutines[listController.selectedIndex];
            nameMesh.text = routine.name;
            descriptionMesh.text = routine.description;
            idMesh.text = routine.routineId;
            routineManager.SetActiveRoutineProgramme(routine);
        }
        else
        {
            descriptionCanvas.SetActive(false);
        }
    }

    public void Start()
    {
        listController.Register(this);
    }

    public void OnDestroy()
    {
        listController.Unregister(this);
    }
}
