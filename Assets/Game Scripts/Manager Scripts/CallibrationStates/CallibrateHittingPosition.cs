using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity;

public class CallibrateHittingPosition : CallibrationState
{
    public CallibrateHittingPosition(CallibrationSystem system) : base(system)
    {
    }

    public override void HandleCallibration()
    {
        Vector3 controllerPosition = this.system.playerTracker.RControllerPosition;
        Vector3 headPosition = this.system.playerTracker.PlayerPosition;
        this.system.targetXOffset = controllerPosition.x - headPosition.x;
        this.system.targetYOffset = controllerPosition.y;
        this.system.punchZOffset = controllerPosition.z - headPosition.z;
        this.system.HandleStateChange();
    }

    public override void Init()
    {
        this.system.callibrationDisplay.ShowMessagePanel("Levanta el brazo derecho a la altura de tu hombro " +
            "y mantén pulsado el gatillo.");
    }

    public override IEnumerator HandleStateChange()
    {
        this.system.callibrationDisplay.ShowSuccessPanel("Brazos calibrados");
        yield return new WaitForSeconds(this.system.successShowcaseTime);
        this.system.changeState(new CallibrateSquattingHeight(this.system));
    }

}
