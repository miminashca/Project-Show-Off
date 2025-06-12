using UnityEngine;

public class NixieChasingState : State
{
    private NixieStateMachine nixieSM;
    private NixieAI nixieAI;
    private NixieNavigation nixieNav;

    public NixieChasingState(StateMachine pSM) : base(pSM)
    {
        nixieSM = (NixieStateMachine)SM;
        nixieAI = nixieSM.NixieAI;
        nixieNav = nixieSM.NixieNav;
    }

    public override void OnEnterState()
    {
        Debug.Log("Nixie entering CHASING state.");
        nixieNav.SetPeeking(true); // Head is slightly above water while chasing
    }

    public override void Handle()
    {
        // --- TRANSITION CHECKS ---
        if (nixieAI.DistanceToPlayer <= nixieAI.AttackRange)
        {
            SM.TransitToState(nixieSM.HurtingState);
            return;
        }

        if (!nixieAI.IsPlayerInWater)
        {
            if (nixieAI.DistanceToPlayer <= nixieAI.StaringRadius)
            {
                SM.TransitToState(nixieSM.StaringState);
            }
            else // Player is out of water AND out of staring range
            {
                SM.TransitToState(nixieSM.RoamingState);
            }
            return;
        }

        // --- BEHAVIOR LOGIC ---
        nixieNav.MoveTo(nixieAI.PlayerTransform.position, nixieNav.ChasingSpeed);
        nixieNav.LookAt(nixieAI.PlayerTransform.position);
    }

    public override void OnExitState()
    {
        nixieNav.StopMoving();
    }
}