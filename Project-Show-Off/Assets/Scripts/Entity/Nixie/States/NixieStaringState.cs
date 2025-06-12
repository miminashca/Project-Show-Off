using UnityEngine;

public class NixieStaringState : State
{
    private NixieStateMachine nixieSM;
    private NixieAI nixieAI;
    private NixieNavigation nixieNav;
    private float lureTimer;

    public NixieStaringState(StateMachine pSM) : base(pSM)
    {
        nixieSM = (NixieStateMachine)SM;
        nixieAI = nixieSM.NixieAI;
        nixieNav = nixieSM.NixieNav;
    }

    public override void OnEnterState()
    {
        Debug.Log("Nixie entering STARING state.");
        nixieNav.StopMoving();
        nixieNav.SetPeeking(true);
        ResetLureTimer();
    }

    public override void Handle()
    {
        // --- TRANSITION CHECKS ---
        if (nixieAI.IsPlayerInWater && nixieAI.DistanceToPlayer <= nixieAI.CurrentDetectionRadius)
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

        lureTimer -= Time.deltaTime;
        if (lureTimer <= 0)
        {
            nixieAI.PlayLuringSound();
            ResetLureTimer();
        }
    }

    // Added OnExitState to match the abstract base class contract.
    public override void OnExitState()
    {
        // No specific exit logic needed, but good practice to have the method override.
    }

    private void ResetLureTimer()
    {
        lureTimer = Random.Range(4f, 9f); // Lure sounds are more frequent when staring
    }
}