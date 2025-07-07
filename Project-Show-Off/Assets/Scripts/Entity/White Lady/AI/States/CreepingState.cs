using UnityEngine;

public class CreepingState : State
{
    private new LadyStateMachine SM;
    private float _gazeBuildupTimer;

    private bool targetCurrentlyVisible = false;

    public CreepingState(LadyStateMachine pSM) : base(pSM)
    {
        SM = pSM;
    }

    public override void OnEnterState()
    {
        Debug.Log("Entering CREEPING State");
        SM.FeedbackController.StopAllEffects();

        _gazeBuildupTimer = 0f;

        SM.WLSoundController.PlayLullaby(); // NEW FMOD CHANGE
    }

    public override void Handle()
    {
        // Req 3.1.3: Transition to SEEN
        _gazeBuildupTimer += Time.deltaTime;

        if (SM.GazeSystem.IsTargetVisible != targetCurrentlyVisible)
        {
            _gazeBuildupTimer = 0f;
            targetCurrentlyVisible = SM.GazeSystem.IsTargetVisible;
        }

        if (targetCurrentlyVisible)
        {
            if (_gazeBuildupTimer >= SM.Config.timeToTriggerSeen)
            {
                base.SM.TransitToState(SM.SeenState);
            }
        }
        if (!targetCurrentlyVisible)
        {
            if (_gazeBuildupTimer >= SM.Config.timeToDissipate)
            {
                base.SM.TransitToState(SM.DissipatedState);
            }
        }
    }

    public override void OnExitState()
    {
        Debug.Log("Exiting CREEPING State");
        // Weeping/humming sound will stop naturally as it's a one-shot.
        // Other looping sounds are stopped by the new state's entry logic.
    }

    private void TriggerTargetVisible(bool visible)
    {
        Debug.LogError("Lady visible: " + visible);
        _gazeBuildupTimer = 0f;
        targetCurrentlyVisible = visible;
    }
}