using System.Collections;
using UnityEngine;

public interface IBusEventCallback
{
    public void HandleEvent(GameEvent gameEvent);
}