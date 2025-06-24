using UnityEngine;

public class HemannekenAttachedState : State
{
    private HemannekenStateMachine HSM => (HemannekenStateMachine)SM;

    public HemannekenAttachedState(StateMachine pSM) : base(pSM) { }

    private WaterSensor playerWaterSensor;

    public override void OnEnterState()
    {
        Debug.Log("Entered Attached State");

        // 1. Tell the movement system to clear any active paths and reset its state.
        HSM.Movement.StopAgentCompletely(true); 

        // 2. Disable the AgentMovement component.
        HSM.Movement.enabled = false;

        if (HSM.Sensor.PlayerTransform != null)
        {
            playerWaterSensor = HSM.Sensor.PlayerTransform.gameObject.GetComponent<WaterSensor>();
        }
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

        // Re-enable the AgentMovement component so that subsequent states can use it again.
        HSM.Movement.enabled = true;

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