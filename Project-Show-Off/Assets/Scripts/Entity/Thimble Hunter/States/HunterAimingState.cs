using UnityEngine;

public class HunterAimingState : State
{
    private HunterAI _hunterAI;
    private HunterStateMachine _hunterSM;

    private float _currentAimTime;
    private Vector3 _playerAimPointInternal;

    private Vector3 currentGunDirection; // The actual direction the gun is pointing, smoothed.
    private float timeOnTarget = 0f;

    // For sway
    private float swayOffsetX;
    private float swayOffsetY;

    public HunterAimingState(StateMachine stateMachine) : base(stateMachine)
    {
        _hunterSM = stateMachine as HunterStateMachine;
        _hunterAI = _hunterSM.HunterAI;
    }

    public override void OnEnterState()
    {
        if (_hunterAI == null) return;
        Debug.Log($"{_hunterAI.gameObject.name} entering AIMING state.");

        _hunterAI.NavAgent.isStopped = true;
        _hunterAI.NavAgent.velocity = Vector3.zero;
        _hunterAI.HunterAnimator.SetBool("IsMoving", false);
        _hunterAI.HunterAnimator.SetBool("IsAiming", true);

        _currentAimTime = _hunterAI.AimTime;
        _hunterAI.CurrentAimTimer = _currentAimTime;
        _hunterAI.CurrentConfirmedAimTarget = Vector3.zero;

        // Initialize currentGunDirection towards where the player was last seen or is currently.
        Vector3 initialTargetPoint = _hunterAI.GetPlayerAimPoint();
        if (initialTargetPoint != Vector3.zero && _hunterAI.GunMuzzleTransform != null) // Added GunMuzzleTransform null check
        {
            currentGunDirection = (initialTargetPoint - _hunterAI.GunMuzzleTransform.position).normalized;
        }
        else if (_hunterAI.transform != null) // Added null check for safety
        {
            currentGunDirection = _hunterAI.transform.forward; // Fallback
        }
        else
        {
            currentGunDirection = Vector3.forward; // Absolute fallback
        }
        timeOnTarget = 0f;

        // Initialize sway offsets for Perlin noise to make it smooth
        swayOffsetX = Random.Range(0f, 100f);
        swayOffsetY = Random.Range(0f, 100f);

        HunterEventBus.HunterStartedAiming();
        _hunterAI.PlaySound(_hunterAI.StartAimingSound);
    }

