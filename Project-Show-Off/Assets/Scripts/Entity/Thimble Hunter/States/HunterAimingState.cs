using UnityEngine;

public class HunterAimingState : State
{
    private HunterAI _hunterAI;
    private HunterStateMachine _hunterSM;

    private float _currentAimTime;
    private const float AIM_TRACKING_SPEED = 5f; // Adjust for how quickly Hunter locks on

    public HunterAimingState(StateMachine stateMachine) : base(stateMachine)
    {
        _hunterSM = stateMachine as HunterStateMachine;
        if (_hunterSM == null)
        {
            Debug.LogError("ThimbleHunterAimingState received an incompatible StateMachine!", stateMachine);
            return;
        }
        _hunterAI = _hunterSM.HunterAI;
    }

    public override void OnEnterState()
    {
        if (_hunterAI == null) return;
        Debug.Log($"{_hunterAI.gameObject.name} entering AIMING state.");

        _hunterAI.NavAgent.isStopped = true; // Stop movement
        _hunterAI.NavAgent.velocity = Vector3.zero; // Ensure no sliding
        _hunterAI.HunterAnimator.SetBool("IsMoving", false);
        _hunterAI.HunterAnimator.SetBool("IsAiming", true); // Animation for aiming

        _currentAimTime = _hunterAI.AimTime;
        _hunterAI.CurrentAimTimer = _currentAimTime;

        // Optional: Play aiming sound cue
        HunterEventBus.HunterStartedAiming();
    }

    public override void Handle()
    {
        if (_hunterAI == null || _hunterAI.PlayerTransform == null)
        {
            Debug.LogWarning($"{_hunterAI.gameObject.name} lost player reference in AimingState. Transitioning to Investigate.");
            SM.TransitToState(_hunterSM.InvestigatingState);
            return;
        }

        // --- Aiming Logic ---
        // Slowly turn towards the player
        Vector3 playerAimPoint = _hunterAI.PlayerTransform.position + Vector3.up * 1.0f; // Approx player center
        Vector3 directionToPlayer = (playerAimPoint - _hunterAI.transform.position).normalized;
        if (directionToPlayer != Vector3.zero) // Avoid LookRotation error if direction is zero
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            _hunterAI.transform.rotation = Quaternion.Slerp(_hunterAI.transform.rotation, targetRotation, Time.deltaTime * AIM_TRACKING_SPEED);
        }

        _currentAimTime -= Time.deltaTime;
        _hunterAI.CurrentAimTimer = _currentAimTime;

        // --- Transition Checks (Priority Order) ---
        // 1. To CHASING: Player breaks LoS OR moves significantly out of ShootingRange during aim
        if (!_hunterAI.IsPlayerVisible || Vector3.Distance(_hunterAI.transform.position, _hunterAI.PlayerTransform.position) > _hunterAI.ShootingRange * 1.1f /* Add some hysteresis */)
        {
            Debug.Log($"{_hunterAI.gameObject.name} lost target or target moved out of range while aiming.");
            SM.TransitToState(_hunterSM.ChasingState);
            return;
        }
        // (Alternative: if LoS completely broken, could go to INVESTIGATING directly)
        // if (!_hunterAI.IsPlayerVisible)
        // {
        //     SM.TransitToState(_hunterSM.InvestigatingState);
        //     return;
        // }


        // 2. To SHOOTING: AimTimer expires AND LoS to Player's AimPoint is still clear
        if (_currentAimTime <= 0f)
        {
            Vector3 finalAimPoint = _hunterAI.PlayerTransform.position + Vector3.up * 1.0f;
            if (_hunterAI.IsPathToPlayerClearForShot(finalAimPoint)) // Use helper from ThimbleHunterAI
            {
                SM.TransitToState(_hunterSM.ShootingState);
            }
            else
            {
                // Path blocked at the last moment (e.g., player ducked, obstacle, water)
                Debug.Log($"{_hunterAI.gameObject.name} path blocked for shot at last moment.");
                SM.TransitToState(_hunterSM.ChasingState); // Re-evaluate, try to get clear shot
            }
            return;
        }
    }

    public override void OnExitState()
    {
        if (_hunterAI == null) return;
        Debug.Log($"{_hunterAI.gameObject.name} exiting AIMING state.");
        _hunterAI.HunterAnimator.SetBool("IsAiming", false);
        _hunterAI.CurrentAimTimer = 0f;
        // NavAgent.isStopped will be handled by the next state if it needs movement.
    }
}