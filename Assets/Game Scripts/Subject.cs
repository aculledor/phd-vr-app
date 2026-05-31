using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Subject : MonoBehaviour
{

    private List<IObserver> observers;

    protected virtual void Awake()
    {
        observers = new List<IObserver>();
    }

    protected void NotifyObservers()
    {
        foreach(IObserver observer in observers)
        {
            observer.OnNotify();
        }
    }

    public void Register(IObserver observer)
    {
        observers.Add(observer);
    }

    public void Unregister(IObserver observer)
    {
        observers.Remove(observer);
    }

}
