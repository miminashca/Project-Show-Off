using UnityEngine;

public class HemannekenStunningState : State
{
    private HemannekenStateMachine HSM => (HemannekenStateMachine)SM;
    private float _stunEndTime;

    public HemannekenStunningState(StateMachine pSM) : base(pSM) { }

    public override void OnEnterState()
    {
        Debug.Log("Entered Stunning State");

        // Stop all looping sounds from previous states (like idle or the 'close hey' loop)
        HSM.SoundController.StopAllHemannekenSounds();

        // --- ADDED: Play the one-shot stunned sound ---
        // This is called immediately when the Hemanneken becomes stunned.
        HSM.SoundController.PlayStunnedSound();
        // --------------------------------------------------

        HSM.Movement.StopAgentCompletely(); // Stop movement
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