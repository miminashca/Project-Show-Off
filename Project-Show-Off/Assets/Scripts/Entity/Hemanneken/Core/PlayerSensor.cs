using System;
using UnityEngine;

public class PlayerSensor : MonoBehaviour
{
    public Transform PlayerTransform { get; private set; }
    public Vector3 PlayerLastKnownPosition { get; private set; }
    public event Action OnPlayerDetected;

    private HemannekenAIConfig _aiConfig;
    private Transform _hemannekenTransform;

    public void Initialize(HemannekenAIConfig aiConfig, Transform hemannekenTransform)
    {
        _aiConfig = aiConfig;
        _hemannekenTransform = hemannekenTransform;

        // Try to find Player dynamically if not set, or rely on a central manager
        if (PlayerTransform == null)
        {
            // A more robust way might be a static reference or singleton for the player if it's always one.
            PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>(); // Or use a tag
            if (playerMovement != null)
            {
                PlayerTransform = playerMovement.transform;
            }
            else
            {
                Debug.LogError("PlayerSensor: PlayerMovement object not found! Player detection will be limited.", this);
            }
        }

        if (PlayerTransform != null)
        {
            PlayerLastKnownPosition = PlayerTransform.position;
        }
        else
        {
            // Fallback if player truly not found: LKP is ahead of Hemanneken
            PlayerLastKnownPosition = _hemannekenTransform.position + _hemannekenTransform.forward * 5f;
            Debug.LogWarning("PlayerSensor: PlayerTransform is null after Initialize. LKP set to fallback.", this);
        }

        // Subscribe to the global shout event
        HunterEventBus.OnHunterHeardPlayer += HandleGlobalPlayerShout;
    }

    void OnDestroy()
    {
        PlayerActionEventBus.OnPlayerShouted -= HandleGlobalPlayerShout;
    }

    private void HandleGlobalPlayerShout(Vector3 shoutPosition)
    {
        if (this == null || !enabled || !gameObject.activeInHierarchy || _aiConfig == null || _hemannekenTransform == null) return;

        // Use Hemanneken's investigateDistance as its "hearing range" for shouts.
        float hearingRange = _aiConfig.investigateDistance;
        float distanceToShout = Vector3.Distance(_hemannekenTransform.position, shoutPosition);

        if (distanceToShout <= hearingRange)
        {
            Debug.Log($"Hemanneken Sensor ({_hemannekenTransform.name}): Heard player shout at {shoutPosition} within range {hearingRange} (Dist: {distanceToShout}). LKP updated.");
            PlayerLastKnownPosition = shoutPosition;
            OnPlayerDetected?.Invoke(); // Signal to subscribed states (Roaming, Investigating)
        }
        else
        {
            // Debug.Log($"Hemanneken Sensor ({_hemannekenTransform.name}): Heard player shout at {shoutPosition} but was too far (Dist: {distanceToShout}, Range: {hearingRange}).");
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
    
    public Vector3 GetPlayerCameraPosition()
    {
        if (PlayerTransform == null)
        {
            Debug.LogWarning("Attempted to GetPlayerCameraPosition, but playerTransform is null. Returning last known.", this);
            return PlayerLastKnownPosition;
        }
        return PlayerTransform.gameObject.GetComponentInChildren<Camera>().transform.position;
    }

    public float GetDistanceToPlayer()
    {
        if (PlayerTransform == null || _hemannekenTransform == null) return float.MaxValue;

        Vector3 myPos = _hemannekenTransform.position;
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