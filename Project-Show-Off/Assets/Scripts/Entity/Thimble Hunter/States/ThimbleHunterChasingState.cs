using UnityEngine;

public class ThimbleHunterChasingState : State
{
    private ThimbleHunterAI _hunterAI;
    private ThimbleHunterStateMachine _hunterSM;

    public ThimbleHunterChasingState(StateMachine stateMachine) : base(stateMachine)
    {
        _hunterSM = stateMachine as ThimbleHunterStateMachine;
        if (_hunterSM == null)
        {
            Debug.LogError("ThimbleHunterChasingState received an incompatible StateMachine!", stateMachine);
            return;
        }
        _hunterAI = _hunterSM.HunterAI;
    }

    public override void OnEnterState()
    {
        if (_hunterAI == null) return;
        Debug.Log($"{_hunterAI.gameObject.name} entering CHASING state.");

        _hunterAI.NavAgent.speed = _hunterAI.MovementSpeedChasing;
        _hunterAI.NavAgent.isStopped = false;
        _hunterAI.HunterAnimator.SetBool("IsMoving", true); // Or specific "IsChasing" animation

        // Optional: Play a "spotted" sound/vocalization
        HunterEventBus.HunterSpottedPlayer(_hunterAI.PlayerTransform.gameObject);
    }

    public override void Handle()
    {
        if (_hunterAI == null || _hunterAI.PlayerTransform == null)
        {
            // If player is lost or destroyed, go back to investigating last known spot or roaming
            Debug.LogWarning($"{_hunterAI.gameObject.name} lost player reference in ChasingState. Transitioning to Investigate.");
            SM.TransitToState(_hunterSM.InvestigatingState); // Or Roaming if LKP is unreliable
            return;
        }

        // --- Chase Logic ---
        // Continuously update destination to player's current position
        if (_hunterAI.NavAgent.isOnNavMesh)
        {
            _hunterAI.NavAgent.SetDestination(_hunterAI.PlayerTransform.position);
        }
        // Update LKP while chasing
        _hunterAI.LastKnownPlayerPosition = _hunterAI.PlayerTransform.position;


        // --- Transition Checks (Priority Order) ---
        // 1. (Optional) To CLOSE_KILLING: Player within melee range
        // if (Vector3.Distance(_hunterAI.transform.position, _hunterAI.PlayerTransform.position) <= _hunterAI.MeleeRange)
        // {
        //     SM.TransitToState(_hunterSM.CloseKillingState); // Assuming CloseKillingState exists
        //     return;
        // }

        // 2. To AIMING: Player within ShootingRange AND LoS is clear (IsPlayerVisible implies LoS)
        if (_hunterAI.IsPlayerVisible && Vector3.Distance(_hunterAI.transform.position, _hunterAI.PlayerTransform.position) <= _hunterAI.ShootingRange)
        {
            SM.TransitToState(_hunterSM.AimingState);
            return;
        }

        // 3. To INVESTIGATING: Player breaks LoS (IsPlayerVisible becomes false)
        if (!_hunterAI.IsPlayerVisible)
        {
            Debug.Log($"{_hunterAI.gameObject.name} lost sight of player during chase.");
            // LKP was updated continuously, so it's the last seen position.
            SM.TransitToState(_hunterSM.InvestigatingState);
            return;
        }
    }

    public override void OnExitState()
    {
        if (_hunterAI == null) return;
        Debug.Log($"{_hunterAI.gameObject.name} exiting CHASING state.");
        // NavAgent might be stopped by AimingState, or speed might be changed by InvestigatingState.
    }
}