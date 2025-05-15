using UnityEngine;

public class HemannekenInvestigatingState : State
{
    public HemannekenInvestigatingState(StateMachine pSM) : base(pSM)
    {
    }

    public override void OnEnterState()
    {
        Debug.Log("Entered Investigating State");
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
