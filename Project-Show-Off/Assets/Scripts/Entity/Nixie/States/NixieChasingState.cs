using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class NixieChasingState : State
{
    private NixieStateMachine nixieSM;
    private NixieAI nixieAI;
    private NixieNavigation nixieNav;
    //NEW FMOD CHANGE
    private NixieSoundController nixieSoundController;

    // --- Strafing Logic Variables ---
    private float strafeTimer;
    private const float STRAFE_INTERVAL = 3.0f;
    private int strafeDirection = 1;

    // --- Attack Run Threshold ---
    private const float ATTACK_RUN_DISTANCE = 4f;

    // --- NEW: Gizmo Variable ---
    private Vector3 lastCalculatedTarget;

    public NixieChasingState(StateMachine pSM) : base(pSM)
    {
        nixieSM = (NixieStateMachine)SM;
        nixieAI = nixieSM.NixieAI;
        nixieNav = nixieSM.NixieNav;
    }

    public override void OnEnterState()
    {
        Debug.Log("Nixie entering CHASING state.");

        // NEW FMOD CHANGE
        nixieSoundController = nixieAI.SoundController;
        // END FMOD CHANGE


        nixieNav.SetPeeking(true);
        NixieEventBus.NotifyChaseStart();

        // --- NEW: Initialize strafing ---
        strafeTimer = STRAFE_INTERVAL;
        // Randomize initial direction
        strafeTimer = Random.Range(0.5f, STRAFE_INTERVAL); // Randomize first interval
        strafeDirection = (Random.value > 0.5f) ? 1 : -1;

        lastCalculatedTarget = nixieAI.transform.position;

        // NEW FMOD CHANGE
        if (nixieSoundController != null)
        {
            nixieSoundController.PlayChaseGrunt();
        }
        // END FMOD CHANGE
    }

    public override void Handle()
    {
        // This logic remains the same
        if (!nixieAI.IsPlayerInMyZone)
        {
            if (nixieAI.DistanceToPlayer <= nixieAI.StaringRadius)
            {
                SM.TransitToState(nixieSM.StaringState);
            }
            else
            {
                SM.TransitToState(nixieSM.RoamingState);
            }
            return;
        }

        if (!nixieAI.PlayerStatus.IsLanternOn && nixieAI.DistanceToPlayer > nixieAI.PointBlankRadius)
        {
            nixieAI.PlayerLastKnownPosition = nixieAI.PlayerTransform.position;
            SM.TransitToState(nixieSM.LurkingState);
            return;
        }

        if (nixieAI.DistanceToPlayer <= nixieAI.AttackRange)
        {
            SM.TransitToState(nixieSM.HurtingState);
            return;
        }

        // Always look at the player, this is menacing and constant.
        nixieNav.LookAt(nixieAI.PlayerTransform.position);

        if (nixieAI.DistanceToPlayer < ATTACK_RUN_DISTANCE)
        {
            Vector3 targetPosition = nixieAI.PlayerTransform.position;
            nixieNav.MoveTo(targetPosition, nixieNav.ChasingSpeed);

            lastCalculatedTarget = targetPosition; // Update for gizmo
        }
        else
        {
            // --- STRAFING ---
            // We are at a safe distance to circle.
            strafeTimer -= Time.deltaTime;
            if (strafeTimer <= 0)
            {
                strafeDirection *= -1;
                strafeTimer = STRAFE_INTERVAL;
            }

            Vector3 playerPos = nixieAI.PlayerTransform.position;
            Vector3 nixiePos = nixieAI.transform.position;
            Vector3 toPlayer = (playerPos - nixiePos).normalized;
            Vector3 strafeVector = Vector3.Cross(toPlayer, Vector3.up).normalized * strafeDirection;

            // Target a point that is a mix of forward and sideways movement.
            // This creates the circling ("orbiting") motion.
            Vector3 targetPosition = playerPos - (toPlayer * 2f) + (strafeVector * 4f);

            nixieNav.MoveTo(targetPosition, nixieNav.ChasingSpeed);

            lastCalculatedTarget = targetPosition; // Update for gizmo
        }

    }

    public override void OnExitState()
    {
        nixieNav.StopMoving();

        NixieEventBus.NotifyChaseEnd();
    }

    public override void DrawGizmos()
    {
        if (nixieAI.DistanceToPlayer < ATTACK_RUN_DISTANCE)
        {
            // In Attack Run mode
            Gizmos.color = Color.red;
            Gizmos.DrawLine(nixieAI.transform.position, lastCalculatedTarget);
            Gizmos.DrawWireSphere(lastCalculatedTarget, 1f);
#if UNITY_EDITOR
            Handles.Label(lastCalculatedTarget + Vector3.up, "ATTACK RUN TARGET");
#endif
        }
        else
        {
            // In Strafing mode
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(nixieAI.transform.position, lastCalculatedTarget);
            Gizmos.DrawWireSphere(lastCalculatedTarget, 1.5f);
#if UNITY_EDITOR
            Handles.Label(lastCalculatedTarget + Vector3.up, "ORBIT TARGET");
#endif
        }
    }
}