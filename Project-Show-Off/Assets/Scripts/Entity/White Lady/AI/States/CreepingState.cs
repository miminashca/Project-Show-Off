using UnityEngine;

public class CreepingState : State
{
    private LadyStateMachine SM;
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

        SM.GazeSystem.PlayerCaughtSightOfLady -= TriggerTargetVisible;
        SM.GazeSystem.PlayerCaughtSightOfLady += TriggerTargetVisible;
        
        // Req 3.1.1: Trigger player feedback
        SM.FeedbackController.StartCreepingEffects();

        // Req 3.1.1: Play spatialized weeping/humming (FMOD template)
        // This sound should be attached to the White Lady's GameObject in the Unity Editor
        // and configured for 3D spatialization.
        // FMODUnity.RuntimeManager.PlayOneShot(SM.Config.creepingAudioEvent, SM.transform.position);
    }

    public override void Handle()
    {
        // Req 3.1.3: Transition to SEEN
        _gazeBuildupTimer += Time.deltaTime;

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
        //Debug.LogError("Lady visible: " + visible);
        _gazeBuildupTimer = 0f;
        targetCurrentlyVisible = visible;
    }
}