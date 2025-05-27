using UnityEngine;

public class HunterInvestigatingState : State
{
    private HunterAI _hunterAI;
    private HunterStateMachine _hunterSM;

    private float _currentInvestigationTime;
    private Vector3 _investigationTargetPosition;

    public HunterInvestigatingState(StateMachine stateMachine) : base(stateMachine)
    {
        _hunterSM = stateMachine as HunterStateMachine;
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

        if (_hunterAI.IsPlayerVisible)
        {
            Debug.Log($"{_hunterAI.gameObject.name} (Investigating): Player visible! Transitioning to Chase.");
            SM.TransitToState(_hunterSM.ChasingState);
            return;
        }

        _currentInvestigationTime -= Time.deltaTime;
        _hunterAI.CurrentInvestigationTimer = _currentInvestigationTime;

        if (_hunterAI.CanHearPlayerAlert) // Check for new shout
        {
            _hunterAI.AcknowledgePlayerAlert(); // Consume the alert
            Debug.Log($"{_hunterAI.gameObject.name} (Investigating): Heard new alert. Resetting investigation. New LKP: {_hunterAI.LastKnownPlayerPosition}");
            _investigationTargetPosition = _hunterAI.LastKnownPlayerPosition;
            if (_hunterAI.NavAgent.isOnNavMesh)
            {
                _hunterAI.NavAgent.SetDestination(_investigationTargetPosition);
            }
            _currentInvestigationTime = _hunterAI.InvestigationDuration;

            if (!_hunterAI.HunterAnimator.GetBool("IsMoving")) // If was idle, start moving anim
            {
                _hunterAI.HunterAnimator.SetBool("IsMoving", true);
            }
        }

        if (!_hunterAI.NavAgent.pathPending && _hunterAI.NavAgent.remainingDistance < _hunterAI.NavAgent.stoppingDistance + 0.1f)
        {
            _hunterAI.HunterAnimator.SetBool("IsMoving", false);
            // TODO: Implement look around behavior
        }

        if (_currentInvestigationTime <= 0f)
        {
            Debug.Log($"{_hunterAI.gameObject.name} (Investigating): Investigation timer expired. Transitioning to Roam.");
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