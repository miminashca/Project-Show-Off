using UnityEngine;

public class HunterInvestigatingState : State
{
    private HunterAI _hunterAI;
    private HunterStateMachine _hunterSM;

    private float _currentInvestigationTime;
    private Vector3 _investigationTargetPosition;

    // For "Look Around" behavior
    private bool _isAtLKP = false;
    private float _lookAroundSubTimer = 0f;
    [SerializeField]
    public float LOOK_SWEEP_DURATION = 2.0f; // Time for one sweep (e.g., look left)
    [SerializeField]
    private float LOOK_PAUSE_DURATION = 1.0f; // Pause between sweeps
    private int _lookSweepsCompleted = 0;
    [SerializeField]
    private int MAX_LOOK_SWEEPS = 2; // e.g., look left, then look right
    private Quaternion _targetLookRotation;
    private enum LookAroundPhase { Sweeping, Pausing }
    private LookAroundPhase _currentLookPhase;

    public HunterInvestigatingState(StateMachine stateMachine) : base(stateMachine)
    {
        _hunterSM = stateMachine as HunterStateMachine;
        _hunterAI = _hunterSM.HunterAI;
    }

    public override void OnEnterState()
    {
        if (_hunterAI == null) return;
        Debug.Log($"{_hunterAI.gameObject.name} entering INVESTIGATING state (LKP: {_hunterAI.LastKnownPlayerPosition}).");

        _hunterAI.NavAgent.speed = _hunterAI.MovementSpeedInvestigating;
        _hunterAI.NavAgent.isStopped = false;
        _hunterAI.HunterAnimator.SetBool("IsMoving", true);
        _hunterAI.PlaySound(_hunterAI.HeardNoiseSound);

        _investigationTargetPosition = _hunterAI.LastKnownPlayerPosition;
        if (_hunterAI.NavAgent.isOnNavMesh)
        {
            _hunterAI.NavAgent.SetDestination(_investigationTargetPosition);
        }

        _currentInvestigationTime = _hunterAI.InvestigationDuration;
        _hunterAI.CurrentInvestigationTimer = _currentInvestigationTime;

        // Reset look around params
        _isAtLKP = false;
        _lookSweepsCompleted = 0;
        _hunterAI.HunterAnimator.SetBool("IsLookingAround", false); // If you have such a parameter
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

        if (_hunterAI.CanHearPlayerAlert)
        {
            _hunterAI.AcknowledgePlayerAlert();
            Debug.Log($"{_hunterAI.gameObject.name} (Investigating): Heard new alert. Resetting investigation. New LKP: {_hunterAI.LastKnownPlayerPosition}");
            // Resetting investigation to new LKP is like re-entering the state with new LKP
            OnEnterState(); // Re-initialize state for new LKP
            return;
        }

        // Check if reached destination OR if already at LKP and performing look around
        if (!_hunterAI.NavAgent.pathPending && (_hunterAI.NavAgent.remainingDistance < _hunterAI.NavAgent.stoppingDistance + 0.1f || _isAtLKP))
        {
            if (!_isAtLKP) // First time reaching LKP
            {
                _isAtLKP = true;
                _hunterAI.HunterAnimator.SetBool("IsMoving", false);
                _hunterAI.HunterAnimator.SetBool("IsLookingAround", true); // If anim exists
                StartNextLookSweep();
            }

            PerformLookAround();
        }
        else if (_isAtLKP) // If we were at LKP but started moving again (e.g. path invalidated)
        {
            _isAtLKP = false;
            _hunterAI.HunterAnimator.SetBool("IsMoving", true);
            _hunterAI.HunterAnimator.SetBool("IsLookingAround", false);
        }


        if (_currentInvestigationTime <= 0f)
        {
            Debug.Log($"{_hunterAI.gameObject.name} (Investigating): Investigation timer expired. Transitioning to Roam.");
            SM.TransitToState(_hunterSM.RoamingState);
            return;
        }
    }

    private void StartNextLookSweep()
    {
        if (_lookSweepsCompleted >= MAX_LOOK_SWEEPS)
        {
            _hunterAI.HunterAnimator.SetBool("IsLookingAround", false); // Finished all sweeps
            return; // Done with looking around
        }

        _currentLookPhase = LookAroundPhase.Sweeping;
        _lookAroundSubTimer = LOOK_SWEEP_DURATION;

        float sweepAngle = Random.Range(60f, 100f);
        if (_lookSweepsCompleted % 2 != 0) sweepAngle *= -1; // Alternate direction

        _targetLookRotation = Quaternion.Euler(0, _hunterAI.transform.eulerAngles.y + sweepAngle, 0);
        // Debug.Log($"Starting sweep {_lookSweepsCompleted + 1} to angle: {_hunterAI.transform.eulerAngles.y + sweepAngle}");
    }

    private void PerformLookAround()
    {
        if (!_isAtLKP || _lookSweepsCompleted >= MAX_LOOK_SWEEPS) return;

        _lookAroundSubTimer -= Time.deltaTime;

        if (_currentLookPhase == LookAroundPhase.Sweeping)
        {
            _hunterAI.transform.rotation = Quaternion.Slerp(_hunterAI.transform.rotation, _targetLookRotation, Time.deltaTime * 1.5f); // Adjust rotation speed
            if (_lookAroundSubTimer <= 0f || Quaternion.Angle(_hunterAI.transform.rotation, _targetLookRotation) < 5f)
            {
                _currentLookPhase = LookAroundPhase.Pausing;
                _lookAroundSubTimer = LOOK_PAUSE_DURATION;
                _lookSweepsCompleted++;
                // Debug.Log($"Finished sweep {_lookSweepsCompleted}, now pausing.");
            }
        }
        else if (_currentLookPhase == LookAroundPhase.Pausing)
        {
            if (_lookAroundSubTimer <= 0f)
            {
                if (_lookSweepsCompleted < MAX_LOOK_SWEEPS)
                {
                    StartNextLookSweep();
                }
                else
                {
                    // Debug.Log("Finished all look sweeps.");
                    _hunterAI.HunterAnimator.SetBool("IsLookingAround", false);
                }
            }
        }
    }

    public override void OnExitState()
    {
        if (_hunterAI == null) return;
        _hunterAI.CurrentInvestigationTimer = 0f; // Reset public timer
    }
}