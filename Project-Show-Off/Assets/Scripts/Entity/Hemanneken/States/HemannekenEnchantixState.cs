using UnityEngine;

public class HemannekenEnchantixState : State
{
    private HemannekenStateMachine HSM => (HemannekenStateMachine)SM;
    private float _transformationEndTime;

    public HemannekenEnchantixState(StateMachine pSM) : base(pSM) { }

    public override void OnEnterState()
    {
        Debug.Log("Entered Enchantix State (Transforming to True Form)");
        //HSM.Movement.EnableAgent(false); // Halt movement during transformation
        HSM.Visuals.PlayTransformationEffects();
        HSM.Visuals.SetForm(true, HSM.transform); // Set to true form

        _transformationEndTime = Time.time + HSM.aiConfig.transformationDuration;
    }

    public override void Handle()
    {
        if (Time.time >= _transformationEndTime)
        {
            SM.TransitToState(new HemannekenChasingState(SM));
        }
    }

    public override void OnExitState()
    {
        Debug.Log("Exited Enchantix State");
        HSM.Visuals.StopTransformationEffects();
        // Next state (Chasing) will manage NavMeshAgent in its OnEnter.
    }
}