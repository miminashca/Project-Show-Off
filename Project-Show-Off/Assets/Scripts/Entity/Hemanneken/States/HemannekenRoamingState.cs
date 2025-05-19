
using UnityEngine;

public class HemannekenRoamingState : State
{
    private int currentPatrolIndex = 0;
    public HemannekenRoamingState(StateMachine pSM) : base(pSM)
    {
    }
    // helper
    private HemannekenStateMachine HSM => (HemannekenStateMachine)SM;

    public override void OnEnterState()
    {
        Debug.Log("Entered Roaming State");
        HemannekenEventBus.HeyTriggered += TriggerHey;

        SetNextPatrolPoint();
    }

    public override void Handle()
    {
        if (!HSM.navAgent.pathPending && HSM.navAgent.remainingDistance < 1f)
        {
            SetNextPatrolPoint();
        }
        
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
    private void SetNextPatrolPoint()
    {
        if (HSM.spManager.SpawnPoints == null || HSM.spManager.SpawnPoints.Count == 0) return;

        int oldIndex = currentPatrolIndex;
        int newIndex = oldIndex;

        while (newIndex == oldIndex)
        {
            newIndex = Random.Range(0, HSM.spManager.SpawnPoints.Count);
        }

        currentPatrolIndex = newIndex;
        HSM.navAgent.SetDestination(HSM.spManager.SpawnPoints[newIndex].gameObject.transform.position);
    }
}
