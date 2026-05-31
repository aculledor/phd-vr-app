using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CallibrateStandingHeight : CallibrationState
{
    public CallibrateStandingHeight(CallibrationSystem system) : base(system)
    {
    }
    
    public override void HandleCallibration()
    {
        Vector3 playerPosition = this.system.playerTracker.PlayerPosition;

        this.system.standingHeight = playerPosition.y;
        this.system.HandleStateChange();
    }

    public override IEnumerator HandleStateChange()
    {
        this.system.callibrationDisplay.ShowSuccessPanel("Altura calibrada");
        yield return new WaitForSeconds(this.system.successShowcaseTime);
        this.system.changeState(new CallibrateHittingPosition(this.system));

    }

    public override void Init()
    {
        this.system.callibrationDisplay.ShowMessagePanel("Ponte de pie y mantén pulsado el gatillo" +
            " para calcular tu altura.");
    }
}
