using UnityEngine;

public class HemannekenStunningState : State
{
    private HemannekenStateMachine HSM => (HemannekenStateMachine)SM;
    private float _stunEndTime;

    public HemannekenStunningState(StateMachine pSM) : base(pSM)
    {
    }

    public override void OnEnterState()
    {
        Debug.Log("Entered Stunning State");
        HSM.LockNavMeshAgent(true); // Temporarily stuck in place

        // Stun timer
        _stunEndTime = Time.time + HSM.stunTimerDuration; 

        // Play sfx and vfx
        HSM.PlayStunEffects(); // Assuming HSM.PlayStunEffects() exists
    }

    public override void Handle()
    {
        // Transition to Roaming (true form) after stun timer runs out
        if (Time.time >= _stunEndTime)
        {
            SM.TransitToState(new HemannekenRoamingState(SM));
        }
        // "is unable to change to another state during this timer" - handled by only having one exit condition.
    }

    public override void OnExitState()
    {
        Debug.Log("Exited Stunning State");
        HSM.StopStunEffects(); // Assuming HSM.StopStunEffects() exists to clean up VFX/SFX
        // Next state (Roaming) will manage NavMeshAgent in its OnEnter.
        // RoamingState.OnEnter calls HSM.LockNavMeshAgent(true), so no need to explicitly unlock here.
    }
}