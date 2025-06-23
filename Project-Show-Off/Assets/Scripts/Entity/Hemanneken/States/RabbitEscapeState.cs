using UnityEngine;

public class RabbitEscapeState : State
{
    private HemannekenStateMachine HSM => (HemannekenStateMachine)SM;
    public RabbitEscapeState(StateMachine pSM) : base(pSM) { }
    
    private float rabbitEscapeDistance;
    private Transform playerTransform;

    public override void OnEnterState()
    {
        rabbitEscapeDistance = HSM.aiConfig.rabbitEscapeDistance;
        playerTransform = HSM.Sensor.PlayerTransform;
    }

    public override void Handle()
    {
        
    }

    public override void OnExitState()
    {
    }
}