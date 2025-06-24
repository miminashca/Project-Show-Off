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
        // --- TRANSITION CHECKS (IN ORDER OF PRIORITY) ---

        // 1. HIGHEST PRIORITY: Player enters the water zone. This should always trigger a chase.
        // --- FIX --- Simplified this condition. If the Nixie is staring and the player enters its zone, it must chase.
        if (nixieAI.IsPlayerInMyZone)
        {
            SM.TransitToState(nixieSM.ChasingState);
            return;
        }

        // 2. SECOND PRIORITY: Check for conditions to stop staring and ROAM.
        // This happens if the player turns off the lantern OR moves too far away.
        if (!nixieAI.PlayerStatus.IsLanternOn || nixieAI.DistanceToPlayer > nixieAI.StaringRadius)
        {
            SM.TransitToState(nixieSM.RoamingState);
            return;
        }

        // --- DEFAULT BEHAVIOR: Continue Staring ---
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