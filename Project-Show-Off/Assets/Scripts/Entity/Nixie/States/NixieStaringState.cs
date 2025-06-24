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
        nixieNav.SetPeeking(true); // Peeks to stare at the player
        ResetLureTimer();
    }

    public override void Handle()
    {
        bool isPointBlank = nixieAI.DistanceToPlayer <= nixieAI.PointBlankRadius;

        // 1. HIGHEST PRIORITY: Check if player enters the water zone. If so, CHASE.
        if (nixieAI.IsPlayerInMyZone && (nixieAI.DistanceToPlayer <= nixieAI.CurrentDetectionRadius || isPointBlank))
        {
            SM.TransitToState(nixieSM.ChasingState);
            return;
        }
        // 2. SECOND PRIORITY: Check for conditions to stop staring and ROAM.
        // This happens if the player turns off the lantern OR moves too far away.
        else if (!nixieAI.PlayerStatus.IsLanternOn || nixieAI.DistanceToPlayer > nixieAI.StaringRadius)
        {
            SM.TransitToState(nixieSM.RoamingState);
            return;
        }

        // 3. DEFAULT BEHAVIOR: Continue Staring
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