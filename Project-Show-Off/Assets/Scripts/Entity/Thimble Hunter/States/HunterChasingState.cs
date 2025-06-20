using UnityEngine;

public class HunterChasingState : State
{
    private HunterAI _hunterAI;
    private HunterStateMachine _hunterSM;

    private float timeSinceLostSight = 0f;
    private const float GRACE_PERIOD_BEFORE_INVESTIGATING = 0.5f; // *** KEY NEW VARIABLE ***

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

        // Don't play the spotted sound repeatedly if we are just coming back from aiming/suppressing
        if (SM.PreviousState is not HunterAimingState and not HunterSuppressingState)
        {
            HunterEventBus.HunterSpottedPlayer(_hunterAI.PlayerTransform.gameObject);
            _hunterAI.PlaySound(_hunterAI.SpottedPlayerSound);
        }

        timeSinceLostSight = 0f; // Reset the grace period timer
    }

    public override void Handle()
    {
        if (_hunterAI == null || _hunterAI.PlayerTransform == null)
        {
            Debug.LogWarning($"{_hunterAI.gameObject.name} lost player reference in ChasingState. Transitioning to Investigate.");
            SM.TransitToState(_hunterSM.InvestigatingState);
            return;
        }

        // --- Core Logic Reordering ---

        // ALWAYS update the destination to the player's last known position.
        // This is crucial. If we lose sight, we'll still head to where they *were*.
        _hunterAI.NavAgent.SetDestination(_hunterAI.LastKnownPlayerPosition);

        // If we can still see the player, update their Last Known Position and reset the timer.
        if (_hunterAI.IsPlayerFullySpotted)
        {
            _hunterAI.LastKnownPlayerPosition = _hunterAI.PlayerTransform.position;
            timeSinceLostSight = 0f;
        }
        else
        {
            // If we've lost sight, start the grace period timer.
            timeSinceLostSight += Time.deltaTime;
        }

        float distanceToLKP = Vector3.Distance(_hunterAI.transform.position, _hunterAI.LastKnownPlayerPosition);

        // --- Transition Checks (in new priority order) ---

        // 1. To MELEE: Highest priority. Player is too close.
        if (distanceToLKP <= _hunterAI.MeleeRange)
        {
            Debug.LogWarning($"{_hunterAI.gameObject.name} Player IN MELEE RANGE. Transitioning to CloseKill.");
            SM.TransitToState(_hunterSM.CloseKillingState);
            return;
        }

        // 2. To AIMING: The MOST IMPORTANT CHANGE IS HERE.
        // We check if we are in shooting range of the LAST KNOWN POSITION.
        // We DON'T require IsPlayerFullySpotted to be true right now. This gives AimingState a chance.
        if (_hunterAI.AimAttemptCooldownTimer <= 0f && distanceToLKP <= _hunterAI.ShootingRange)
        {
            // Let the Aiming state figure out if the player is actually visible or behind cover.
            // This is the key to enabling the Suppressing state.
            Debug.Log($"{_hunterAI.gameObject.name}: In range of LKP. Transitioning to Aiming to assess the situation.");
            SM.TransitToState(_hunterSM.AimingState);
            return;
        }

        // 3. To INVESTIGATING: The fallback condition.
        // This only happens if we are OUTSIDE shooting range and the grace period has expired.
        if (timeSinceLostSight > GRACE_PERIOD_BEFORE_INVESTIGATING)
        {
            Debug.Log($"{_hunterAI.gameObject.name} lost sight of player for more than grace period. Transitioning to Investigate.");
            SM.TransitToState(_hunterSM.InvestigatingState);
            return;
        }
    }

    public override void OnExitState()
    {
        if (_hunterAI == null) return;
    }
}