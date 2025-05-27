// HemannekenChasingState.cs
using UnityEngine;

public class HemannekenChasingState : State
{
    private HemannekenStateMachine HSM => (HemannekenStateMachine)SM;
    private const float CHASE_UPDATE_INTERVAL = 0.25f;
    private const float PLAYER_MOVE_THRESHOLD_SQR = 0.25f * 0.25f; // Only repath if player moves more than 0.25 units (squared for efficiency)
    private float _nextChaseUpdateTime;
    private Vector3 _lastChasedPlayerPosition = Vector3.positiveInfinity; // Initialize to ensure first chase updates

    public HemannekenChasingState(StateMachine pSM) : base(pSM) { }

    public override void OnEnterState()
    {
        _nextChaseUpdateTime = Time.time;
        _lastChasedPlayerPosition = Vector3.positiveInfinity; // Reset for new chase
        HemannekenEventBus.StartChase();
        // Debug.Log("Entered Chasing State");
    }

    public override void Handle()
    {
        if (HSM.Interactor != null) HSM.Interactor.countLanternTime = true;

        if (Time.time >= _nextChaseUpdateTime)
        {
            Vector3 currentPlayerPosition = HSM.Sensor.GetPlayerCurrentPosition();
            
            // Check if player has moved enough to warrant a new path
            if ((currentPlayerPosition - _lastChasedPlayerPosition).sqrMagnitude > PLAYER_MOVE_THRESHOLD_SQR || 
                _lastChasedPlayerPosition == Vector3.positiveInfinity) // Always path on first update or if no last pos
            {
                // Debug.Log(LOG_PREFIX + "Player moved or first chase update. Re-pathing chase.");
                HSM.Movement.SetDestination(currentPlayerPosition, MovementStyle.SplineWave);
                _lastChasedPlayerPosition = currentPlayerPosition;
            }
            // else { Debug.Log(LOG_PREFIX + "Player stationary, not re-pathing chase."); }

            _nextChaseUpdateTime = Time.time + CHASE_UPDATE_INTERVAL;
        }
        
        // ... (rest of the transitions: Attach, Stun, EndChase) ...
        if (HSM.Sensor.IsPlayerInAttachDistance())
        {
            SM.TransitToState(new HemannekenAttachedState(SM));
            return;
        }
        
        if (HSM.Sensor.IsPlayerInStunDistance() && CanBeStunnedByLantern())
        {
            SM.TransitToState(new HemannekenStunningState(SM));
            return;
        }
        
        if(HSM.Sensor.IsPlayerInEndChaseDistance()) 
        {
            SM.TransitToState(new HemannekenRoamingState(SM));
            return;
        }
    }

    public override void OnExitState()
    {
        if (HSM.Interactor != null) HSM.Interactor.countLanternTime = false;
        HSM.Movement.StopAgentCompletely(); 
        HemannekenEventBus.EndChase();
        // Debug.Log("Exited Chasing State");
    }
    
    private bool CanBeStunnedByLantern()
    {
        return HSM.Interactor != null && HSM.Interactor.lanternTimeCounter >= HSM.aiConfig.lanternStunHoldDuration;
    }
}