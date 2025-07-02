using UnityEngine;

public class HunterRoamingState : State
{
    private HunterAI _hunterAI;
    private HunterStateMachine _hunterSM;

    // --- NEW: Timer for pausing at a destination ---
    private float _roamWaitTimer;
    private float _roamWaitDuration = 2.0f; // How long to wait at a node. Could be a random range.

    // --- NEW: A flag to know what the AI is doing within the roaming state ---
    private enum RoamSubState { Moving, Waiting }
    private RoamSubState _currentSubState;

    public HunterRoamingState(StateMachine stateMachine) : base(stateMachine)
    {
        _hunterSM = stateMachine as HunterStateMachine;
        _hunterAI = _hunterSM.HunterAI;
    }

    public override void OnEnterState()
    {
        if (_hunterAI == null) return;
        Debug.Log($"{_hunterAI.gameObject.name} entering ROAMING state.");

        _hunterAI.NavAgent.speed = _hunterAI.MovementSpeedRoaming;
        _hunterAI.NavAgent.isStopped = false;

        // Start by immediately finding a place to go.
        SetNewRoamDestination();

        if (_hunterAI.SoundController != null)
        {
            _hunterAI.SoundController.StartIdleGrunts();
        }
    }

    public override void Handle()
    {
        if (_hunterAI == null) return;

        // --- PRIORITY 1: State Transitions ---
        if (_hunterAI.IsPlayerFullySpotted)
        {
            SM.TransitToState(_hunterSM.ChasingState);
            return;
        }

        if (_hunterAI.CanHearPlayerAlert)
        {
            Debug.Log($"{_hunterAI.gameObject.name} (Roaming): Heard player alert. Transitioning to Investigate.");
            _hunterAI.AcknowledgePlayerAlert();
            SM.TransitToState(_hunterSM.InvestigatingState);
            return;
        }

        // --- PRIORITY 2: Superposition (Relocation) ---
        if (_hunterAI.CurrentSuperpositionCooldownTimer <= 0f && _hunterAI.PlayerTransform != null)
        {
            bool isTooFar = Vector3.Distance(_hunterAI.transform.position, _hunterAI.PlayerTransform.position) > _hunterAI.MaxSuperpositionDistance;
            if (isTooFar && !_hunterAI.IsVisibleToPlayer())
            {
                // Attempting to teleport is a major action, so we do it and finish the frame.
                AttemptSuperposition();
                return;
            }
        }

        // --- PRIORITY 3: Core Roaming Logic (Move & Wait) ---
        if (_currentSubState == RoamSubState.Moving)
        {
            // Check if we have arrived at our destination.
            // Using <= is slightly cleaner than < ... + 0.1f
            if (!_hunterAI.NavAgent.pathPending && _hunterAI.NavAgent.remainingDistance <= _hunterAI.NavAgent.stoppingDistance)
            {
                // We've arrived. Now, wait.
                StartWaiting();
            }
        }
        else if (_currentSubState == RoamSubState.Waiting)
        {
            // Count down the wait timer.
            _roamWaitTimer -= Time.deltaTime;
            if (_roamWaitTimer <= 0f)
            {
                // Wait is over. Find a new place to go.
                SetNewRoamDestination();
            }
        }
    }

    // --- REFINED: Superposition is now cleaner ---
    private void AttemptSuperposition()
    {
        Transform superpositionNode = _hunterAI.GetSuperpositionNode();
        if (superpositionNode != null)
        {
            Debug.Log($"{_hunterAI.gameObject.name}: Superpositioning to {superpositionNode.name}...");
            if (_hunterAI.NavAgent.Warp(superpositionNode.position))
            {
                // After a successful warp, we are in a new location.
                // Our next logical action is to find a path from here.
                SetNewRoamDestination();
            }
            else
            {
                Debug.LogWarning($"{_hunterAI.gameObject.name}: Superposition Warp FAILED to {superpositionNode.position}. Node might be off-mesh.");
            }
        }
        else
        {
            Debug.LogWarning($"{_hunterAI.gameObject.name}: Failed to find a suitable superposition node.");
        }

        // Reset the cooldown regardless of success to prevent spamming failed attempts.
        _hunterAI.CurrentSuperpositionCooldownTimer = _hunterAI.SuperpositionAttemptCooldown;
    }

    private void SetNewRoamDestination()
    {
        if (_hunterAI == null) return;
        Debug.Log("Finding new roam destination...");

        Transform targetNode = _hunterAI.GetConfiguredRoamNode();

        // --- ADDED: Prevent picking the same node we are already at ---
        // This is a safety net against the "NearestToPlayer" busy loop if wait time is zero.
        int attempts = 0;
        while (targetNode == _hunterAI.CurrentTargetNode && attempts < 10)
        {
            targetNode = _hunterAI.GetConfiguredRoamNode();
            attempts++;
        }

        if (targetNode != null)
        {
            _hunterAI.CurrentTargetNode = targetNode;
            if (_hunterAI.NavAgent.isOnNavMesh)
            {
                _hunterAI.NavAgent.SetDestination(_hunterAI.CurrentTargetNode.position);
                _hunterAI.HunterAnimator.SetFloat("MovementSpeed", _hunterAI.MovementSpeedRoaming);
                _currentSubState = RoamSubState.Moving; // We are now in the "Moving" sub-state.
            }
            else
            {
                Debug.LogWarning($"{_hunterAI.gameObject.name} is not on a NavMesh. Cannot set roam destination.", _hunterAI);
                StartWaiting(); // If we can't move, just wait.
            }
        }
        else
        {
            Debug.LogWarning("Could not find any valid roam node.");
            StartWaiting(); // If there's nowhere to go, just wait.
        }
    }

    // --- NEW: Helper method to handle starting the wait state ---
    private void StartWaiting()
    {
        Debug.Log("Arrived at destination. Waiting...");
        _currentSubState = RoamSubState.Waiting;
        _roamWaitTimer = Random.Range(1.5f, 3.5f); // Use a random wait time for more natural behavior
        _hunterAI.HunterAnimator.SetFloat("MovementSpeed", 0f);
        _hunterAI.CurrentTargetNode = null; // We no longer have an active target node
    }

    public override void OnExitState()
    {
        if (_hunterAI == null) return;

        if (_hunterAI.SoundController != null)
        {
            _hunterAI.SoundController.StopIdleGrunts();
        }
    }
}