using UnityEngine;

public class HemannekenKillState : State // Assumed this state is for killing the player
{
    private HemannekenStateMachine HSM => (HemannekenStateMachine)SM;

    public HemannekenKillState(StateMachine pSM) : base(pSM) { }

    public override void OnEnterState()
    {
        Debug.Log("Entered Kill Player State");
        HSM.Movement.StopAgentCompletely(); // Stop AI
        HSM.PlayPlayerDefeatAnimation(); // Play player defeat
        HSM.TriggerGameOver(); // Trigger game over sequence
        // This state might be terminal or transition to an "IdleAfterKill" or similar.
    }

    public override void Handle()
    {
        // Potentially wait for an animation or event before allowing game restart, etc.
    }

    public override void OnExitState()
    {
        Debug.Log("Exited Kill Player State");
    }
}