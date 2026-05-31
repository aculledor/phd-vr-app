using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class UserListController : InteractableListController
{
    public UserManager userManager;

    private BaseClickableListItem CreateItem(UserData data)
    {
        UserListItemScript item = Instantiate(itemPrefab, listView).GetComponent<UserListItemScript>();
        item.InitializeButton(this, data.userName);
        return item;
    }

    protected void Start()
    {
        RenderData();
    }

    protected override void Awake()
    {
        base.Awake();
        userManager.Register(this);
    }

    private void OnDestroy()
    {
        userManager.Unregister(this);
    }

    public override void RenderData()
    {
        listElements.Clear();

        foreach (UserData data in this.userManager.users)
        {

            listElements.Add(CreateItem(data));
        }
    }
}
