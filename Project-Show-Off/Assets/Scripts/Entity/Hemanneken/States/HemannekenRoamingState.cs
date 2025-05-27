using UnityEngine;

public class HemannekenRoamingState : State
{
    private HemannekenStateMachine HSM => (HemannekenStateMachine)SM;

    public HemannekenRoamingState(StateMachine pSM) : base(pSM) { }

    public override void OnEnterState()
    {
        Debug.Log("Entered Roaming State");
        HSM.Sensor.OnPlayerDetected += HandlePlayerDirectlyDetected; // Direct detection for investigation
    }

    public override void Handle()
    {
        HSM.Movement.RoamWaypoints(); // Uses NavMeshAgent to move between waypoints
        
        if (HSM.Visuals.IsTrueForm)
        { 
            if (HSM.Sensor.IsPlayerInTrueChaseDistance())
            {
                SM.TransitToState(new HemannekenChasingState(SM));
                return;
            }
        }
        else // Rabbit form
        {
            if (HSM.Sensor.IsPlayerInRabbitChaseDistance())
            {
                SM.TransitToState(new HemannekenEnchantixState(SM));
                return;
            }
        }
    }
    
    // Called when global "Hey" event fires (player used "Hey" action)
    private void HandleHeyTriggered()
    {
        // Check if player is within investigation distance when "Hey" is used
        if (HSM.Sensor.IsPlayerInInvestigateDistance())
        {
            // Only true form Hemannekens investigate "Hey" calls, rabbits transform if close.
            if (HSM.Visuals.IsTrueForm) 
            {
                SM.TransitToState(new HemannekenInvestigatingState(SM));
            }
        }
    }

    // Called by PlayerSensor if it directly "sees/hears" player without global "Hey"
    private void HandlePlayerDirectlyDetected()
    {
        // This is a more general detection, separate from the "Hey" call.
        // If in true form and player is within investigate distance (but not chase)
        // and a "Hey" call comes in (via HandleHeyTriggered), it will investigate.
        // If player just enters investigate distance without a "Hey", it might not react immediately
        // unless it's close enough for chase.
        // This specific handler could be used for more subtle detections if needed.
        // For now, the main trigger for investigation is the global "Hey" event.
    }


    public override void OnExitState()
    {
        Debug.Log("Exited Roaming State");
        HSM.Sensor.OnPlayerDetected -= HandlePlayerDirectlyDetected;
    }
}