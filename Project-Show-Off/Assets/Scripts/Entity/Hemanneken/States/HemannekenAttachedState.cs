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
        
        HSM.PerformAttachmentToPlayer(); // Handles parenting, positioning, visual hiding, agent disabling
        
        HemannekenEventBus.AttachHemanneken(); // Global game event
        
        HSM.Visuals.PlayReplyHeySound();
        if (HSM.Interactor != null) HSM.Interactor.countLanternTime = true;
    }

    public override void Handle()
    {
        HSM.HandleAttachment();
        if(playerWaterSensor.GetTimeUnderwater()>=HSM.aiConfig.waterDeathThreshold) HSM.TransitToState(new HemannekenDeathState(SM));
        if (CanBeStunnedByLantern())
        {
            Debug.Log("STUN");
            HSM.TransitToState(new HemannekenDeathState(SM));
        }
    }

    public override void OnExitState()
    {
        Debug.Log("Exited Attached State");
        HSM.PerformDetachmentFromPlayer(); // Handles unparenting, visual restoring
        HemannekenEventBus.DetachHemanneken(); // Global game event
        if (HSM.Interactor != null) HSM.Interactor.countLanternTime = false;
    }
    
    private bool CanBeStunnedByLantern()
    {
        return HSM.Interactor != null && HSM.Interactor.lanternTimeCounter >= HSM.aiConfig.lanternStunHoldDuration;
    }

}