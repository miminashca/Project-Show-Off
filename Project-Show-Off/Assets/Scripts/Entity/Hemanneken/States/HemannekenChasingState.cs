using UnityEngine;

public class HemannekenChasingState : State
{
    public HemannekenChasingState(StateMachine pSM) : base(pSM)
    {
    }

    private HemannekenStateMachine HSM => (HemannekenStateMachine)SM;

    public override void OnEnterState()
    {
        Debug.Log("Entered Chasing State");
        HSM.LockNavMeshAgent(false);
    }

    public override void Handle()
    {
        HSM.interactor.countLanternTime = true;

        HSM.nav.SetDestination(HSM.GetPlayerPosition());
        HSM.nav.Roam();
        
        if (HSM.PlayerIsInAttachingDistance())
        {
            SM.TransitToState(new HemannekenAttachedState(SM));
            return;
        }
        // Transition to Stunned
        // "Stunned when Lantern is held up for 2 seconds and player is within 7 meters while holding Lantern up"
        if (CanBeStunned()) // HSM.CanBeStunned() encapsulates lantern conditions
        {
            SM.TransitToState(new HemannekenStunningState(SM));
            return;
        }
        
        if(HSM.PlayerIsInEndChaseDistance()) SM.TransitToState(new HemannekenRoamingState(SM));
    }

    public override void OnExitState()
    {
        HSM.LockNavMeshAgent(true);
        HSM.interactor.countLanternTime = false;

    }
    
    public bool CanBeStunned()
    {
        return HSM.interactor.lanternTimeCounter >= HSM.stunTimerDuration;
    }
}
