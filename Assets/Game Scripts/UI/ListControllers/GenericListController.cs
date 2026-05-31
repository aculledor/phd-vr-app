using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GenericListController : Subject, IObserver
{

    public Transform listView;
    public GameObject itemPrefab;

    protected virtual void RefreshListContent()
    {
        foreach (Transform child in listView)
        {
            Destroy(child.gameObject);
        }
    }

    public void OnNotify() {
        RefreshListContent();
        RenderData();
        NotifyObservers();
    }

    public abstract void RenderData();
}
