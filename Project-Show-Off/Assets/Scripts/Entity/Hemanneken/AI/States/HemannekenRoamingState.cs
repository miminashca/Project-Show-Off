
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
        HemannekenEventBus.HeyTriggered += TriggerHey;
    }

    public override void Handle()
    {
        if (HSM.IsTrueForm)
        { 
            if (HSM.PlayerIsInTrueChaseDistance()) SM.TransitToState(new HemannekenChasingState(SM));
        }
        if (!HSM.IsTrueForm) // rabbit form
        { 
            if (HSM.PlayerIsInRabbitChaseDistance()) SM.TransitToState(new HemannekenEnchantixState(SM));
        }
    }

    private void TriggerHey()
    {
        if(HSM.IsTrueForm && HSM.PlayerIsInInvestigateDistance()) SM.TransitToState(new HemannekenInvestigatingState(SM));
    }

    public override void OnExitState()
    {
        HemannekenEventBus.HeyTriggered -= TriggerHey;
    }
}
