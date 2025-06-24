using UnityEngine;

public class NixieRoamingState : State
{
    // Get references by casting the generic StateMachine
    private NixieStateMachine nixieSM;
    private NixieAI nixieAI;
    private NixieNavigation nixieNav;

    private Transform currentPatrolTarget;
    private float lureTimer;

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
        nixieNav.SetPeeking(false); // Hide while roaming
        currentPatrolTarget = nixieNav.GetNextPatrolNode();
        if (currentPatrolTarget != null)
        {
            // Use the straight movement style for patrolling
            nixieNav.MoveTo(currentPatrolTarget.position, nixieNav.RoamingSpeed, NixieNavigation.MoveStyle.Straight);
        }
        ResetLureTimer();
    }

    public override void Handle()
    {
        bool isLanternOn = nixieAI.PlayerStatus.IsLanternOn;
        bool isPointBlank = nixieAI.DistanceToPlayer <= nixieAI.PointBlankRadius;

        // 1. HIGHEST PRIORITY: Check for conditions to CHASE
        // Player must be IN the water zone. They are detected if the lantern is on OR they are point-blank.
        if (nixieAI.IsPlayerInMyZone && (isLanternOn || isPointBlank))
        {
            // The point-blank check is an unconditional chase trigger.
            // Otherwise, they must be within the current detection radius.
            if (isPointBlank || nixieAI.DistanceToPlayer <= nixieAI.CurrentDetectionRadius)
            {
                SM.TransitToState(nixieSM.ChasingState);
                return;
            }
        }
        // 2. SECOND PRIORITY: Check for conditions to STARE
        // Player must be OUT of the water zone, have the lantern ON, and be within StaringRadius.
        else if (!nixieAI.IsPlayerInMyZone && isLanternOn && nixieAI.DistanceToPlayer <= nixieAI.StaringRadius)
        {
            SM.TransitToState(nixieSM.StaringState);
            return;
        }

        // 3. DEFAULT BEHAVIOR: Continue Roaming
        if (currentPatrolTarget != null && Vector3.Distance(nixieAI.transform.position, currentPatrolTarget.position) < 1.5f)
        {
            currentPatrolTarget = nixieNav.GetNextPatrolNode();
            if (currentPatrolTarget != null)
            {
                nixieNav.MoveTo(currentPatrolTarget.position, nixieNav.RoamingSpeed, NixieNavigation.MoveStyle.Straight);
            }
        }

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
    }

    private void ResetLureTimer()
    {
        lureTimer = Random.Range(5f, 12f);
    }
}