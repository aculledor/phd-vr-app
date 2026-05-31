using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserListItemScript : BaseClickableListItem
{


    public TextMeshProUGUI userName;


    public void InitializeButton(IClickableListElementCallback listener, string userString)
    {
        this.button.onClick.AddListener(delegate { listener.ClickCallback(this); });
        userName.text = userString;
    }

    protected override void HandleTextSelection()
    {
        userName.color = selectedText;
    }

    protected override void HandleTextDeselection()
    {
        userName.color = unselectedText;
    }
}
