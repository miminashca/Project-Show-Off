using UnityEngine;

public class HunterAimingState : State
{
    private HunterAI _hunterAI;
    private HunterStateMachine _hunterSM;

    private float _currentAimTime;
    private const float AIM_TRACKING_SPEED = 5f;
    private Vector3 _playerAimPointInternal;

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

        _playerAimPointInternal = _hunterAI.GetPlayerAimPoint(); // Use the getter from HunterAI
        if (_playerAimPointInternal == Vector3.zero) // Player might have disappeared
        {
            SM.TransitToState(_hunterSM.InvestigatingState);
            return;
        }


        Vector3 directionToPlayer = (_playerAimPointInternal - _hunterAI.EyeLevelTransform.position).normalized;
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            _hunterAI.transform.rotation = Quaternion.Slerp(_hunterAI.transform.rotation, targetRotation, Time.deltaTime * AIM_TRACKING_SPEED);
        }

        _currentAimTime -= Time.deltaTime;
        _hunterAI.CurrentAimTimer = _currentAimTime;

        if (!_hunterAI.IsPlayerVisible || Vector3.Distance(_hunterAI.transform.position, _hunterAI.PlayerTransform.position) > _hunterAI.ShootingRange * 1.1f)
        {
            SM.TransitToState(_hunterSM.ChasingState);
            return;
        }

        if (_currentAimTime <= 0f)
        {
            if (_hunterAI.IsPathToPlayerClearForShot(_playerAimPointInternal))
            {
                _hunterAI.CurrentConfirmedAimTarget = _playerAimPointInternal;
                SM.TransitToState(_hunterSM.ShootingState);
            }
            else
            {
                Debug.Log($"{_hunterAI.gameObject.name} path blocked for shot or target submerged.");
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