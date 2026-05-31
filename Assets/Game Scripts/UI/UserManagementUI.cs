using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UserManagementUI : MonoBehaviour, IObserver
{
    public List<Button> interactableButtons;

    public UserManager userManager;
    public UserListController userListController;

    private void Start()
    {
        userListController.Register(this);
    }


    public void OnNotify()
    {
        foreach (Button button in interactableButtons)
        {
            button.interactable = userListController.isSelected;
        }

        if (userListController.isSelected)
        {
            userManager.SetActiveUser(userListController.selectedIndex);
        }
    }
}
