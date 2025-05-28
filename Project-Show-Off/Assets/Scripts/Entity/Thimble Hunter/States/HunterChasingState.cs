using UnityEngine;

public class HunterChasingState : State
{
    private HunterAI _hunterAI;
    private HunterStateMachine _hunterSM;

    public HunterChasingState(StateMachine stateMachine) : base(stateMachine)
    {
        _hunterSM = stateMachine as HunterStateMachine;
        _hunterAI = _hunterSM.HunterAI;
    }

    public override void OnEnterState()
    {
        if (_hunterAI == null) return;
        Debug.Log($"{_hunterAI.gameObject.name} entering CHASING state.");

        _hunterAI.NavAgent.speed = _hunterAI.MovementSpeedChasing;
        _hunterAI.NavAgent.isStopped = false;
        _hunterAI.HunterAnimator.SetBool("IsMoving", true);

        HunterEventBus.HunterSpottedPlayer(_hunterAI.PlayerTransform.gameObject);
        _hunterAI.PlaySound(_hunterAI.SpottedPlayerSound); // Play spotted sound
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

        if (_hunterAI.NavAgent.isOnNavMesh)
        {
            _hunterAI.NavAgent.SetDestination(_hunterAI.PlayerTransform.position);
        }
        _hunterAI.LastKnownPlayerPosition = _hunterAI.PlayerTransform.position;


        if (Vector3.Distance(_hunterAI.transform.position, _hunterAI.PlayerTransform.position) <= _hunterAI.MeleeRange)
        {
            // Optional: Check if player is facing hunter or other conditions for melee
            Debug.Log($"{_hunterAI.gameObject.name} Player in melee range. Transitioning to CloseKill.");
            SM.TransitToState(_hunterSM.CloseKillingState);
            return;
        }

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

    }
}