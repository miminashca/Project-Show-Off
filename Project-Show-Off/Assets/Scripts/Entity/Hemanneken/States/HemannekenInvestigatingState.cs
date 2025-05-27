using UnityEngine;
using System.Collections;

public class HemannekenInvestigatingState : State
{
    private HemannekenStateMachine HSM => (HemannekenStateMachine)SM;
    private float _investigationEndTime;
    private Coroutine _replyCoroutine;

    public HemannekenInvestigatingState(StateMachine pSM) : base(pSM) { }

    public override void OnEnterState()
    {
        Debug.Log("Entered Investigating State");
        //HSM.Movement.EnableAgent(true); // Allow movement

        // Subscribe to player detection events from the sensor for this state
        HSM.Sensor.OnPlayerDetected += HandlePlayerDetectedWhileInvestigating;
        
        SetupInvestigation();
    }

    private void SetupInvestigation()
    {
        _investigationEndTime = Time.time + HSM.aiConfig.investigationTimerDuration;
        
        Vector3 targetPos = HSM.Sensor.PlayerLastKnownPosition;
        // Optional: Add a slight Y offset if your ground level isn't perfectly flat for NavMeshAgent
        // targetPos.y = HSM.transform.position.y; // Or a fixed offset from ground
        HSM.Movement.SetDestination(targetPos, MovementStyle.SplineWave);

        // Cancel previous reply if any, and start a new one
        if (_replyCoroutine != null) HSM.StopCoroutine(_replyCoroutine);
        _replyCoroutine = HSM.StartCoroutine(DelayedHeyReplyCoroutine());
    }

    private void HandlePlayerDetectedWhileInvestigating()
    {
        // Player made another "Hey" or was re-detected, reset investigation
        Debug.Log("Player re-detected during investigation. Resetting target and timer.");
        SetupInvestigation(); // Re-target and reset timer
    }

    private IEnumerator DelayedHeyReplyCoroutine()
    {
        // Dynamic delay based on distance or fixed, adjust as needed
        float distanceToTarget = Vector3.Distance(HSM.transform.position, HSM.Sensor.PlayerLastKnownPosition);
        float replyDelay = Mathf.Clamp(distanceToTarget * 0.05f, 0.5f, 2.0f); // Shorter delay
        
        yield return new WaitForSeconds(replyDelay);
        HSM.Visuals.PlayReplyHeySound();
    }

    public override void Handle()
    {
        if (Time.time >= _investigationEndTime)
        {
            Debug.Log("Investigation timer ended.");
            SM.TransitToState(new HemannekenRoamingState(SM)); // Back to roaming
            return;
        }

        // If player comes into direct chase range while investigating
        if (HSM.Visuals.IsTrueForm && HSM.Sensor.IsPlayerInTrueChaseDistance())
        {
            Debug.Log("Player entered chase distance during investigation.");
            SM.TransitToState(new HemannekenChasingState(SM));
            return;
        }
        // If it was in rabbit form and investigating (though current logic makes it transform first)
        // and player comes into rabbit chase range
        else if (!HSM.Visuals.IsTrueForm && HSM.Sensor.IsPlayerInRabbitChaseDistance())
        {
            Debug.Log("Player entered rabbit chase distance during investigation (transition to Enchantix).");
            SM.TransitToState(new HemannekenEnchantixState(SM));
            return;
        }

        // Continue moving towards investigation point (handled by AgentMovement)
    }

    public override void OnExitState()
    {
        Debug.Log("Exited Investigating State");
        if (_replyCoroutine != null)
        {
            HSM.StopCoroutine(_replyCoroutine);
            _replyCoroutine = null;
        }
        HSM.Sensor.OnPlayerDetected -= HandlePlayerDetectedWhileInvestigating;
        // Next state's OnEnter will manage agent.
    }
}