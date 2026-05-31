using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProfilesUIManager : MonoBehaviour, IObserver
{
    public DifficultyListController listController;
    public RoutineManager routineManager;
    public Button selectButton;

    private RoutinePresets routinePresets;

    private void Start()
    {
        listController.Register(this);
        routinePresets = (RoutinePresets)routineManager.selectedRoutine;
    }

    private void OnDestroy()
    {
        listController.Unregister(this);
    }

    public void OnNotify()
    {
        this.selectButton.interactable = listController.isSelected;

        if (listController.isSelected)
        {
            routinePresets.difficultyLevel = listController.
                difficultyList[listController.selectedIndex];
        }

    }
}
