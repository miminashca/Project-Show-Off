using UnityEngine;

public class HemannekenDeathState : State
{
    private HemannekenStateMachine HSM => (HemannekenStateMachine)SM;

    public HemannekenDeathState(StateMachine pSM) : base(pSM)
    {
    }

    public override void OnEnterState()
    {
        Debug.Log("Entered Death State");
        HSM.LockNavMeshAgent(true); // Stop all movement

        // Play death effects (SFX, VFX)
        HSM.PlayDeathEffects(); // Assuming HSM.PlayDeathEffects() exists

        // "The creature is dead and is removed from the scene."
        // Destroy the GameObject after a short delay for effects to play out.
        // HSM.deathEffectDuration should be defined in HemannekenStateMachine
        GameObject.Destroy(HSM.gameObject, HSM.deathEffectDuration);
    }

    public override void Handle()
    {
    }

    public override void OnExitState()
    {
    }
}