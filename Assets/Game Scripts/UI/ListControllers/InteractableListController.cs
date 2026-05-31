using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class InteractableListController : GenericListController, IClickableListElementCallback
{
    [System.NonSerialized]
    public int selectedIndex;
    protected List<BaseClickableListItem> listElements;

    protected override void Awake()
    {
        base.Awake();
        selectedIndex = -1;
        listElements = new List<BaseClickableListItem>();
    }

    protected override void RefreshListContent()
    {
        base.RefreshListContent();
        listElements.Clear();
        selectedIndex = -1;
    }

    public bool isSelected
    {
        get {
            return selectedIndex > -1;
        }
    }




    public void ClickCallback(BaseClickableListItem item)
    {
        selectedIndex = listElements.IndexOf(item);

        HandleSelection();

        NotifyObservers();
    }

    protected void HandleSelection()
    {
        for (int i = 0; i < listElements.Count; i++)
        {
            if (i == selectedIndex)
            {
                listElements[i].SelectButton();
            }
            else
            {
                listElements[i].DeselectButton();
            }
        }
    }

}
