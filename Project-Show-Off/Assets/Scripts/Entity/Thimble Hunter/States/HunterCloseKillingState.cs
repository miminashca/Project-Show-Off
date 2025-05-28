using UnityEngine;

public class HunterCloseKillingState : State
{
    private HunterAI _hunterAI;
    private HunterStateMachine _hunterSM;
    private float _killAnimationDuration = 2.0f; // Example, adjust based on animation
    private float _timer;

    public HunterCloseKillingState(StateMachine stateMachine) : base(stateMachine)
    {
        _hunterSM = stateMachine as HunterStateMachine;
        _hunterAI = _hunterSM.HunterAI;
    }

    public override void OnEnterState()
    {
        if (_hunterAI == null) return;
        Debug.Log($"{_hunterAI.gameObject.name} entering CLOSE_KILLING state.");

        _hunterAI.NavAgent.isStopped = true;
        _hunterAI.NavAgent.velocity = Vector3.zero;
        _hunterAI.HunterAnimator.SetTrigger("MeleeKill"); // Animation for melee kill

        // Optional: Orient towards player instantly
        if (_hunterAI.PlayerTransform != null)
        {
            Vector3 directionToPlayer = (_hunterAI.PlayerTransform.position - _hunterAI.transform.position).normalized;
            if (directionToPlayer != Vector3.zero)
                _hunterAI.transform.rotation = Quaternion.LookRotation(directionToPlayer);
        }

        // TODO: Trigger player's death sequence / game over
        // This might involve an event or direct call to a GameManager or PlayerHealth component.
        PlayerHealth playerHealth = _hunterAI.PlayerTransform?.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(9999); // Insta-kill
        }
        Debug.LogWarning($"{_hunterAI.gameObject.name} executed MELEE KILL on player!");

        _timer = _killAnimationDuration; // To allow animation to play out
    }

    public override void Handle()
    {
        // Hunter might be stuck in this state if game doesn't end/reload
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            // After animation, what happens? Usually game over screen is shown by now.
            // If not, hunter might go idle or despawn.
            // For now, it just stays, assuming game handles the player death outcome.
            // Debug.Log("CloseKill animation timer ended.");
        }
    }

    public override void OnExitState()
    {
        // This state typically isn't exited cleanly if it results in game over.
        // If it could be, reset any relevant hunter parameters here.
        // Debug.Log($"{_hunterAI.gameObject.name} exiting CLOSE_KILLING state (unlikely).");
    }
}