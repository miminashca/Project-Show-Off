using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class HemannekenStateMachine : StateMachine
{
    [SerializeField, Range(0f, 10f)] private float chaseDistanceRabbit = 3f;
    [SerializeField, Range(0f, 15f)] private float chaseDistanceTrue = 15f;
    [SerializeField, Range(0f, 20f)] private float endChaseDistance = 20f;
    [SerializeField, Range(0f, 10f)] private float stunDistance = 10f;
    [SerializeField, Range(0f, 100f)] private float investigateDistance = 50f;
    [SerializeField, Range(0f, 100f)] private float attachDistance = 1f;
    [SerializeField] private GameObject hemannekenTrueModel;
    [SerializeField] private GameObject hemannekenRabbitModel;
    [Range(0, 30)] public int investigationTimerDuration = 10;

    // Timers and durations needed by states
    [Header("State Durations & Settings")]
    public float stunTimerDuration = 5f;
    public float transformationDuration = 1f; // Example duration for transformation
    public float deathEffectDuration = 2f; // Example duration for death effects

    [NonSerialized] public AiNavigation aiNav;
    [NonSerialized] public Navigation nav;
    [NonSerialized] public bool IsTrueForm; // You'll need to set this, e.g., based on spawn or transformation
    
    private Transform playerTransform;
    private Vector3 playerLastKnownPosition; // To store the last known position

    private GameObject currentModel;
    [NonSerialized] public InteractWithHemanneken interactor;

    protected override State InitialState => new HemannekenRoamingState(this);

    public event Action OnPlayerDetected;

    protected override void Start()
    {
        interactor = FindFirstObjectByType<InteractWithHemanneken>();
        
        aiNav = GetComponent<AiNavigation>();
        nav = GetComponent<Navigation>();
        
        // Find player, handle if not found
        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerTransform = playerMovement.transform;
            playerLastKnownPosition = playerTransform.position; // Initialize with current player position
        }
        else
        {
            Debug.LogError("PlayerMovement object not found in the scene! Hemanneken AI will not function correctly.", this);
            playerLastKnownPosition = transform.position + transform.forward * 5f; // Fallback
        }

        if (hemannekenRabbitModel && hemannekenTrueModel)
        {
            // Determine initial form based on spawn condition (e.g., over water or land)
            // For now, assuming IsTrueForm is set externally or defaults. Example:
            // IsTrueForm = CheckSpawnCondition(); // Implement this method
            SetForm(IsTrueForm);
        }

        HemannekenEventBus.OnHeyTriggered += RecordPlayerLastKnownPosition;
        base.Start();
    }

    private void OnDisable()
    {
        HemannekenEventBus.OnHeyTriggered -= RecordPlayerLastKnownPosition;
    }

    // Method to update the player's last known position
    // This should be called when an event occurs that makes the Hemanneken aware of the player's location (e.g., "Hey" call)
    public void RecordPlayerLastKnownPosition()
    {
        if (playerTransform != null)
        {
            playerLastKnownPosition = playerTransform.position;
            OnPlayerDetected?.Invoke();
        }
        else
        {
            // If playerTransform was lost somehow after Start (unlikely but good to be safe)
            Debug.LogWarning("Player transform is null. Cannot record last known position accurately.", this);
            // Keep the last valid playerLastKnownPosition or use a fallback
        }
    }

    // The requested method
    public Vector3 GetPlayerLastKnownPosition()
    {
        // This will return the position recorded by RecordPlayerLastKnownPosition()
        // or the initial player position if RecordPlayerLastKnownPosition() hasn't been called yet.
        return playerLastKnownPosition;
    }

    public float GetDistanceToPlayer()
    {
        if (playerTransform == null) return float.MaxValue; // Player not found, effectively infinite distance

        Vector3 posA = gameObject.transform.position;
        posA.y = 0;
        Vector3 posB = playerTransform.position;
        posB.y = 0;
        
        return Vector3.Magnitude(posA - posB);
    }

    public Vector3 GetPlayerPosition()
    {
        if (playerTransform == null)
        {
            Debug.LogError("Attempted to GetPlayerPosition, but playerTransform is null.", this);
            return playerLastKnownPosition; // Return last known as a fallback
        }
        return playerTransform.position;
    }

    public bool PlayerIsInRabbitChaseDistance()
    {
        return GetDistanceToPlayer() <= chaseDistanceRabbit;
    }

    public bool PlayerIsInEndChaseDistance()
    {
        return GetDistanceToPlayer() >= endChaseDistance;
    }

    public bool PlayerIsInTrueChaseDistance()
    {
        return GetDistanceToPlayer() <= chaseDistanceTrue;
    }

    public bool PlayerIsInInvestigateDistance()
    {
        return GetDistanceToPlayer() <= investigateDistance;
    }

    public bool PlayerIsInAttachingDistance()
    {
        return GetDistanceToPlayer() <= attachDistance;
    }
    public bool PlayerIsInStunDistance()
    {
        return GetDistanceToPlayer() <= stunDistance;
    }
    
    
    public void SetForm(bool isTrue)
    {
        IsTrueForm = isTrue;
        if (IsTrueForm)
        {
            if (hemannekenTrueModel)
            {
                if(currentModel) Destroy(currentModel);
                // Destroy existing model if any to prevent duplicates (you'll need a reference to it)
                // Or, ensure only one is active.
                // For now, assuming this is handled or it's an initial setup.
                currentModel = Instantiate(hemannekenTrueModel, this.gameObject.transform);
                if (hemannekenRabbitModel.transform.parent == this.transform) hemannekenRabbitModel.SetActive(false); // Example
            }
        }
        else
        {
            if (hemannekenRabbitModel)
            {
                if(currentModel) Destroy(currentModel);

                currentModel = Instantiate(hemannekenRabbitModel, this.gameObject.transform);
                if (hemannekenTrueModel.transform.parent == this.transform) hemannekenTrueModel.SetActive(false); // Example
            }
        }
    }


    public void LockNavMeshAgent(bool Lock)
    {
        //if (!aiNav || !aiNav.navAgent) return;

        if (Lock)
        {
            // aiNav.navAgent.updatePosition = false;
            // aiNav.navAgent.updateRotation = false;
            aiNav.navAgent.enabled = false;
        }
        else
        {
            // aiNav.navAgent.updatePosition = true;
            // aiNav.navAgent.updateRotation = true;
            aiNav.navAgent.enabled = true;
        }
    }
    // --- Methods for State Logic (as assumed by the state classes) ---

    public void PlayStunEffects() { Debug.Log("SFX/VFX: Hemanneken Stunned"); /* Implement actual effects */ }
    public void StopStunEffects() { Debug.Log("SFX/VFX: Hemanneken Stun Effects Stopped"); /* Implement cleanup */ }
    public void PlayReplyHeySound() { Debug.Log("SFX: Hemanneken replies 'Hey'"); /* Implement sound */ }

    public void PlayTransformationEffects()
    {
        ParticleSystem particles = GetComponentInChildren<ParticleSystem>();
        particles.Play();
        Debug.Log("SFX/VFX: Hemanneken Transforming");
    }
    public void StopTransformationEffects() { Debug.Log("SFX/VFX: Hemanneken Transformation Effects Stopped"); /* Implement cleanup */ }

    public void PlayDeathEffects()
    {
        ParticleSystem particles = GetComponentInChildren<ParticleSystem>();
        particles.Play();
        Debug.Log("SFX/VFX: Hemanneken Dying");
    }

    public void PerformAttachment()
    {
        Debug.Log("Hemanneken Attached to Player");
        // Logic to parent to player, apply offset, etc.
        // transform.SetParent(playerTransform);
        // transform.localPosition = new Vector3(0, 1, -0.5f); // Example offset
    }

    public void ApplySlowToPlayer()
    {
        Debug.Log("Player Slowed");
        // Access player movement script and reduce speed
        // e.g., playerTransform.GetComponent<PlayerMovement>().ApplySpeedModifier(0.7f);
    }

    public void RemoveSlowFromPlayer()
    {
        Debug.Log("Player Slow Removed");
        // e.g., playerTransform.GetComponent<PlayerMovement>().RemoveSpeedModifier(0.7f);
    }
    
    public bool IsPlayerSubmergedInWater()
    {
        // Placeholder: Implement logic to check if player is crouched in water
        // e.g., return PlayerStatus.Instance.IsSubmerged;
        return false; // Default to false
    }

    public void PlayPlayerDefeatAnimation() { Debug.Log("GAME OVER: Playing player defeat animation"); /* Implement */ }
    public void TriggerGameOver() { Debug.Log("GAME OVER triggered"); Time.timeScale = 0; /* Show UI, etc. */ }
}