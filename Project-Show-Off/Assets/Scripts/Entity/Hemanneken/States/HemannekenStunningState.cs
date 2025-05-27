using UnityEngine;

public class HemannekenStunningState : State
{
    private HemannekenStateMachine HSM => (HemannekenStateMachine)SM;
    private float _stunEndTime;

    public HemannekenStunningState(StateMachine pSM) : base(pSM) { }

    public override void OnEnterState()
    {
        Debug.Log("Entered Stunning State");
        HSM.Movement.StopAgentCompletely(); // Stop movement
        //HSM.Movement.EnableAgent(false); // Disable agent for stun duration

        _stunEndTime = Time.time + HSM.aiConfig.stunEffectDuration;
        HSM.Visuals.StartStunEffectsAndBehavior();
    }

    public override void Handle()
    {
        if (Time.time >= _stunEndTime)
        {
            SM.TransitToState(new HemannekenRoamingState(SM)); // Transition to Roaming (true form)
        }
    }

    public override void OnExitState()
    {
        Debug.Log("Exited Stunning State");
        HSM.Visuals.StopStunBehavior();
        // Next state (Roaming) will re-enable agent in its OnEnter.
    }
}