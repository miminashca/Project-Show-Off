using UnityEngine;

public class HunterAimingState : State
{
    private HunterAI _hunterAI;
    private HunterStateMachine _hunterSM;

    private float _currentAimTime;
    private const float AIM_TRACKING_SPEED = 5f;
    private Vector3 _playerAimPointInternal;

    [SerializeField] private float aimCatchUpSpeed = 2.0f; // How quickly the gun tries to catch up to the target point.
    [SerializeField] private float maxAimSwayAngle = 1.5f; // Max random sway in degrees from the "perfect" aim.
    [SerializeField] private float swaySpeed = 1.0f;      // How quickly the sway oscillates.

    private Vector3 currentGunDirection; // The actual direction the gun is pointing, smoothed.
    private float timeOnTarget = 0f;
    [SerializeField] private float timeToMaxConfidence = 1.5f; // Time needed on target for max confidence.
    [SerializeField] private float shotConfidenceThreshold = 0.75f; // (0 to 1)
    [SerializeField] private float minAngleForConfidence = 5.0f; // How close gunDir must be to targetDir to gain confidence (degrees)

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
        if (initialTargetPoint != Vector3.zero)
        {
            currentGunDirection = (initialTargetPoint - _hunterAI.GunMuzzleTransform.position).normalized;
        }
        else
        {
            currentGunDirection = _hunterAI.transform.forward; // Fallback
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

        // 2. Smoothly Lerp/Slerp currentGunDirection towards idealGunDirection
        currentGunDirection = Vector3.Slerp(currentGunDirection, idealGunDirection, Time.deltaTime * aimCatchUpSpeed);

        // 3. Apply Aim Sway
        //    This makes the gun not perfectly still even if currentGunDirection matches idealGunDirection.
        float swayX = (Mathf.PerlinNoise(swayOffsetX + Time.time * swaySpeed, 0f) * 2f - 1f) * maxAimSwayAngle;
        float swayY = (Mathf.PerlinNoise(0f, swayOffsetY + Time.time * swaySpeed) * 2f - 1f) * maxAimSwayAngle;

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
        if (angleToIdealTarget <= minAngleForConfidence) // If aim is "close enough"
        {
            timeOnTarget += Time.deltaTime;
        }
        else
        {
            timeOnTarget -= Time.deltaTime * 0.5f; // Lose confidence faster if off-target, or just reset/decay
        }
        timeOnTarget = Mathf.Clamp(timeOnTarget, 0f, timeToMaxConfidence);
        float currentShotConfidence = timeOnTarget / timeToMaxConfidence; // Normalized 0-1

        // Debugging
        Debug.Log($"AIMING: AimTimeLeft: {_currentAimTime:F2}, AngleToIdeal: {angleToIdealTarget:F2}, Confidence: {currentShotConfidence:F2}");


        // 5. Check Exit Conditions (Player not visible, out of range)
        //    This needs to use the new Cone Vision IsPlayerVisible once implemented.
        //    For now, it uses the old IsPlayerVisible.
        _currentAimTime -= Time.deltaTime;
        _hunterAI.CurrentAimTimer = _currentAimTime;

        if (!_hunterAI.IsPlayerVisible || Vector3.Distance(_hunterAI.GunMuzzleTransform.position, _playerAimPointInternal) > _hunterAI.ShootingRange * 1.1f)
        {
            SM.TransitToState(_hunterSM.ChasingState);
            return;
        }

        // 6. Decide to Shoot
        bool shouldTakeShot = false;
        if (currentShotConfidence >= shotConfidenceThreshold)
        {
            shouldTakeShot = true;
        }
        // "Patience" / Forced Shot (if _currentAimTime runs out)
        else if (_currentAimTime <= 0f)
        {
            Debug.LogWarning($"{_hunterAI.gameObject.name} AIMING: Patience ran out, taking a less confident shot!");
            shouldTakeShot = true; // Could have a lower confidence requirement here or a different flag
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