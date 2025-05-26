using UnityEngine;

public class ThimbleHunterShootingState : State
{
    private ThimbleHunterAI _hunterAI;
    private ThimbleHunterStateMachine _hunterSM;

    private float _currentReloadTime;
    private bool _hasFired;

    public ThimbleHunterShootingState(StateMachine stateMachine) : base(stateMachine)
    {
        _hunterSM = stateMachine as ThimbleHunterStateMachine;
        if (_hunterSM == null)
        {
            Debug.LogError("ThimbleHunterShootingState received an incompatible StateMachine!", stateMachine);
            return;
        }
        _hunterAI = _hunterSM.HunterAI;
    }

    public override void OnEnterState()
    {
        if (_hunterAI == null) return;
        Debug.Log($"{_hunterAI.gameObject.name} entering SHOOTING state.");

        _hunterAI.NavAgent.isStopped = true; // Remain stationary
        _hunterAI.NavAgent.velocity = Vector3.zero;
        _hunterAI.HunterAnimator.SetBool("IsMoving", false);
        // Animator trigger for "Shoot" will be called by _hunterAI.FireGun()

        _hasFired = false;
        _currentReloadTime = _hunterAI.ReloadTime;
        _hunterAI.CurrentReloadTimer = _currentReloadTime;

        // Fire the gun immediately
        _hunterAI.FireGun(); // This method handles raycast, damage, VFX, SFX
        _hasFired = true;

        // Start reload animation if separate from shooting
        // _hunterAI.HunterAnimator.SetTrigger("Reload");
    }

    public override void Handle()
    {
        if (_hunterAI == null) return;

        // --- Reload Logic ---
        if (_hasFired)
        {
            _currentReloadTime -= Time.deltaTime;
            _hunterAI.CurrentReloadTimer = _currentReloadTime;

            if (_currentReloadTime <= 0f)
            {
                // Reload complete, decide next action
                Debug.Log($"{_hunterAI.gameObject.name} reload complete.");

                // --- Transition Checks (Priority Order - after ReloadTimer expires) ---
                // 1. To AIMING: Player is still within ShootingRange AND LoS is clear
                if (_hunterAI.IsPlayerVisible && _hunterAI.PlayerTransform != null &&
                    Vector3.Distance(_hunterAI.transform.position, _hunterAI.PlayerTransform.position) <= _hunterAI.ShootingRange)
                {
                    SM.TransitToState(_hunterSM.AimingState);
                    return;
                }
                // 2. To CHASING: Player is in VisionCone but outside ShootingRange
                else if (_hunterAI.IsPlayerVisible)
                {
                    SM.TransitToState(_hunterSM.ChasingState);
                    return;
                }
                // 3. To INVESTIGATING: Player is no longer in VisionCone
                else if (_hunterAI.PlayerTransform != null) // Check if player still exists
                {
                    // LKP should be the position where the player was last seen (or shot at)
                    // ThimbleHunterAI's ProcessSensors might have updated LKP if player briefly reappeared
                    // or it defaults to the last known.
                    SM.TransitToState(_hunterSM.InvestigatingState);
                    return;
                }
                // 4. To ROAMING: Player is completely lost (e.g., PlayerTransform is null or some other condition)
                else
                {
                    SM.TransitToState(_hunterSM.RoamingState);
                    return;
                }
            }
        }
    }

    public override void OnExitState()
    {
        if (_hunterAI == null) return;
        Debug.Log($"{_hunterAI.gameObject.name} exiting SHOOTING state.");
        _hunterAI.CurrentReloadTimer = 0f;
        // NavAgent.isStopped will be handled by the next state.
    }
}