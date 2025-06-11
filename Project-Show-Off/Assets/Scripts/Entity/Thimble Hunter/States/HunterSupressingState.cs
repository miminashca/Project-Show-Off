using UnityEngine;

public class HunterSuppressingState : State
{
    private HunterAI _hunterAI;
    private HunterStateMachine _hunterSM;

    private Vector3 _suppressionTarget;
    private float _shotTimer;
    private int _shotsFired;
    private float _stateTimer;

    // ... (constructor, OnEnterState, Handle, and FireSuppressiveShot are unchanged) ...
    public HunterSuppressingState(StateMachine stateMachine) : base(stateMachine)
    {
        _hunterSM = stateMachine as HunterStateMachine;
        _hunterAI = _hunterSM.HunterAI;
    }

    public override void OnEnterState()
    {
        if (_hunterAI == null) return;
        Debug.Log($"{_hunterAI.gameObject.name} entering SUPPRESSING state.");

        _hunterAI.NavAgent.isStopped = true;
        _hunterAI.HunterAnimator.SetBool("IsAiming", true);

        // Target the last known position. This is the cover the player is behind.
        _suppressionTarget = _hunterAI.LastKnownPlayerPosition;

        _shotsFired = 0;
        _shotTimer = 0.5f; // Fire the first shot quickly to apply immediate pressure.
        _stateTimer = _hunterAI.SuppressingStateDuration; // Use new variable from HunterAI
    }

    public override void Handle()
    {
        if (_hunterAI == null) return;

        // --- HIGHEST PRIORITY: Player makes a mistake ---
        if (_hunterAI.IsPlayerFullySpotted && _hunterAI.IsPathToPlayerClearForShot(_hunterAI.TargetPlayerStatus.TorsoVisibilityPoint.position))
        {
            Debug.Log($"{_hunterAI.gameObject.name}: Player has left cover! Re-engaging.");
            SM.TransitToState(_hunterSM.AimingState);
            return;
        }

        _stateTimer -= Time.deltaTime;
        _shotTimer -= Time.deltaTime;

        // Keep facing the cover while suppressing.
        Vector3 directionToCover = (_suppressionTarget - _hunterAI.transform.position).normalized;
        if (directionToCover != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(directionToCover.x, 0, directionToCover.z));
            _hunterAI.transform.rotation = Quaternion.Slerp(_hunterAI.transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        // --- FIRING LOGIC ---
        if (_shotTimer <= 0 && _shotsFired < _hunterAI.SuppressingMaxShots) // Use new variable
        {
            FireSuppressiveShot();
            _shotsFired++;
            _shotTimer = _hunterAI.SuppressingTimeBetweenShots; // Use new variable
        }

        // --- EXIT CONDITIONS ---
        if (_shotsFired >= _hunterAI.SuppressingMaxShots || _stateTimer <= 0)
        {
            Debug.Log("Suppressing fire finished. Repositioning.");

            // --- *** THE FIX IS HERE *** ---
            // Before transitioning, force a cooldown on aiming.
            // This forces the Hunter to actually move for a bit in the Chasing state.
            _hunterAI.TriggerAimAttemptCooldown(2.0f); // e.g., 2-second cooldown before it can aim again.

            SM.TransitToState(_hunterSM.ChasingState);
        }
    }

    private void FireSuppressiveShot()
    {
        // This method is unchanged
        Vector3 fireDirection = (_suppressionTarget - _hunterAI.GunMuzzleTransform.position).normalized;
        Vector2 randomCirclePoint = Random.insideUnitCircle * _hunterAI.SuppressingSpreadRadius;
        Quaternion rotationToDirection = Quaternion.LookRotation(fireDirection);
        Vector3 randomOffset = rotationToDirection * new Vector3(randomCirclePoint.x, randomCirclePoint.y, 0);
        Vector3 finalFireTarget = _suppressionTarget + randomOffset;
        Vector3 finalFireDirection = (finalFireTarget - _hunterAI.GunMuzzleTransform.position).normalized;
        _hunterAI.SetActualFiringDirection(finalFireDirection);
        _hunterAI.FireGun();
    }

    public override void OnExitState()
    {
        if (_hunterAI == null) return;
        _hunterAI.HunterAnimator.SetBool("IsAiming", false);
    }
}