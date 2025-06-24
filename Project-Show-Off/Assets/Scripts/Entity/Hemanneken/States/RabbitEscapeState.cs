using UnityEngine;

public class RabbitEscapeState : State
{
    private HemannekenStateMachine HSM => (HemannekenStateMachine)SM;
    public RabbitEscapeState(StateMachine pSM) : base(pSM) { }
    
    private float rabbitEscapeDistance;
    private Transform playerTransform;
    Vector3 currentEscapeDestination;
    
    MovementParameters escapeParams;

    public override void OnEnterState()
    {
        escapeParams = new MovementParameters(HSM.aiConfig);
        escapeParams.hopWaitDuration = 0.1f;  
        escapeParams.hopSpeed *= 1.5f;         
        escapeParams.hopDistance *= 1.5f;
        
        rabbitEscapeDistance = HSM.aiConfig.rabbitEscapeDistance;
        playerTransform = HSM.Sensor.PlayerTransform;
        
        FindEscapeDestination();
        SetDestination();
        
    }

    private void FindEscapeDestination()
    {
        Vector3 forwardVector = playerTransform.forward;
        Vector3 escapeVector = forwardVector * rabbitEscapeDistance;
        currentEscapeDestination = playerTransform.position + escapeVector;
    }

    private void SetDestination()
    {
        HSM.Movement.SetDestination(
            destination: currentEscapeDestination,
            style: MovementStyle.Hop,
            groundRestricted: true,
            pauseOnArrival: false,
            newParams: escapeParams
        );
    }

    public override void Handle()
    {
        if (HSM.Sensor.GetDistanceToPlayer() <= 0.2f)
        {
            FindEscapeDestination();
            SetDestination();
        }

        if (HSM.Sensor.GetDistanceToPlayer() >= 5f)
        {
            HSM.TransitToState(new HemannekenRoamingState(SM));
        }
    }

    public override void OnExitState()
    {
        HSM.Movement.StopAgentCompletely();
    }
}