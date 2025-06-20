using UnityEngine;

public class HunterRoamingState : State
{
    private HunterAI _hunterAI;
    private HunterStateMachine _hunterSM;

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
        _hunterAI.HunterAnimator.SetBool("IsMoving", true);

        SetNewRoamDestination();
    }

    public override void Handle()
    {
        if (_hunterAI == null) return;

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

        // --- Superposition Logic ---
        // Check if cooldown is over AND distance condition is met
        if (_hunterAI.CurrentSuperpositionCooldownTimer <= 0f && _hunterAI.PlayerTransform != null)
        {
            bool isTooFar = Vector3.Distance(_hunterAI.transform.position, _hunterAI.PlayerTransform.position) > _hunterAI.MaxSuperpositionDistance;

            // --- MODIFIED LOGIC ---
            // The Hunter will only attempt superposition IF it's too far AND the player is NOT looking at it.
            if (isTooFar && !_hunterAI.IsVisibleToPlayer())
            {
                AttemptSuperposition();
                _hunterAI.CurrentSuperpositionCooldownTimer = _hunterAI.SuperpositionAttemptCooldown; // Reset cooldown
            }
        }


        if (!_hunterAI.NavAgent.pathPending && _hunterAI.NavAgent.remainingDistance < _hunterAI.NavAgent.stoppingDistance + 0.1f)
        {
            SetNewRoamDestination();
        }
    }

    private void AttemptSuperposition()
    {
        // This part remains the same. It calls your already-correct GetSuperpositionNode method.
        Transform superpositionNode = _hunterAI.GetSuperpositionNode();
        if (superpositionNode != null)
        {
            Debug.Log($"{_hunterAI.gameObject.name}: Superpositioning to {superpositionNode.name} because player is far and not looking.");
            if (_hunterAI.NavAgent.Warp(superpositionNode.position))
            {
                SetNewRoamDestination();
            }
            else
            {
                Debug.LogWarning($"{_hunterAI.gameObject.name}: Superposition Warp failed to {superpositionNode.position}. Node might be off-mesh.");
            }
        }
        else
        {
            Debug.LogWarning($"{_hunterAI.gameObject.name}: Failed to find a suitable superposition node.");
        }
    }

    private void SetNewRoamDestination()
    {
        if (_hunterAI == null) return;
        Transform targetNode = _hunterAI.GetConfiguredRoamNode();
        if (targetNode != null)
        {
            _hunterAI.CurrentTargetNode = targetNode;
            if (_hunterAI.NavAgent.isOnNavMesh)
            {
                _hunterAI.NavAgent.SetDestination(_hunterAI.CurrentTargetNode.position);
                if (!_hunterAI.HunterAnimator.GetBool("IsMoving"))
                    _hunterAI.HunterAnimator.SetBool("IsMoving", true);
            }
            else
            {
                Debug.LogWarning($"{_hunterAI.gameObject.name} is not on a NavMesh. Cannot set roam destination.", _hunterAI);
                _hunterAI.HunterAnimator.SetBool("IsMoving", false);
            }
        }
        else
        {
            _hunterAI.HunterAnimator.SetBool("IsMoving", false);
        }
    }

    public override void OnExitState()
    {
        if (_hunterAI == null) return;
    }
}