
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
        EnableMesh(HSM.IsTrueForm);
    }

    public override void Handle()
    {
        if (HSM.IsTrueForm)
        { 
            if (HSM.PlayerIsInTrueChaseDistance()) SM.TransitToState(new HemannekenChasingState(SM));
            //if(HSM.PlayerIsInInvestigateDistance() && HEY EVENT TRIGGERED in Hemanneken Event Bus) SM.TransitToState(new HemannekenInvestigateState(SM));
        }
        if (!HSM.IsTrueForm) // rabbit form
        { 
            if (HSM.PlayerIsInRabbitChaseDistance()) SM.TransitToState(new HemannekenEnchantixState(SM));
        }
    }

    public override void OnExitState()
    {
    }

    private void EnableMesh(bool trueFormMesh)
    {
        if(trueFormMesh){}
        else
        {
            
        }
    }
}
