using UnityEngine;

public class HunterRoamingState : State
{
    private HunterAI _hunterAI;
    private HunterStateMachine _hunterSM;
    private float _superpositionCheckTimer;
    private const float SUPERPOSITION_CHECK_INTERVAL = 5.0f;

    public HunterRoamingState(StateMachine stateMachine) : base(stateMachine)
    {
        _hunterSM = stateMachine as HunterStateMachine;
        _hunterAI = _hunterSM.HunterAI;
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

    public override void Handle()
    {
        if (_hunterAI == null) return;

        if (_hunterAI.IsPlayerVisible)
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

        _superpositionCheckTimer -= Time.deltaTime;
        if (_superpositionCheckTimer <= 0f)
        {
            _superpositionCheckTimer = SUPERPOSITION_CHECK_INTERVAL;
            if (_hunterAI.PlayerTransform != null && Vector3.Distance(_hunterAI.transform.position, _hunterAI.PlayerTransform.position) > _hunterAI.MaxSuperpositionDistance)
            {
                AttemptSuperposition();
            }
        }

        if (!_hunterAI.NavAgent.pathPending && _hunterAI.NavAgent.remainingDistance < _hunterAI.NavAgent.stoppingDistance + 0.1f)
        {
            // Add a small delay before picking next node to avoid instant turning if nodes are close
            // For now, immediate:
            SetNewRoamDestination();
        }
    }

    private void AttemptSuperposition()
    {
        Transform superpositionNode = _hunterAI.GetSuperpositionNode();
        if (superpositionNode != null)
        {
            Debug.Log($"{_hunterAI.gameObject.name}: Superpositioning to {superpositionNode.name}!");
            // Ensure agent is on navmesh before warping, or can be placed on it.
            if (_hunterAI.NavAgent.Warp(superpositionNode.position))
            {
                _hunterAI.transform.position = superpositionNode.position; // Redundant if Warp successful, but good for visual sync
                SetNewRoamDestination(); // Get a new roam target from the new position
            }
            else
            {
                Debug.LogWarning($"{_hunterAI.gameObject.name}: Superposition Warp failed to {superpositionNode.position}. Node might be off-mesh.");
            }
        }
    }

    private void SetNewRoamDestination()
    {
        if (_hunterAI == null) return;
        Transform targetNode = _hunterAI.GetConfiguredRoamNode(); // Use renamed method
        if (targetNode != null)
        {
            _hunterAI.CurrentTargetNode = targetNode;
            if (_hunterAI.NavAgent.isOnNavMesh)
            {
                _hunterAI.NavAgent.SetDestination(_hunterAI.CurrentTargetNode.position);
            }
            else
            {
                Debug.LogWarning($"{_hunterAI.gameObject.name} is not on a NavMesh. Cannot set roam destination.", _hunterAI);
            }
        }
        else
        {
            // Debug.LogWarning("RoamingState: Could not find a new node to roam to. Hunter will idle.");
            // Consider what to do if no node: hunter idles, or tries again after a delay.
            // For now, it will just stop if GetConfiguredRoamNode returns null.
            _hunterAI.HunterAnimator.SetBool("IsMoving", false);
        }
    }

    public override void OnExitState()
    {
        if (_hunterAI == null) return;
    }
}