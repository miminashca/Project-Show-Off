using UnityEngine;

public class DissipatedState : State
{
    private LadyStateMachine SM;

    public DissipatedState(LadyStateMachine pSM) : base(pSM)
    {
        SM = pSM;
    }

    public override void OnEnterState()
    {
        Debug.Log("Entering DISSIPATED State");

        // Req 3.3.1: Reverse all player feedback effects
        SM.FeedbackController.StopAllEffects();

        // Req 3.3.1: Play dissipation VFX (placeholder)
        // e.g., Instantiate(dissipationVFX, SM.transform.position, Quaternion.identity);
        Debug.Log("Playing dissipation VFX...");

        // Req 3.3.1: Disable renderer and collider
        SM.AiRenderer.enabled = false;
        SM.AiCollider.enabled = false;
        
        // Req 3.3.2 & 3.3.3: Trigger the de-spawn sequence
        SM.DeSpawn();
    }

    public override void Handle()
    {
        // Logic is handled by the DeSpawn coroutine in the AIController,
        // so this state does nothing in its Handle loop.
    }

    public override void OnExitState()
    {
        Debug.Log("Exiting DISSIPATED State (Object is being destroyed)");
        // Cleanup is handled by AIController's OnDestroy method.
    }
}