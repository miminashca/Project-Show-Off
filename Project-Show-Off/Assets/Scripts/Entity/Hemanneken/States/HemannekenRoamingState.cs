// File: HemannekenRoamingState.cs
using UnityEngine;

public class HemannekenRoamingState : State
{
    private HemannekenStateMachine HSM => (HemannekenStateMachine)SM;

    public HemannekenRoamingState(StateMachine pSM) : base(pSM) { }

    public override void OnEnterState()
    {
        Debug.Log("Entered Roaming State");
        
        if (HSM.Visuals.IsTrueForm)
        {
            HSM.Movement.RoamWaypoints(MovementStyle.SplineWave, false, false);
        }
        else // Rabbit form
        {
            HSM.Movement.RoamWaypoints(MovementStyle.Hop, true, true);
        }

        // Subscribe to events that can trigger a state change.
        HSM.Sensor.OnPlayerDetected += HandlePlayerDirectlyDetected;
        PlayerActionEventBus.OnPlayerShouted += HandleHeyTriggered;
    }

    public override void Handle()
    {
        if (HSM.Visuals.IsTrueForm)
        {
            if (HSM.Sensor.IsPlayerInTrueChaseDistance())
            {
                SM.TransitToState(new HemannekenChasingState(SM));
                return; // Transitioning, so stop further checks
            }
        }
        else // Rabbit form
        {
            if (HSM.Sensor.IsPlayerInRabbitChaseDistance())
            {
                SM.TransitToState(new HemannekenEnchantixState(SM));
                return; // Transitioning
            }
        }
    }
    
    // Called when global "Hey" event fires
    private void HandleHeyTriggered(Vector3 pos)
    {
        if (HSM.Sensor.IsPlayerInInvestigateDistance())
        {
            HSM.Sensor.PlayerLastKnownPosition = pos;
            if (HSM.Visuals.IsTrueForm) 
            {
                SM.TransitToState(new HemannekenInvestigatingState(SM));
            }
        }
    }

    // Called by PlayerSensor if it directly "sees/hears" player
    private void HandlePlayerDirectlyDetected()
    {
    }

    public override void OnExitState()
    {
        Debug.Log("Exited Roaming State");
        
        HSM.Movement.StopAgentCompletely(true);

        HSM.Sensor.OnPlayerDetected -= HandlePlayerDirectlyDetected;
        PlayerActionEventBus.OnPlayerShouted -= HandleHeyTriggered;
    }
}