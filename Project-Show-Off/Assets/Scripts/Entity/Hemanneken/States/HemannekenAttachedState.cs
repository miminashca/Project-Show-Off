using UnityEngine;

public class HemannekenAttachedState : State
{
    private HemannekenStateMachine HSM => (HemannekenStateMachine)SM;

    public HemannekenAttachedState(StateMachine pSM) : base(pSM) { }

    public override void OnEnterState()
    {
        Debug.Log("Entered Attached State");
        
        HSM.PerformAttachmentToPlayer(); // Handles parenting, positioning, visual hiding, agent disabling
        
        HemannekenEventBus.AttachHemanneken(); // Global game event
        HemannekenEventBus.OnWaterTouch += HandleWaterTouch;
    }

    public override void Handle()
    {
        // Position is now managed by being parented to the player via PerformAttachmentToPlayer
        // If specific relative movement or updates are needed while attached, do them here.
        // For example, ensuring rotation matches player or a fixed offset.
        // For now, HSM.PerformAttachmentToPlayer already set a localPosition.
    }

    public override void OnExitState()
    {
        Debug.Log("Exited Attached State");
        HSM.PerformDetachmentFromPlayer(); // Handles unparenting, visual restoring
        
        HemannekenEventBus.DetachHemanneken(); // Global game event
        HemannekenEventBus.OnWaterTouch -= HandleWaterTouch;
        // The next state will handle enabling the agent if necessary.
    }

    private void HandleWaterTouch()
    {
        Debug.Log("Attached Hemanneken detected water touch.");
        HSM.TransitToState(new HemannekenDeathState(HSM));
    }
}