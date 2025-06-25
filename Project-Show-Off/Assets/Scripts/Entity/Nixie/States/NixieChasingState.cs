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

        NixieEventBus.NotifyChaseStart();
    }

    public override void Handle()
    {
        // This logic remains the same
        if (!nixieAI.IsPlayerInMyZone)
        {
            if (nixieAI.DistanceToPlayer <= nixieAI.StaringRadius)
            {
                SM.TransitToState(nixieSM.StaringState);
            }
            else
            {
                SM.TransitToState(nixieSM.RoamingState);
            }
            return;
        }

        if (!nixieAI.PlayerStatus.IsLanternOn && nixieAI.DistanceToPlayer > nixieAI.PointBlankRadius)
        {
            nixieAI.PlayerLastKnownPosition = nixieAI.PlayerTransform.position;
            SM.TransitToState(nixieSM.LurkingState);
            return;
        }

        if (nixieAI.DistanceToPlayer <= nixieAI.AttackRange)
        {
            SM.TransitToState(nixieSM.HurtingState);
            return;
        }

        nixieNav.MoveTo(nixieAI.PlayerTransform.position, nixieNav.ChasingSpeed);
        nixieNav.LookAt(nixieAI.PlayerTransform.position);
    }

    public override void OnExitState()
    {
        nixieNav.StopMoving();

        NixieEventBus.NotifyChaseEnd();
    }
}