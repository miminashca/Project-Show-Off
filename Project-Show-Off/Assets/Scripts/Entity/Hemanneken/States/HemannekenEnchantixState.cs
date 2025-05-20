using UnityEngine;

public class HemannekenEnchantixState : State
{
    public HemannekenEnchantixState(StateMachine pSM) : base(pSM)
    {
    }
    private HemannekenStateMachine HSM => (HemannekenStateMachine)SM;

    public override void OnEnterState()
    {
        Debug.Log("Entered Enchantix State");
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
