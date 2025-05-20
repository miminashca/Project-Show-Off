using UnityEngine;

public class HemannekenChasingState : State
{
    public HemannekenChasingState(StateMachine pSM) : base(pSM)
    {
    }

    public override void OnEnterState()
    {
        Debug.Log("Entered Chasing State");
    }

    public override void Handle()
    {
        throw new System.NotImplementedException();
    }

    public override void OnExitState()
    {
        throw new System.NotImplementedException();
    }
}
