using UnityEngine;

public class HemannekenEnchantixState : State
{
    private HemannekenStateMachine HSM => (HemannekenStateMachine)SM;
    private float _transformationEndTime;

    public HemannekenEnchantixState(StateMachine pSM) : base(pSM)
    {
    }

    public override void OnEnterState()
    {
        Debug.Log("Entered Enchantix State (Transforming to True Form)");
        HSM.LockNavMeshAgent(true); // Halt movement during transformation

        // "this state transition triggers a particle effect and sound effect"
        HSM.PlayTransformationEffects();

        // Set the Hemanneken to its true form
        HSM.SetForm(true);

        // Timer for transformation effect duration
        // HSM.transformationDuration should be defined in HemannekenStateMachine
        _transformationEndTime = Time.time + HSM.transformationDuration;
    }

    public override void Handle()
    {
        // After transformation duration, transition to Chasing state
        if (Time.time >= _transformationEndTime)
        {
            SM.TransitToState(new HemannekenChasingState(SM));
        }
    }

    public override void OnExitState()
    {
        Debug.Log("Exited Enchantix State");
        HSM.StopTransformationEffects(); // Clean up any looping transformation effects
        // Next state (Chasing) will manage NavMeshAgent in its OnEnter (HSM.LockNavMeshAgent(false)).
    }
}