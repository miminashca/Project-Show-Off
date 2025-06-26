using UnityEngine;

public class NixieStaringState : State
{
    private NixieStateMachine nixieSM;
    private NixieAI nixieAI;
    private NixieNavigation nixieNav;
    private NixieSoundController nixieSoundController; // --- ADDED: Reference to the sound controller

    // --- REMOVED: This timer logic is now handled by the FMOD controller's coroutine
    // private float lureTimer; 

    public NixieStaringState(StateMachine pSM) : base(pSM)
    {
        nixieSM = (NixieStateMachine)SM;
        nixieAI = nixieSM.NixieAI;
        nixieNav = nixieSM.NixieNav;
        nixieSoundController = nixieAI.SoundController; // --- ADDED: Get the sound controller from the AI
    }

    public override void OnEnterState()
    {
        Debug.Log("Nixie entering STARING state.");
        nixieNav.StopMoving();
        nixieNav.SetPeeking(true);

        // --- ADDED: Start the provocative sound loop when entering this state
        if (nixieSoundController != null)
        {
            nixieSoundController.StartProvocativeLoop();
        }

        // --- REMOVED: Old timer logic
        // ResetLureTimer(); 
    }

    public override void Handle()
    {
        // If player turns lantern off, Nixie loses interest and roams.
        if (!nixieAI.PlayerStatus.IsLanternOn)
        {
            SM.TransitToState(nixieSM.RoamingState);
            return;
        }

        bool isPointBlank = nixieAI.DistanceToPlayer <= nixieAI.PointBlankRadius;

        if (nixieAI.IsPlayerInMyZone && (nixieAI.DistanceToPlayer <= nixieAI.CurrentDetectionRadius || isPointBlank))
        {
            SM.TransitToState(nixieSM.ChasingState);
            return;
        }
        if (nixieAI.DistanceToPlayer > nixieAI.StaringRadius)
        {
            SM.TransitToState(nixieSM.RoamingState);
            return;
        }

        // --- BEHAVIOR LOGIC ---
        nixieNav.LookAt(nixieAI.PlayerTransform.position);

        // --- REMOVED: All old timer and sound playing logic is gone from Handle()
        // lureTimer -= Time.deltaTime;
        // if (lureTimer <= 0)
        // {
        //     nixieAI.PlayLuringSound();
        //     ResetLureTimer();
        // }
    }

    public override void OnExitState()
    {
        // --- ADDED: It is CRITICAL to stop the sound loop when we leave this state.
        if (nixieSoundController != null)
        {
            nixieSoundController.StopProvocativeLoop();
        }
    }

    // --- REMOVED: This method is no longer needed
    // private void ResetLureTimer()
    // {
    //     lureTimer = Random.Range(4f, 9f);
    // }
}