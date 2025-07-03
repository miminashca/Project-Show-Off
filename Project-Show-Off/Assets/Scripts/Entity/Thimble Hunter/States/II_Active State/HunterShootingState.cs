using UnityEngine;

public class HunterShootingState : State
{
    private HunterAI _hunterAI;
    private HunterStateMachine _hunterSM;
    private float _timeInState;

    private float _currentReloadTime;
    private bool _isReloading;

    public HunterShootingState(StateMachine stateMachine) : base(stateMachine)
    {
        _hunterSM = stateMachine as HunterStateMachine;
        _hunterAI = _hunterSM.HunterAI;
    }

    public override void OnEnterState()
    {
        if (_hunterAI == null) return;
        Debug.Log($"{_hunterAI.gameObject.name} in SHOOTING state, waiting for animation to finish.");
    }

    public override void Handle()
    {
        if (_hunterAI == null) return;

        // The job of this state is to wait until the Animator is no longer
        // in the "Shooting" or "Reloading" states.
        AnimatorStateInfo stateInfo = _hunterAI.HunterAnimator.GetCurrentAnimatorStateInfo(0); // 0 is the base layer index

        // Check if the current animation is NOT Shooting and NOT Reloading.
        // NOTE: Use the exact names of your states from the Animator window.
        if (!stateInfo.IsName("Shooting") && !stateInfo.IsName("Reloading"))
        {
            Debug.Log("Shooting/Reloading animation cycle complete. Deciding next action.");
            DecideNextAction();
        }
    }

    private void DecideNextAction()
    {
        if (_hunterAI.PlayerTransform == null)
        {
            SM.TransitToState(_hunterSM.RoamingState);
            return;
        }

        // After shooting/reloading, decide whether to aim again, chase, or investigate.
        if (_hunterAI.IsPlayerFullySpotted && Vector3.Distance(_hunterAI.transform.position, _hunterAI.PlayerTransform.position) <= _hunterAI.ShootingRange)
        {
            SM.TransitToState(_hunterSM.AimingState);
        }
        else if (_hunterAI.IsPlayerFullySpotted)
        {
            SM.TransitToState(_hunterSM.ChasingState);
        }
        else
        {
            SM.TransitToState(_hunterSM.InvestigatingState);
        }
    }

    public override void OnExitState()
    {
        if (_hunterAI == null) return;
        _hunterAI.CurrentReloadTimer = 0f;
    }
}