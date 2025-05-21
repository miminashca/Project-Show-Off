using UnityEngine;
using System.Collections; // Required for Coroutines

public class HemannekenInvestigatingState : State
{
    private HemannekenStateMachine HSM => (HemannekenStateMachine)SM;
    private float _investigationEndTime;
    private Vector3 _investigationTargetPosition;
    private Coroutine _replyCoroutine;

    public HemannekenInvestigatingState(StateMachine pSM) : base(pSM)
    {
    }

    public override void OnEnterState()
    {
        Debug.Log("Entered Investigating State");
        HSM.LockNavMeshAgent(false); // Allow movement

        // Investigation timer
        _investigationEndTime = Time.time + HSM.investigationTimerDuration; // Assuming HSM.investigationTimerDuration = 10f;

        // Moves towards the player's position at the time of "Hey"
        _investigationTargetPosition = HSM.GetPlayerLastKnownPosition(); // HSM should store this when "Hey" is triggered
        HSM.aiNav.SetDestination(_investigationTargetPosition);

        // // Replies with its own “Hey” Call with a delay
        // float distanceToTarget = Vector3.Distance(HSM.transform.position, _investigationTargetPosition);
        // // Example delay: 0.1s per meter, min 0.5s, max 3s. Adjust as needed.
        // float replyDelay = Mathf.Clamp(distanceToTarget * 0.1f, 0.5f, 3.0f);
        // _replyCoroutine = HSM.StartCoroutine(DelayedHeyReplyCoroutine(replyDelay));

        HSM.OnPlayerDetected += NavigateToPlayer;
    }

    private IEnumerator DelayedHeyReplyCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        HSM.PlayReplyHeySound(); // Assuming HSM has a method like this
        Debug.Log("Hemanneken (Investigating) replies: Hey!");
    }

    private void NavigateToPlayer()
    {
        _investigationEndTime = Time.time + HSM.investigationTimerDuration;
        // Moves towards the player's position at the time of "Hey"
        _investigationTargetPosition = HSM.GetPlayerLastKnownPosition(); // HSM should store this when "Hey" is triggered
        HSM.aiNav.SetDestination(_investigationTargetPosition);

        // Replies with its own “Hey” Call with a delay
        float distanceToTarget = Vector3.Distance(HSM.transform.position, _investigationTargetPosition);
        // Example delay: 0.1s per meter, min 0.5s, max 3s. Adjust as needed.
        float replyDelay = Mathf.Clamp(distanceToTarget * 0.1f, 0.5f, 3.0f);
        _replyCoroutine = HSM.StartCoroutine(DelayedHeyReplyCoroutine(replyDelay));
    }
    public override void Handle()
    {
        // Transition to Roaming (true form) when investigation timer runs out
        if (Time.time >= _investigationEndTime)
        {
            SM.TransitToState(new HemannekenRoamingState(SM));
            return;
        }

        // Transition to Chasing when the player comes within 10 meters
        if (HSM.PlayerIsInTrueChaseDistance()) // Assuming 10m is HSM.trueChaseDistance
        {
            SM.TransitToState(new HemannekenChasingState(SM));
            return;
        }
    }

    public override void OnExitState()
    {
        Debug.Log("Exited Investigating State");
        if (_replyCoroutine != null)
        {
            HSM.StopCoroutine(_replyCoroutine);
            _replyCoroutine = null;
        }
        
        // Stop movement before transitioning. Next state's OnEnter will manage agent.
        HSM.LockNavMeshAgent(false); 
        HSM.OnPlayerDetected -= NavigateToPlayer;
    }
}