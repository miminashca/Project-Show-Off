using UnityEngine;

public class ThimbleHunterRoamingState : State // Inherits from your abstract State
{
    private ThimbleHunterAI _hunterAI; // Cached reference for convenience
    private ThimbleHunterStateMachine _hunterSM; // Cached specific state machine

    private float _superpositionCheckTimer;
    private const float SUPERPOSITION_CHECK_INTERVAL = 5.0f;

    // Constructor takes the StateMachine instance (as per your base State class)
    public ThimbleHunterRoamingState(StateMachine stateMachine) : base(stateMachine)
    {
        // Cast SM to the specific ThimbleHunterStateMachine to access HunterAI
        _hunterSM = stateMachine as ThimbleHunterStateMachine;
        if (_hunterSM == null)
        {
            Debug.LogError("ThimbleHunterRoamingState was given a StateMachine that is not a ThimbleHunterStateMachine!", stateMachine);
            return;
        }
        _hunterAI = _hunterSM.HunterAI; // Get the AI brain reference
    }

    public override void OnEnterState()
    {
        if (_hunterAI == null) return; // Safety check if constructor failed

        Debug.Log($"{_hunterAI.gameObject.name} entering ROAMING state.");
        _hunterAI.NavAgent.speed = _hunterAI.MovementSpeedRoaming;
        _hunterAI.NavAgent.isStopped = false;
        _hunterAI.HunterAnimator.SetBool("IsMoving", true);

        _superpositionCheckTimer = SUPERPOSITION_CHECK_INTERVAL;
        SetNewRoamDestination();
    }

    public override void Handle() // This is your state's update logic
    {
        if (_hunterAI == null) return;

        // --- Transition Checks (Priority Order) ---
        if (_hunterAI.IsPlayerVisible)
        {
            SM.TransitToState(_hunterSM.ChasingState); // Use specific state machine to get state instance
            return;
        }
        if (_hunterAI.CanHearPlayerAlert)
        {
            SM.TransitToState(_hunterSM.InvestigatingState);
            return;
        }

        // --- Superposition Logic ---
        _superpositionCheckTimer -= Time.deltaTime;
        if (_superpositionCheckTimer <= 0f)
        {
            _superpositionCheckTimer = SUPERPOSITION_CHECK_INTERVAL;
            if (_hunterAI.PlayerTransform != null && Vector3.Distance(_hunterAI.transform.position, _hunterAI.PlayerTransform.position) > _hunterAI.MaxSuperpositionDistance)
            {
                Transform superpositionNode = _hunterAI.GetSuperpositionNode();
                if (superpositionNode != null)
                {
                    Debug.Log($"{_hunterAI.gameObject.name}: Superpositioning!");
                    _hunterAI.NavAgent.Warp(superpositionNode.position);
                    _hunterAI.transform.position = superpositionNode.position;
                    SetNewRoamDestination();
                }
            }
        }

        // --- Roaming Movement Logic ---
        if (!_hunterAI.NavAgent.pathPending && _hunterAI.NavAgent.remainingDistance < _hunterAI.NavAgent.stoppingDistance + 0.1f)
        {
            SetNewRoamDestination();
        }
    }

    public override void OnExitState()
    {
        if (_hunterAI == null) return;
        Debug.Log($"{_hunterAI.gameObject.name} exiting ROAMING state.");
        // Cleanup logic specific to exiting roaming, if any.
        // Often, the next state's OnEnterState will handle setting new parameters.
    }

    private void SetNewRoamDestination()
    {
        if (_hunterAI == null) return;
        Transform targetNode = _hunterAI.GetRandomNodeFromGraph();
        if (targetNode != null)
        {
            _hunterAI.CurrentTargetNode = targetNode;
            if (_hunterAI.NavAgent.isOnNavMesh) // Check if agent is on a navmesh
            {
                _hunterAI.NavAgent.SetDestination(_hunterAI.CurrentTargetNode.position);
            }
            else
            {
                Debug.LogWarning($"{_hunterAI.gameObject.name} is not on a NavMesh. Cannot set destination.", _hunterAI);
            }
        }
        else
        {
            // Debug.LogWarning("RoamingState: Could not find a new node to roam to.");
        }
    }
}