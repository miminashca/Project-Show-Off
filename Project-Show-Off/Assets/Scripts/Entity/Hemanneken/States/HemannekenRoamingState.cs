
using UnityEngine;

public class HemannekenRoamingState : State
{
    public HemannekenRoamingState(StateMachine pSM) : base(pSM)
    {
    }
    // helper
    private HemannekenStateMachine HSM => (HemannekenStateMachine)SM;

    public override void OnEnterState()
    {
        Debug.Log("Entered Roaming State");
        HemannekenEventBus.OnHeyTriggered += TriggerOnHey;
        HSM.LockNavMeshAgent(true);
    }

    public override void Handle()
    {
        if(HSM.nav) HSM.nav.Handle();
        
        if (HSM.IsTrueForm)
        { 
            if (HSM.PlayerIsInTrueChaseDistance()) SM.TransitToState(new HemannekenChasingState(SM));
        }
        if (!HSM.IsTrueForm) // rabbit form
        {
            if (HSM.PlayerIsInRabbitChaseDistance()) SM.TransitToState(new HemannekenEnchantixState(SM));
        }
    }

    private void TriggerOnHey()
    {
        if(HSM.IsTrueForm && HSM.PlayerIsInInvestigateDistance()) SM.TransitToState(new HemannekenInvestigatingState(SM));
    }

    public override void OnExitState()
    {
        HemannekenEventBus.OnHeyTriggered -= TriggerOnHey;
        HSM.LockNavMeshAgent(false);
    }
}
