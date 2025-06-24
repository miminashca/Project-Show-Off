using UnityEngine;

public class NixieRoamingState : State
{
    // Get references by casting the generic StateMachine
    private NixieStateMachine nixieSM;
    private NixieAI nixieAI;
    private NixieNavigation nixieNav;

    private Transform currentPatrolTarget;
    private float lureTimer;
    private float tensionTimer;

    // The constructor now only takes the StateMachine
    public NixieRoamingState(StateMachine pSM) : base(pSM)
    {
        nixieSM = (NixieStateMachine)SM;
        nixieAI = nixieSM.NixieAI;
        nixieNav = nixieSM.NixieNav;
    }

    public override void OnEnterState()
    {
        //Debug.Log("Nixie entering ROAMING state.");
        nixieNav.SetPeeking(false);
        tensionTimer = 0f;
        currentPatrolTarget = nixieNav.GetNextPatrolNode();
        if (currentPatrolTarget != null)
        {
            nixieNav.MoveTo(currentPatrolTarget.position, nixieNav.RoamingSpeed, NixieNavigation.MoveStyle.Straight);
        }
        ResetLureTimer();
    }

    public override void Handle()
    {
        // --- TRANSITION CHECKS (IN ORDER OF PRIORITY) ---

        // 1. CHASE: Player is in the zone and clearly visible.
        if (nixieAI.IsPlayerInMyZone)
        {
            bool isLanternOn = nixieAI.PlayerStatus.IsLanternOn;
            bool isPointBlank = nixieAI.DistanceToPlayer <= nixieAI.PointBlankRadius;

            if (isPointBlank || (isLanternOn && nixieAI.DistanceToPlayer <= nixieAI.DetectionRadiusLantern))
            {
                SM.TransitToState(nixieSM.ChasingState);
                return;
            }
        }

        // 2. STARE: Player is OUT of the zone but visible with lantern.
        else if (!nixieAI.IsPlayerInMyZone && nixieAI.PlayerStatus.IsLanternOn && nixieAI.DistanceToPlayer <= nixieAI.StaringRadius)
        {
            SM.TransitToState(nixieSM.StaringState);
            return;
        }

        // --- BEHAVIOR LOGIC (If no transition occurs) ---

        // 3a. Tension Timer: Build tension if player is hiding in the zone.
        bool canBuildTension = nixieAI.IsPlayerInMyZone && !nixieAI.PlayerStatus.IsLanternOn;
        if (canBuildTension)
        {
            tensionTimer += Time.deltaTime;
            if (tensionTimer >= nixieAI.MaxTensionDuration)
            {
                Debug.Log("Tension timer expired! Nixie has found the player.");
                SM.TransitToState(nixieSM.ChasingState); // --- FIX --- This transition is now handled here.
                return;
            }
        }
        else
        {
            tensionTimer = 0f; // Reset if conditions aren't met.
        }

        // 3b. Patrolling Behavior
        if (currentPatrolTarget != null && Vector3.Distance(nixieAI.transform.position, currentPatrolTarget.position) < 1.5f)
        {
            currentPatrolTarget = nixieNav.GetNextPatrolNode();
            if (currentPatrolTarget != null)
            {
                nixieNav.MoveTo(currentPatrolTarget.position, nixieNav.RoamingSpeed, NixieNavigation.MoveStyle.Straight);
            }
        }

        // 3c. Luring Sounds
        lureTimer -= Time.deltaTime;
        if (lureTimer <= 0)
        {
            nixieAI.PlayLuringSound();
            ResetLureTimer();
        }
    }

    // Renamed method
    public override void OnExitState()
    {
        nixieNav.StopMoving();
        tensionTimer = 0f;
    }

    private void ResetLureTimer()
    {
        lureTimer = Random.Range(5f, 12f);
    }
}