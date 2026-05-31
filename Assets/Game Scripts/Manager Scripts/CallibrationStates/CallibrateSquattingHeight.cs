using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CallibrateSquattingHeight : CallibrationState
{
    public CallibrateSquattingHeight(CallibrationSystem system) : base(system)
    {
    }

    public override void HandleCallibration()
    {
        this.system.squattingHeight = this.system.playerTracker.PlayerPosition.y;
        this.system.HandleStateChange();
    }

    public override IEnumerator HandleStateChange()
    {
        this.system.callibrationDisplay.ShowSuccessPanel("Sentadilla calibrada");
        yield return new WaitForSeconds(this.system.successShowcaseTime);
        this.system.finishCallibration();
    }
    public override void Init()
    {
        this.system.callibrationDisplay.ShowMessagePanel("Sientate como si estuvieses realizando una sentadilla y mantén pulsado " +
            "el gatillo.");
    }
}
