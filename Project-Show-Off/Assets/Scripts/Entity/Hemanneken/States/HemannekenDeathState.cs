using UnityEngine;

public class HemannekenDeathState : State
{
    private HemannekenStateMachine HSM => (HemannekenStateMachine)SM;

    public HemannekenDeathState(StateMachine pSM) : base(pSM) { }

    public override void OnEnterState()
    {
        Debug.Log("Entered Death State");
        HSM.Movement.StopAgentCompletely(); // Stop all movement
        //HSM.Movement.EnableAgent(false); // Disable agent

        HSM.Visuals.PlayDeathEffects();
        HSM.DestroySelfAfterDelay(HSM.aiConfig.deathEffectDuration);
    }

    public override void Handle() { /* No actions needed, waiting for destruction */ }
    public override void OnExitState() { /* Usually not called as GameObject is destroyed */ }
}