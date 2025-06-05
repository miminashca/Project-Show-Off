using UnityEngine;

public class HunterShootingState : State
{
    private HunterAI _hunterAI;
    private HunterStateMachine _hunterSM;

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
        Debug.Log($"{_hunterAI.gameObject.name} entering SHOOTING state.");

        _hunterAI.NavAgent.isStopped = true;
        _hunterAI.NavAgent.velocity = Vector3.zero;
        _hunterAI.HunterAnimator.SetBool("IsMoving", false);
        // Animator trigger "Shoot" is called by _hunterAI.FireGun()

        _isReloading = false; // Will be set to true after firing

        // Fire the gun immediately using the confirmed aim target
        _hunterAI.FireGun();

        // Start reload phase
        _isReloading = true;
        _currentReloadTime = _hunterAI.ReloadTime;
        _hunterAI.CurrentReloadTimer = _currentReloadTime;
        _hunterAI.HunterAnimator.SetTrigger("Reload"); // Assuming a "Reload" trigger
        _hunterAI.PlaySound(_hunterAI.ReloadSound);
    }

    public override void Handle()
    {
        if (_hunterAI == null) return;

        if (_isReloading)
        {
            _currentReloadTime -= Time.deltaTime;
            _hunterAI.CurrentReloadTimer = _currentReloadTime;

            if (_currentReloadTime <= 0f)
            {
                _isReloading = false; // Reload complete
                Debug.Log($"{_hunterAI.gameObject.name} reload complete.");
                DecideNextAction();
            }
        }
    }

    private void DecideNextAction()
    {
        if (_hunterAI.PlayerTransform == null) // Player might have been destroyed
        {
            SM.TransitToState(_hunterSM.RoamingState);
            return;
        }

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
            // LKP was updated when player was last seen or shot at
            SM.TransitToState(_hunterSM.InvestigatingState);
        }
    }

    public override void OnExitState()
    {
        if (_hunterAI == null) return;
        _hunterAI.CurrentReloadTimer = 0f;
    }
}