using UnityEngine;

public class NixieHurtingState : State
{
    // References fetched from the State Machine
    private NixieStateMachine nixieSM;
    private NixieAI nixieAI;

    // Updated constructor
    public NixieHurtingState(StateMachine pSM) : base(pSM)
    {
        nixieSM = (NixieStateMachine)SM;
        nixieAI = nixieSM.NixieAI;
    }

    // Renamed from OnEnter to OnEnterState
    public override void OnEnterState()
    {
        Debug.Log("Nixie entering HURTING state.");
        nixieAI.PlayAttackSound();

        // --- Deal damage to the player ---
        // Example: PlayerHealth.Instance.TakeDamage(1);
        Debug.Log("Nixie attacks the player!");

        // This is an instantaneous state, so transition immediately.
        // Updated transition call.
        SM.TransitToState(nixieSM.StuntedState);
    }

    // This state has no ongoing logic, so Handle is empty.
    public override void Handle()
    {
    }

    // This state has no exit logic, as the transition happens instantly.
    public override void OnExitState()
    {
    }
}