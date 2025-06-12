using UnityEngine;

public class NixieStuntedState : State
{
    // References fetched from the State Machine
    private NixieStateMachine nixieSM;
    private NixieAI nixieAI;
    private NixieNavigation nixieNav;

    private float stunTimer;

    // The constructor now only takes the StateMachine and gets other components from it.
    public NixieStuntedState(StateMachine pSM) : base(pSM)
    {
        nixieSM = (NixieStateMachine)SM;
        nixieAI = nixieSM.NixieAI;
        nixieNav = nixieSM.NixieNav;
    }

    // Renamed from OnEnter to OnEnterState
    public override void OnEnterState()
    {
        Debug.Log("Nixie entering STUNTED state.");
        nixieNav.StopMoving();
        nixieNav.SetPeeking(false); // Hide underwater while stunned
        stunTimer = nixieAI.StunDuration;
    }

    public override void Handle()
    {
        stunTimer -= Time.deltaTime;
        if (stunTimer <= 0)
        {
            // After stun timer expires, re-evaluate the situation.
            // Transitions now use SM.TransitToState and access states via the nixieSM reference.
            if (nixieAI.IsPlayerInWater && nixieAI.DistanceToPlayer <= nixieAI.CurrentDetectionRadius)
            {
                SM.TransitToState(nixieSM.ChasingState);
            }
            else if (!nixieAI.IsPlayerInWater && nixieAI.DistanceToPlayer <= nixieAI.StaringRadius)
            {
                SM.TransitToState(nixieSM.StaringState);
            }
            else
            {
                SM.TransitToState(nixieSM.RoamingState);
            }
        }
    }

    // Added OnExitState to match the abstract base class contract.
    public override void OnExitState()
    {
        // No specific exit logic needed for this state.
    }
}