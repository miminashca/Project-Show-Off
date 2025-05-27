using UnityEngine;

public class ThimbleHunterInvestigatingState : State
{
    private ThimbleHunterAI _hunterAI;
    private ThimbleHunterStateMachine _hunterSM;

    private float _currentInvestigationTime;
    private Vector3 _investigationTargetPosition;

    public ThimbleHunterInvestigatingState(StateMachine stateMachine) : base(stateMachine)
    {
        _hunterSM = stateMachine as ThimbleHunterStateMachine;
        if (_hunterSM == null)
        {
            Debug.LogError("ThimbleHunterInvestigatingState received an incompatible StateMachine!", stateMachine);
            return;
        }
        _hunterAI = _hunterSM.HunterAI;
    }

    public override void OnEnterState()
    {
        if (_hunterAI == null) return;
        Debug.Log($"{_hunterAI.gameObject.name} entering INVESTIGATING state.");

        _hunterAI.NavAgent.speed = _hunterAI.MovementSpeedInvestigating;
        _hunterAI.NavAgent.isStopped = false;
        _hunterAI.HunterAnimator.SetBool("IsMoving", true); // Or specific "IsInvestigating" animation

        _investigationTargetPosition = _hunterAI.LastKnownPlayerPosition;
        if (_hunterAI.NavAgent.isOnNavMesh)
        {
            _hunterAI.NavAgent.SetDestination(_investigationTargetPosition);
        }

        _currentInvestigationTime = _hunterAI.InvestigationDuration;
        _hunterAI.CurrentInvestigationTimer = _currentInvestigationTime; // Update AI's public timer if needed
    }

    public override void Handle()
    {
        if (_hunterAI == null) return;

        // --- Transition Checks (Priority Order) ---
        // 1. To CHASING: Player is visible
        if (_hunterAI.IsPlayerVisible)
        {
            SM.TransitToState(_hunterSM.ChasingState);
            return;
        }

        // --- Investigation Logic ---
        _currentInvestigationTime -= Time.deltaTime;
        _hunterAI.CurrentInvestigationTimer = _currentInvestigationTime;

        // If player makes another "Hey!" while investigating, reset LKP and timer
        if (_hunterAI.CanHearPlayerAlert)
        {
            Debug.Log($"{_hunterAI.gameObject.name} heard new alert while investigating. Resetting investigation.");
            _investigationTargetPosition = _hunterAI.LastKnownPlayerPosition; // LKP updated by AI component
            if (_hunterAI.NavAgent.isOnNavMesh)
            {
                _hunterAI.NavAgent.SetDestination(_investigationTargetPosition);
            }
            _currentInvestigationTime = _hunterAI.InvestigationDuration; // Reset timer
        }

        // Reached LKP, perform search routine (placeholder for now)
        if (!_hunterAI.NavAgent.pathPending && _hunterAI.NavAgent.remainingDistance < _hunterAI.NavAgent.stoppingDistance + 0.1f)
        {
            // TODO: Implement search routine (e.g., look around, move to sub-points)
            // For now, just wait out the timer.
            _hunterAI.HunterAnimator.SetBool("IsMoving", false); // Stop moving animation if at LKP
        }


        // --- Transition Checks (After main logic) ---
        // 2. To ROAMING: InvestigationTimer expires and Player not re-acquired
        if (_currentInvestigationTime <= 0f)
        {
            Debug.Log($"{_hunterAI.gameObject.name} investigation timer expired.");
            SM.TransitToState(_hunterSM.RoamingState);
            return;
        }
    }

    public override void OnExitState()
    {
        if (_hunterAI == null) return;
        Debug.Log($"{_hunterAI.gameObject.name} exiting INVESTIGATING state.");
        _hunterAI.CurrentInvestigationTimer = 0f; // Reset public timer
    }
}