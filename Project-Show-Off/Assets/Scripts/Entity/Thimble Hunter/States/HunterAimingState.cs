using UnityEngine;

public class HunterAimingState : State
{
    private HunterAI _hunterAI;
    private HunterStateMachine _hunterSM;

    private float _currentAimTime;
    private Vector3 _playerAimPointInternal;
    private Vector3 currentGunDirection;
    private float timeOnTarget = 0f;
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

        Vector3 initialTargetPoint = _hunterAI.LastKnownPlayerPosition;
        _hunterAI.CurrentConfirmedAimTarget = initialTargetPoint;

        if (initialTargetPoint != Vector3.zero && _hunterAI.GunMuzzleTransform != null)
        {
            currentGunDirection = (initialTargetPoint - _hunterAI.GunMuzzleTransform.position).normalized;
        }
        else
        {
            currentGunDirection = _hunterAI.transform.forward;
        }
        timeOnTarget = 0f;

        swayOffsetX = Random.Range(0f, 100f);
        swayOffsetY = Random.Range(0f, 100f);

        _hunterAI.PlaySound(_hunterAI.StartAimingSound);
    }

    public override void Handle()
    {
        if (_hunterAI == null || _hunterAI.PlayerTransform == null)
        {
            SM.TransitToState(_hunterSM.InvestigatingState);
            return;
        }

        if (_hunterAI.IsPlayerFullySpotted)
        {
            _playerAimPointInternal = _hunterAI.GetPlayerAimPoint();
            _hunterAI.LastKnownPlayerPosition = _playerAimPointInternal;
        }
        else
        {
            _playerAimPointInternal = _hunterAI.LastKnownPlayerPosition;
        }
        _hunterAI.CurrentConfirmedAimTarget = _playerAimPointInternal;

        Vector3 directionToTarget = (_playerAimPointInternal - _hunterAI.transform.position).normalized;
        if (directionToTarget != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(directionToTarget.x, 0, directionToTarget.z));
            _hunterAI.transform.rotation = Quaternion.Slerp(_hunterAI.transform.rotation, lookRotation, Time.deltaTime * _hunterAI.BodyTurnSpeedInAim);
        }

        Vector3 idealGunDirection = (_playerAimPointInternal - _hunterAI.GunMuzzleTransform.position).normalized;
        if (idealGunDirection == Vector3.zero) idealGunDirection = _hunterAI.transform.forward;

        currentGunDirection = Vector3.Slerp(currentGunDirection, idealGunDirection, Time.deltaTime * _hunterAI.AimCatchUpSpeed);
        float swayX = (Mathf.PerlinNoise(swayOffsetX + Time.time * _hunterAI.AimSwaySpeed, 0f) * 2f - 1f) * _hunterAI.MaxAimSwayAngle;
        float swayY = (Mathf.PerlinNoise(0f, swayOffsetY + Time.time * _hunterAI.AimSwaySpeed) * 2f - 1f) * _hunterAI.MaxAimSwayAngle;
        Quaternion unsweptGunWorldRotation = Quaternion.LookRotation(currentGunDirection);
        Quaternion localSwayRotation = Quaternion.Euler(swayY, swayX, 0f);
        Quaternion swayedGunWorldRotation = unsweptGunWorldRotation * localSwayRotation;
        Vector3 finalSwayedGunDirection = swayedGunWorldRotation * Vector3.forward;

        float angleToIdealTarget = Vector3.Angle(finalSwayedGunDirection, idealGunDirection);
        if (angleToIdealTarget <= _hunterAI.MinAngleForShotConfidence) timeOnTarget += Time.deltaTime;
        else timeOnTarget -= Time.deltaTime * 0.5f;
        timeOnTarget = Mathf.Clamp(timeOnTarget, 0f, _hunterAI.TimeToMaxConfidence);
        float currentShotConfidence = (Mathf.Approximately(_hunterAI.TimeToMaxConfidence, 0f)) ? 1f : (timeOnTarget / _hunterAI.TimeToMaxConfidence);

        _currentAimTime -= Time.deltaTime;
        _hunterAI.CurrentAimTimer = _currentAimTime;

        // --- REMOVED REDUNDANT EXIT CHECK ---
        // The logic below handles all exit cases correctly now.

        bool shouldTakeShot = false;
        if (currentShotConfidence >= _hunterAI.ShotConfidenceThreshold) shouldTakeShot = true;
        else if (_currentAimTime <= 0f)
        {
            shouldTakeShot = true;
        }

        if (shouldTakeShot)
        {
            if (_hunterAI.IsPlayerFullySpotted && _hunterAI.IsPathToPlayerClearForShot(_playerAimPointInternal))
            {
                Debug.Log($"{_hunterAI.gameObject.name} AIMING: Path is clear, taking lethal shot.");
                _hunterAI.SetActualFiringDirection(finalSwayedGunDirection);
                SM.TransitToState(_hunterSM.ShootingState);
            }
            else
            {
                Debug.LogWarning($"{_hunterAI.gameObject.name} AIMING: Path blocked or player not visible. Transitioning to SUPPRESSING.");
                SM.TransitToState(_hunterSM.SuppressingState);
            }
            return;
        }

        if (!_hunterAI.IsPlayerFullySpotted && _currentAimTime <= 0f)
        {
            Debug.Log($"{_hunterAI.gameObject.name}: Aim timer expired and player not visible. Returning to Chase.");
            SM.TransitToState(_hunterSM.ChasingState);
        }
    }

    public override void OnExitState()
    {
        if (_hunterAI == null) return;
        _hunterAI.HunterAnimator.SetBool("IsAiming", false);
        _hunterAI.CurrentAimTimer = 0f;
        _hunterAI.CurrentConfirmedAimTarget = Vector3.zero;
    }
}