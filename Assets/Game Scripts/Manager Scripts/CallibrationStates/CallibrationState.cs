using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CallibrationState
{
    public string callibrationMessage;
    public string successMessage;

    protected CallibrationSystem system;

    public CallibrationState(CallibrationSystem system)
    {
        this.system = system;
    }
    public abstract void Init();
    public abstract void HandleCallibration();

    //Coroutine which will be in charge of specifying the wait times between succesful
    //syncs, etc.
    public abstract IEnumerator HandleStateChange();
}
