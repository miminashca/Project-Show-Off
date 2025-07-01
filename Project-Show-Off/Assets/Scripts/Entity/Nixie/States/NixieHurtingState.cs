using UnityEngine;

public class NixieHurtingState : State
{
    // References fetched from the State Machine
    private NixieStateMachine nixieSM;
    private NixieAI nixieAI;

    // NEW FMOD CHANGE
    private NixieSoundController nixieSoundController;
    // END FMOD CHANGE

    // Updated constructor
    public NixieHurtingState(StateMachine pSM) : base(pSM)
    {
        nixieSM = (NixieStateMachine)SM;
        nixieAI = nixieSM.NixieAI;
    }

    public override void OnEnterState()
    {
        Debug.Log("Nixie entering HURTING state.");
        //nixieAI.PlayAttackSound();

        // NEW FMOD CHANGE
        nixieSoundController = nixieAI.SoundController;
        // END FMOD CHANGE

        // --- NEW: INSTA-KILL LOGIC ---
        // Find the PlayerHealth component on the player object.
        PlayerHealth playerHealth = nixieAI.PlayerStatus?.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            Debug.Log("Nixie attacks and kills the player!");
            playerHealth.Die(); // This handles disabling movement and firing the OnPlayerDied event.
        }
        else
        {
            Debug.LogWarning("Nixie attacked, but couldn't find a PlayerHealth component to kill!");
        }

        // Transition to the stunned state after the attack. The player is already "dead",
        // but this completes the Nixie's state loop gracefully.
        SM.TransitToState(nixieSM.StuntedState);

        // NEW FMOD CHANGE
        if (nixieSoundController != null)
        {
            nixieSoundController.PlayHurtGrunt();
        }
        // END FMOD CHANGE
    }

    // This state has no ongoing logic, so Handle is empty.
    public override void Handle()
    {
    }

    // This state has no exit logic, as the transition happens instantly.
    public override void OnExitState()
    {
    }
}