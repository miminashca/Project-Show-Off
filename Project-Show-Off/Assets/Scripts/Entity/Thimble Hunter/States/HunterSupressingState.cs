using UnityEngine;

public class HunterSuppressingState : State
{
    private HunterAI _hunterAI;
    private HunterStateMachine _hunterSM;

    private Vector3 _suppressionTarget;
    private float _timeBetweenShots = 1.2f;
    private float _shotTimer;
    private int _shotsFired;
    private int _maxShots = 3;
    private float _stateDuration = 5.0f;
    private float _stateTimer;

    public HunterSuppressingState(StateMachine stateMachine) : base(stateMachine)
    {
        _hunterSM = stateMachine as HunterStateMachine;
        _hunterAI = _hunterSM.HunterAI;
    }

    public override void OnEnterState()
    {
        Debug.Log("Entering SUPPRESSING state.");
        _hunterAI.NavAgent.isStopped = true;
        _hunterAI.HunterAnimator.SetBool("IsAiming", true); // Stay in aiming pose

        // The target is the last place the Hunter knew the player was. This is the "cover".
        _suppressionTarget = _hunterAI.LastKnownPlayerPosition;

        _shotsFired = 0;
        _shotTimer = 0.5f; // Fire the first shot quickly
        _stateTimer = _stateDuration;
    }

    public override void Handle()
    {
        // Always look for the player. If they pop out, go back to chasing/aiming.
        if (_hunterAI.IsPlayerFullySpotted && _hunterAI.IsPathToPlayerClearForShot(_hunterAI.GetPlayerAimPoint()))
        {
            SM.TransitToState(_hunterSM.AimingState);
            return;
        }

        _stateTimer -= Time.deltaTime;
        _shotTimer -= Time.deltaTime;

        // Turn to face the cover
        Vector3 directionToCover = (_suppressionTarget - _hunterAI.transform.position).normalized;
        if (directionToCover != Vector3.zero)
            _hunterAI.transform.rotation = Quaternion.Slerp(_hunterAI.transform.rotation, Quaternion.LookRotation(directionToCover), Time.deltaTime * 5f);


        if (_shotTimer <= 0 && _shotsFired < _maxShots)
        {
            // Fire at the cover, not accurately at the player
            // We can add a bit of random offset to make it look like he's spraying the area
            Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);
            Vector3 fireDirection = ((_suppressionTarget + randomOffset) - _hunterAI.GunMuzzleTransform.position).normalized;

            _hunterAI.SetActualFiringDirection(fireDirection);
            _hunterAI.FireGun(); // FireGun will handle spread etc.

            _shotsFired++;
            _shotTimer = _timeBetweenShots;
        }

        // Exit condition: Fired all shots or timer ran out
        if (_shotsFired >= _maxShots || _stateTimer <= 0)
        {
            Debug.Log("Suppressing fire finished. Repositioning.");
            // After suppressing, the hunter should move. Chasing to the cover is a good default.
            SM.TransitToState(_hunterSM.ChasingState);
        }
    }

    public override void OnExitState()
    {
        _hunterAI.HunterAnimator.SetBool("IsAiming", false);
    }
}