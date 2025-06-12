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

    // Renamed method
    public override void OnEnterState()
    {
        Debug.Log("Nixie entering ROAMING state.");
        nixieNav.SetPeeking(false);
        currentPatrolTarget = nixieNav.GetNextPatrolNode();
        if (currentPatrolTarget != null)
        {
            nixieNav.MoveTo(currentPatrolTarget.position, nixieNav.RoamingSpeed);
        }
        ResetLureTimer();
    }

    public override void Handle()
    {
        // --- TRANSITION CHECKS (using the new method name and casting) ---
        if (nixieAI.IsPlayerInWater && nixieAI.DistanceToPlayer <= nixieAI.CurrentDetectionRadius)
        {
            SM.TransitToState(nixieSM.ChasingState);
            return;
        }
        if (nixieAI.DistanceToPlayer <= nixieAI.StaringRadius)
        {
            SM.TransitToState(nixieSM.StaringState);
            return;
        }

        // --- BEHAVIOR LOGIC (no change here) ---
        if (currentPatrolTarget != null && Vector3.Distance(nixieAI.transform.position, currentPatrolTarget.position) < 1f)
        {
            currentPatrolTarget = nixieNav.GetNextPatrolNode();
            nixieNav.MoveTo(currentPatrolTarget.position, nixieNav.RoamingSpeed);
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