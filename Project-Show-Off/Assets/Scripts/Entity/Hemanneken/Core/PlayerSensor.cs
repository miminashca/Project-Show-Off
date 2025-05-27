using System;
using UnityEngine;

public class PlayerSensor : MonoBehaviour
{
    public Transform PlayerTransform { get; private set; }
    public Vector3 PlayerLastKnownPosition { get; private set; }

    public event Action OnPlayerDetected; // For investigating state

    private HemannekenAIConfig _aiConfig;

    public void Initialize(HemannekenAIConfig aiConfig)
    {
        _aiConfig = aiConfig;
        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            PlayerTransform = playerMovement.transform;
            PlayerLastKnownPosition = PlayerTransform.position;
        }
        else
        {
            Debug.LogError("PlayerMovement object not found in the scene! PlayerSensor will not function correctly.", this);
            // Fallback: last known position is ahead of the AI
            PlayerLastKnownPosition = transform.position + transform.forward * 5f; 
        }
    }

    public void RecordPlayerLastKnownPosition()
    {
        if (PlayerTransform != null)
        {
            PlayerLastKnownPosition = PlayerTransform.position;
            OnPlayerDetected?.Invoke();
        }
        else
        {
            Debug.LogWarning("Player transform is null. Cannot record last known position accurately.", this);
        }
    }

    public Vector3 GetPlayerCurrentPosition()
    {
        if (PlayerTransform == null)
        {
            Debug.LogWarning("Attempted to GetPlayerCurrentPosition, but playerTransform is null. Returning last known.", this);
            return PlayerLastKnownPosition;
        }
        return PlayerTransform.position;
    }

    public float GetDistanceToPlayer()
    {
        if (PlayerTransform == null) return float.MaxValue;

        Vector3 myPos = transform.position;
        myPos.y = 0; // Compare on 2D plane
        Vector3 playerPos = PlayerTransform.position;
        playerPos.y = 0;
        
        return Vector3.Distance(myPos, playerPos);
    }

    public bool IsPlayerInRabbitChaseDistance() => GetDistanceToPlayer() <= _aiConfig.chaseDistanceRabbit;
    public bool IsPlayerInTrueChaseDistance() => GetDistanceToPlayer() <= _aiConfig.chaseDistanceTrue;
    public bool IsPlayerInEndChaseDistance() => GetDistanceToPlayer() >= _aiConfig.endChaseDistance;
    public bool IsPlayerInStunDistance() => GetDistanceToPlayer() <= _aiConfig.stunDistance;
    public bool IsPlayerInInvestigateDistance() => GetDistanceToPlayer() <= _aiConfig.investigateDistance;
    public bool IsPlayerInAttachDistance() => GetDistanceToPlayer() <= _aiConfig.attachDistance;
}