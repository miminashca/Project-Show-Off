using UnityEngine;

public class HunterChasingState : State
{
    private HunterAI _hunterAI;
    private HunterStateMachine _hunterSM;

    private float timeSinceLostSight = 0f;
    private const float GRACE_PERIOD_BEFORE_INVESTIGATING = 0.5f;

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

        _hunterAI.HunterAnimator.SetFloat("MovementSpeed", _hunterAI.MovementSpeedChasing);

        // This logic prevents the sound from playing repeatedly if we are just cycling
        // between Chasing and Aiming/Suppressing. The yell should only happen once
        // at the start of the encounter.
        if (SM.PreviousState is not HunterAimingState and not HunterSuppressingState)
        {
            HunterEventBus.HunterSpottedPlayer(_hunterAI.PlayerTransform.gameObject);

            // We comment out the old sound system call...
            // _hunterAI.PlaySound(_hunterAI.SpottedPlayerSound);

            // NEW FMOD CHANGE
            // ...and replace it with our new FMOD event call.
            if (_hunterAI.SoundController != null)
            {
                _hunterAI.SoundController.PlayChaseYell();
            }
            // END FMOD CHANGE
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
        _hunterAI.NavAgent.SetDestination(_hunterAI.LastKnownPlayerPosition);

        if (_hunterAI.IsPlayerFullySpotted)
        {
            _hunterAI.LastKnownPlayerPosition = _hunterAI.PlayerTransform.position;
            timeSinceLostSight = 0f;
        }
        else
        {
            timeSinceLostSight += Time.deltaTime;
        }

        float distanceToLKP = Vector3.Distance(_hunterAI.transform.position, _hunterAI.LastKnownPlayerPosition);

        // --- Transition Checks (in new priority order) ---

        // 1. To MELEE (or point-blank shot)
        if (distanceToLKP <= _hunterAI.MeleeRange)
        {
            Debug.LogWarning($"{_hunterAI.gameObject.name} Player IN MELEE RANGE. Transitioning to Aiming for a point-blank shot.");
            SM.TransitToState(_hunterSM.AimingState);
            return;
        }

        // 2. To AIMING
        if (_hunterAI.AimAttemptCooldownTimer <= 0f && distanceToLKP <= _hunterAI.ShootingRange)
        {
            Debug.Log($"{_hunterAI.gameObject.name}: In range of LKP. Transitioning to Aiming to assess the situation.");
            SM.TransitToState(_hunterSM.AimingState);
            return;
        }

        // 3. To INVESTIGATING
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