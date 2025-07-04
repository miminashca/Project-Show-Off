using UnityEngine;

public class SeenState : State
{
    private LadyStateMachine SM;
    private float _lookAwayTimer;
    private bool _hasDamagedPlayer;

    public SeenState(LadyStateMachine pSM) : base(pSM)
    {
        SM = pSM;
    }

    public override void OnEnterState()
    {
        Debug.Log("Entering SEEN State");
        _lookAwayTimer = 0f;
        _hasDamagedPlayer = false;
        
        // Req 3.2.1: Stop creeping audio cues (handled by starting new effects)
        // and trigger SEEN effects.
        SM.FeedbackController.StartSeenEffects(SM.PullTargetTransform);

        SM.WLSoundController.StartKillNoise();
    }

    public override void Handle()
    {
        // Check for transition first
        if (!SM.GazeSystem.IsTargetVisible)
        {
            // Player is looking away.
            // Immediately disable the camera pull for a rewarding "breakaway" feel.
            SM.FeedbackController.SetGazePullActive(false);
            
            // Req 3.2.3: Transition to DISSIPATED
            _lookAwayTimer += Time.deltaTime;
            if (_lookAwayTimer >= SM.Config.timeToReturnCreeping)
            {
                base.SM.TransitToState(SM.CreepingState);
            }
        }
        else
        {
            // Player is looking at her.
            // *** NEW: Ensure the camera pull is active. ***
            SM.FeedbackController.SetGazePullActive(true);
            
            // Player is looking. Reset look-away timer and process gaze.
            _lookAwayTimer = 0f;

            // Req 3.2.2: State Behavior (Continuous Gaze)
            // The gaze timer is paused when not in this state because we increment it here.
            SM.ContinuousGazeTimer += Time.deltaTime;
            
            RotateTowardsPlayer();
            // Update camera pull every frame
            //SM.FeedbackController.UpdateSeenEffects(SM.transform);

            // Check for death first
            if (SM.ContinuousGazeTimer >= SM.Config.timeToDeath)
            {
                SM.FeedbackController.KillPlayer();
                // Optionally transition to a "PlayerKilled" state to prevent further logic.
            }
            // Check for damage
            else if (!_hasDamagedPlayer && SM.ContinuousGazeTimer >= SM.Config.timeToDamage)
            {
                SM.FeedbackController.InflictHealthDamage();
                _hasDamagedPlayer = true; // Ensure damage is only applied once
            }
        }
    }
    
    /// <summary>
    /// Smoothly rotates the White Lady to face the player's camera.
    /// </summary>
    private void RotateTowardsPlayer()
    {
        if (SM.PlayerTransform == null) return;

        // 1. Get the direction from the lady to the player.
        Vector3 directionToPlayer = SM.PlayerTransform.position - SM.transform.position;
        
        // 2. We only want to rotate on the Y-axis so she doesn't tilt up or down.
        // We do this by zeroing out the Y component of the direction vector.
        directionToPlayer.y = 0;

        // 3. If the direction is not zero (to avoid errors when player is directly above/below),
        // calculate the target rotation.
        if (directionToPlayer.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);

            // 4. Smoothly interpolate from the current rotation to the target rotation.
            SM.transform.rotation = Quaternion.Slerp(
                SM.transform.rotation, 
                targetRotation, 
                Time.deltaTime * SM.Config.turnSpeed
            );
        }
    }


    public override void OnExitState()
    {
        Debug.Log("Exiting SEEN State");
        // Gaze timer automatically "pauses" because Handle() is no longer called.
        // When re-entering, it will pick up where it left off.
        // The DISSIPATED state is responsible for stopping all feedback effects.

        SM.WLSoundController.StopKillNoise();
    }
}