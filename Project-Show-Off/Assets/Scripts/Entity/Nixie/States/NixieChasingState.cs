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
        // --- TRANSITION CHECKS (IN ORDER OF PRIORITY) ---

        // 1. Player leaves the Nixie's zone.
        if (!nixieAI.IsPlayerInMyZone)
        {
            // --- FIX --- Added check for PlayerStatus.IsLanternOn.
            if (nixieAI.PlayerStatus.IsLanternOn && nixieAI.DistanceToPlayer <= nixieAI.StaringRadius)
            {
                SM.TransitToState(nixieSM.StaringState);
            }
            else // Player is out of water AND (out of staring range OR lantern is off).
            {
                SM.TransitToState(nixieSM.RoamingState);
            }
            return;
        }

        // 2. Player turns off their lantern to hide.
        if (!nixieAI.PlayerStatus.IsLanternOn && nixieAI.DistanceToPlayer > nixieAI.PointBlankRadius)
        {
            Debug.Log("Player turned off lantern, Nixie is now lurking.");
            nixieAI.PlayerLastKnownPosition = nixieAI.PlayerTransform.position;
            SM.TransitToState(nixieSM.LurkingState);
            return;
        }

        // 3. Player is close enough to be attacked.
        if (nixieAI.DistanceToPlayer <= nixieAI.AttackRange)
        {
            SM.TransitToState(nixieSM.HurtingState);
            return;
        }

        // --- BEHAVIOR LOGIC ---
        repathTimer -= Time.deltaTime;

        if (repathTimer <= 0)
        {
            repathTimer = REPATH_INTERVAL;
            nixieNav.MoveTo(nixieAI.PlayerTransform.position, nixieNav.ChasingSpeed, NixieNavigation.MoveStyle.Wavy);
        }

        nixieNav.LookAt(nixieAI.PlayerTransform.position);
    }

    public override void OnExitState()
    {
        nixieNav.StopMoving();
    }
}