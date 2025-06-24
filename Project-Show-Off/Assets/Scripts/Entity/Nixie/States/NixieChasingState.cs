using UnityEngine;

public class NixieChasingState : State
{
    private NixieStateMachine nixieSM;
    private NixieAI nixieAI;
    private NixieNavigation nixieNav;

    // A flag to ensure MoveTo is only called once per target update, preventing path recalculation every frame.
    private bool isPathSet = false;
    private float repathTimer;
    private const float REPATH_INTERVAL = 0.5f; // Recalculate path to player every half second.

    public NixieChasingState(StateMachine pSM) : base(pSM)
    {
        nixieSM = (NixieStateMachine)SM;
        nixieAI = nixieSM.NixieAI;
        nixieNav = nixieSM.NixieNav;
    }

    public override void OnEnterState()
    {
        Debug.Log("Nixie entering CHASING state.");
        nixieNav.SetPeeking(true); // Head is slightly above water while chasing
        isPathSet = false; // Force a path calculation on first frame.
        repathTimer = 0;
    }

    public override void Handle()
    {
        if (!nixieAI.IsPlayerInMyZone)
        {
            if (nixieAI.DistanceToPlayer <= nixieAI.StaringRadius)
            {
                SM.TransitToState(nixieSM.StaringState);
            }
            else // Player is out of water AND out of staring range
            {
                SM.TransitToState(nixieSM.RoamingState);
            }
            return;
        }

        if (!nixieAI.PlayerStatus.IsLanternOn && nixieAI.DistanceToPlayer > nixieAI.PointBlankRadius)
        {
            Debug.Log("Player turned off lantern, Nixie is now lurking.");
            nixieAI.PlayerLastKnownPosition = nixieAI.PlayerTransform.position;
            SM.TransitToState(nixieSM.LurkingState);
            return;
        }

        if (nixieAI.DistanceToPlayer <= nixieAI.AttackRange)
        {
            SM.TransitToState(nixieSM.HurtingState);
            return;
        }

        // --- BEHAVIOR LOGIC ---
        repathTimer -= Time.deltaTime;

        // Only calculate a new path if the timer is up, to save performance.
        if (repathTimer <= 0)
        {
            repathTimer = REPATH_INTERVAL;
            // The magic happens here: We request the Wavy movement style.
            nixieNav.MoveTo(nixieAI.PlayerTransform.position, nixieNav.ChasingSpeed, NixieNavigation.MoveStyle.Wavy);
        }

        // We still look directly at the player for a more focused, predatory feel, even while swaying.
        nixieNav.LookAt(nixieAI.PlayerTransform.position);
    }

    public override void OnExitState()
    {
        nixieNav.StopMoving();
    }
}