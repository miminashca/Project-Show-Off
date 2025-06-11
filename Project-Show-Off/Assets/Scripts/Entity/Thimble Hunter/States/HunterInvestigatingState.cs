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
    private int _lookSweepsCompleted = 0;
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
        if (!_hunterAI.HunterAudioSource.isPlaying) // Avoid interrupting other sounds
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
        _hunterAI.HunterAnimator.SetBool("IsLookingAround", false);
        _hunterAI.IsActivelyScanning = false; // Reset alertness flag
    }

    public override void Handle()
    {
        if (_hunterAI == null) return;

        if (_hunterAI.IsPlayerFullySpotted)
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
            OnEnterState(); // Re-initialize state for new LKP
            return;
        }

        if (!_hunterAI.NavAgent.pathPending && (_hunterAI.NavAgent.remainingDistance < _hunterAI.NavAgent.stoppingDistance + 0.1f || _isAtLKP))
        {
            if (!_isAtLKP) // First time reaching LKP
            {
                _isAtLKP = true;
                _hunterAI.HunterAnimator.SetBool("IsMoving", false);
                _hunterAI.HunterAnimator.SetBool("IsLookingAround", true);
                _hunterAI.IsActivelyScanning = true; // --- SETTING ALERTNESS FLAG ---
                StartNextLookSweep();
            }

            PerformLookAround();
        }
        else if (_isAtLKP)
        {
            _isAtLKP = false;
            _hunterAI.HunterAnimator.SetBool("IsMoving", true);
            _hunterAI.HunterAnimator.SetBool("IsLookingAround", false);
            _hunterAI.IsActivelyScanning = false; // No longer scanning
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
        if (_lookSweepsCompleted >= _hunterAI.InvestigationMaxLookSweeps)
        {
            _hunterAI.HunterAnimator.SetBool("IsLookingAround", false);
            _hunterAI.IsActivelyScanning = false; // Finished all sweeps
            return;
        }

        _currentLookPhase = LookAroundPhase.Sweeping;
        _lookAroundSubTimer = _hunterAI.InvestigationLookSweepDuration; // Use new variable

        float sweepAngle = Random.Range(60f, 100f);
        if (_lookSweepsCompleted % 2 != 0) sweepAngle *= -1;

        _targetLookRotation = Quaternion.Euler(0, _hunterAI.transform.eulerAngles.y + sweepAngle, 0);
    }

    private void PerformLookAround()
    {
        if (!_isAtLKP || _lookSweepsCompleted >= _hunterAI.InvestigationMaxLookSweeps) return;

        _lookAroundSubTimer -= Time.deltaTime;

        if (_currentLookPhase == LookAroundPhase.Sweeping)
        {
            _hunterAI.transform.rotation = Quaternion.Slerp(_hunterAI.transform.rotation, _targetLookRotation, Time.deltaTime * 1.5f);
            if (_lookAroundSubTimer <= 0f || Quaternion.Angle(_hunterAI.transform.rotation, _targetLookRotation) < 5f)
            {
                _currentLookPhase = LookAroundPhase.Pausing;
                _lookAroundSubTimer = _hunterAI.InvestigationLookPauseDuration; // Use new variable
                _lookSweepsCompleted++;
            }
        }
        else if (_currentLookPhase == LookAroundPhase.Pausing)
        {
            if (_lookAroundSubTimer <= 0f)
            {
                if (_lookSweepsCompleted < _hunterAI.InvestigationMaxLookSweeps)
                {
                    StartNextLookSweep();
                }
                else
                {
                    _hunterAI.HunterAnimator.SetBool("IsLookingAround", false);
                    _hunterAI.IsActivelyScanning = false; // Finished all sweeps
                }
            }
        }
    }

    public override void OnExitState()
    {
        if (_hunterAI == null) return;
        _hunterAI.CurrentInvestigationTimer = 0f;
        _hunterAI.IsActivelyScanning = false;
    }
}