    public override void Handle()
    {
        if (_hunterAI == null || _hunterAI.PlayerTransform == null)
        {
            SM.TransitToState(_hunterSM.InvestigatingState);
            return;
        }

        // 1. Determine the Ideal Target Point (where the Hunter *wants* to aim)
        //    _playerAimPointInternal = _hunterAI.GetPlayerAimPoint(); // This is fine from your existing code
        //    This _playerAimPointInternal will eventually come from the Cone Vision system (e.g., center of visible mass)
        _playerAimPointInternal = _hunterAI.GetPlayerAimPoint();
        if (_playerAimPointInternal == Vector3.zero)
        {
            SM.TransitToState(_hunterSM.InvestigatingState); // Lost target
            return;
        }
        Vector3 idealGunDirection = (_playerAimPointInternal - _hunterAI.GunMuzzleTransform.position).normalized;
        if (idealGunDirection == Vector3.zero) // Avoid issues if player is at muzzle position
        {
            idealGunDirection = _hunterAI.transform.forward;
        }

        // 2. Smoothly Lerp/Slerp currentGunDirection towards idealGunDirection
        currentGunDirection = Vector3.Slerp(currentGunDirection, idealGunDirection, Time.deltaTime * _hunterAI.AimCatchUpSpeed);

        // 3. Apply Aim Sway
        //    This makes the gun not perfectly still even if currentGunDirection matches idealGunDirection.
        float swayX = (Mathf.PerlinNoise(swayOffsetX + Time.time * _hunterAI.AimSwaySpeed, 0f) * 2f - 1f) * _hunterAI.MaxAimSwayAngle;
        float swayY = (Mathf.PerlinNoise(0f, swayOffsetY + Time.time * _hunterAI.AimSwaySpeed) * 2f - 1f) * _hunterAI.MaxAimSwayAngle;

        // Create a rotation representing the unswept, lagged gun direction in world space.
        Quaternion unsweptGunWorldRotation = Quaternion.LookRotation(currentGunDirection);

        // Create a local sway rotation (relative to the gun's current facing).
        // swayY is pitch (up/down), swayX is yaw (left/right).
        Quaternion localSwayRotation = Quaternion.Euler(swayY, swayX, 0f);

        // Apply the local sway to the unswept gun's world rotation.
        // The order matters: worldRotation * localOffsetRotation
        Quaternion swayedGunWorldRotation = unsweptGunWorldRotation * localSwayRotation;

        // Get the forward vector of this final, swayed world rotation. This is where the gun is truly pointing.
        Vector3 finalSwayedGunDirection = swayedGunWorldRotation * Vector3.forward;

        // For visualization and actual shooting, this finalSwayedGunDirection is what matters.
        // The Hunter's model should also orient its body towards this swayed direction.
        if (finalSwayedGunDirection != Vector3.zero)
        {
            // Make the Hunter's body smoothly turn to look in the direction the gun is now pointing (including sway)
            _hunterAI.transform.rotation = Quaternion.Slerp(
                _hunterAI.transform.rotation,
                Quaternion.LookRotation(finalSwayedGunDirection),
                Time.deltaTime * 10f // This 10f is the Hunter's body turn speed
            );
        }


        // 4. Update "Time on Target" & Shot Confidence
        float angleToIdealTarget = Vector3.Angle(finalSwayedGunDirection, idealGunDirection);
        if (angleToIdealTarget <= _hunterAI.MinAngleForShotConfidence)
        {
            timeOnTarget += Time.deltaTime;
        }
        else
        {
            timeOnTarget -= Time.deltaTime * 0.5f;
        }
        timeOnTarget = Mathf.Clamp(timeOnTarget, 0f, _hunterAI.TimeToMaxConfidence);
        float currentShotConfidence = (Mathf.Approximately(_hunterAI.TimeToMaxConfidence, 0f)) ? 1f : (timeOnTarget / _hunterAI.TimeToMaxConfidence);

        // Debugging
        Debug.Log($"AIMING: AimTimeLeft: {_currentAimTime:F2}, AngleToIdeal: {angleToIdealTarget:F2}, Confidence: {currentShotConfidence:F2}");

        // 5. Check Exit Conditions (Player not visible, out of range)
        //    This needs to use the new Cone Vision IsPlayerVisible once implemented.
        //    For now, it uses the old IsPlayerVisible.
        _currentAimTime -= Time.deltaTime;
        _hunterAI.CurrentAimTimer = _currentAimTime;

        if (!_hunterAI.IsPlayerFullySpotted || Vector3.Distance(_hunterAI.GunMuzzleTransform.position, _playerAimPointInternal) > _hunterAI.ShootingRange * 1.1f)
        {
            SM.TransitToState(_hunterSM.ChasingState);
            return;
        }

        // 6. Decide to Shoot
        bool shouldTakeShot = false;
        if (currentShotConfidence >= _hunterAI.ShotConfidenceThreshold)
        {
            shouldTakeShot = true;
        }
        // "Patience" / Forced Shot (if _currentAimTime runs out)
        else if (_currentAimTime <= 0f)
        {
            Debug.LogWarning($"{_hunterAI.gameObject.name} AIMING: Patience ran out, taking a less confident shot!");
            shouldTakeShot = true;
        }

        if (shouldTakeShot)
        {
            // Path check still important, but now it's about MAJOR obstructions.
            // The IsPathToPlayerClearForShot should check if there's a wall, not if the aim is pixel perfect.
            // The "accuracy" of the shot itself will be handled by weapon spread.
            if (_hunterAI.IsPathToPlayerClearForShot(_playerAimPointInternal)) // Use the player's *ideal* aim point for obstruction check
            {
                // Pass the ACTUAL gun direction for firing, not the ideal player point
                _hunterAI.SetActualFiringDirection(finalSwayedGunDirection); // New method in HunterAI
                SM.TransitToState(_hunterSM.ShootingState);
            }
            else
            {
                Debug.LogWarning($"{_hunterAI.gameObject.name} AIMING: Path blocked for shot. Target: {_playerAimPointInternal}. Cooldown.");
                _hunterAI.TriggerAimAttemptCooldown(1.0f);
                SM.TransitToState(_hunterSM.ChasingState);
            }
            return;
        }
    }

    public override void OnExitState()
    {
        if (_hunterAI == null) return;
        _hunterAI.HunterAnimator.SetBool("IsAiming", false);
        _hunterAI.CurrentAimTimer = 0f;
    }
}