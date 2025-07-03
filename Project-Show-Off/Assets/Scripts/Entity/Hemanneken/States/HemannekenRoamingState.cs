using System.Collections;
using UnityEngine;

public class HemannekenRoamingState : State
{
    private HemannekenStateMachine HSM => (HemannekenStateMachine)SM;

    public HemannekenRoamingState(StateMachine pSM) : base(pSM) { }

    public override void OnEnterState()
    {
        Debug.Log("Entered Roaming State");

        // --- Sound Setup ---
        // Start the ambient idle sound and the periodic 'Hey' loop for this state.
        if (HSM.SoundController != null)
        {
            //HSM.SoundController.StartIdleSound();
            HSM.SoundController.StartPeriodicHeyLoop();
        }

        // Subscribe to events that can trigger a state change.
        HSM.Sensor.OnPlayerDetected += HandlePlayerDirectlyDetected;
        PlayerActionEventBus.OnPlayerShouted += HandleHeyTriggered;
        
        if (HSM.Visuals.IsTrueForm)
        {
            HSM.Movement.RoamWaypoints(MovementStyle.SplineWave, false, false);
        }
        else // Rabbit form
        {
            HSM.Movement.RoamWaypoints(MovementStyle.Hop, true, true);
        }
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
                Debug.Log("Real rabbit? " + HSM.isRealRabbit);

                if (HSM.isRealRabbit)
                {
                    SM.TransitToState(new RabbitEscapeState(SM));
                    return;
                }
                else
                {
                    SM.TransitToState(new HemannekenEnchantixState(SM));
                }
                return;
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

        // --- Sound Cleanup ---
        // Stop the looping sounds that were started by this state to ensure
        // they don't leak into the next state (e.g., Chasing).
        if (HSM.SoundController != null)
        {
            //HSM.SoundController.StopIdleSound();
            HSM.SoundController.StopPeriodicHeyLoop();
        }
        // ---------------------

        HSM.Movement.StopAgentCompletely(true);

        HSM.Sensor.OnPlayerDetected -= HandlePlayerDirectlyDetected;
        PlayerActionEventBus.OnPlayerShouted -= HandleHeyTriggered;
    }
}