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
        HSM.aiNav.SetDestination(HSM.GetPlayerPosition());
        if(HSM.PlayerIsInAttachingDistance()) SM.TransitToState(new HemannekenAttachedState(SM));
    }

    public override void OnExitState()
    {
        HSM.LockNavMeshAgent(true);
    }
}
