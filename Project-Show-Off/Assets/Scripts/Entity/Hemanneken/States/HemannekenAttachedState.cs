using UnityEngine;

public class HemannekenAttachedState : State
{
    private HemannekenStateMachine HSM => (HemannekenStateMachine)SM;

    public HemannekenAttachedState(StateMachine pSM) : base(pSM) { }

    private WaterSensor playerWaterSensor;

    public override void OnEnterState()
    {
        Debug.Log("Entered Attached State");
        playerWaterSensor = HSM.Sensor.PlayerTransform.gameObject.GetComponent<WaterSensor>();
        HSM.PerformAttachmentToPlayer();
        HemannekenEventBus.AttachHemanneken();
        HSM.Visuals.PlayReplyHeySound();
        //new vignett code
        HSM.Visuals.EnableVignette();
        //end
    }

    public override void Handle()
    {
        HSM.HandleAttachment();
        //if (playerWaterSensor.GetTimeUnderwater() >= HSM.aiConfig.waterDeathThreshold) HSM.TransitToState(new HemannekenDeathState(SM));
        if (CanBeStunnedByLantern())
        {
            Debug.Log("STUN");
            HSM.TransitToState(new HemannekenDeathState(SM));
        }
    }

    public override void OnExitState()
    {
        Debug.Log("Exited Attached State");
        HSM.PerformDetachmentFromPlayer();
        HemannekenEventBus.DetachHemanneken();
        //new vignett code
        HSM.Visuals.DisableVignette();
        //end
    }
    
    private bool CanBeStunnedByLantern()
    {
        return HSM.PlayerLanternController != null &&
               HSM.PlayerLanternController.TimeLanternRaised >= HSM.aiConfig.lanternStunHoldDuration;
    }

}