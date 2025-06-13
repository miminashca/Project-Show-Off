using System;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(PlayerSensor), typeof(AgentMovement), typeof(HemannekenVisuals))]
public class HemannekenStateMachine : StateMachine
{
    [Header("Configuration")]
    [SerializeField] public HemannekenAIConfig aiConfig; // Assign in Inspector

    // Public references for States (Context)
    public PlayerSensor Sensor { get; private set; }
    public AgentMovement Movement { get; private set; }
    public HemannekenVisuals Visuals { get; private set; }
    public PlayerStateController Interactor { get; private set; } // Player interaction (e.g. lantern)

    // Internal state properties, managed by this SM or its components
    public bool IsInitiallyTrueForm { get; set; } // Set by HemannekenManager on spawn

    protected override State InitialState => new HemannekenRoamingState(this); // Default, can be adjusted

    // High-level game interaction properties
    private Transform _playerTransformForAttachment; // Store when needed

    // ... in HemannekenStateMachine.cs, inside Awake() ...
    protected virtual void Awake() 
    {
        if (aiConfig == null)
        {
            Debug.LogError("HemannekenAIConfig is not assigned in the Inspector!", this);
            enabled = false;
            return;
        }

        Sensor = GetComponent<PlayerSensor>();
        Movement = GetComponent<AgentMovement>();
        Visuals = GetComponent<HemannekenVisuals>();
        Interactor = FindFirstObjectByType<PlayerStateController>();

        if (Sensor == null) Debug.LogError("PlayerSensor not found!", this);
        else Sensor.Initialize(aiConfig, this.transform);

        // Find SpawnPointsManager - ensure this logic correctly finds your SpawnPointsManager
        SpawnPoint parentSpawnPoint = GetComponentInParent<SpawnPoint>();
        // Option 2: If SPManager is globally findable (less ideal but works)
        // if (spManager == null) spManager = FindFirstObjectByType<SpawnPointsManager>();
    
        if (parentSpawnPoint == null) Debug.LogWarning("SpawnPoint parent not found for AgentMovement initialization.", this);

        Movement.Initialize(parentSpawnPoint.gameObject.GetComponentInChildren<SpawnPointsManager>(), aiConfig); // Ensure SPManager is found
        Visuals.Initialize();

        Visuals.SetForm(IsInitiallyTrueForm, transform);
    }

    protected override void Start()
    {
        // Set initial form based on spawn condition
        Visuals.SetForm(IsInitiallyTrueForm, transform);

        // Base Start will initialize the first state.
        base.Start();
    }

    private void OnDestroy()
    {

    }

    private Vector3 randomOffset;
    // --- High-level Interaction Methods ---
    public void PerformAttachmentToPlayer()
    {
        _playerTransformForAttachment = Sensor.PlayerTransform;
        if (_playerTransformForAttachment != null)
        {
            Debug.Log("Hemanneken Attached to Player");

            // --- THIS IS THE KEY CHANGE: DO NOT PARENT THE OBJECT ---
            // transform.SetParent(_playerTransformForAttachment); // <-- REMOVED

            // 1. Calculate a random offset direction around the player
            Vector2 randomDirectionXZ = Random.insideUnitCircle.normalized;
            randomOffset = new Vector3(
                randomDirectionXZ.x * aiConfig.attachedStateDistance, // Random X position
                _playerTransformForAttachment.gameObject.GetComponentInChildren<Camera>().transform.localPosition.y,                         // Fixed vertical offset
                randomDirectionXZ.y * aiConfig.attachedStateDistance  // Random Z position (using Y from the 2D vector)
            );

            // 2. Set the entity's initial WORLD position
            transform.position = _playerTransformForAttachment.position + randomOffset;
            transform.rotation = Quaternion.identity;
            // Visuals.SetModelVisibility(true); // Ensure model is visible
            // Movement.EnableAgent(false); // Ensure agent is disabled
        }
        else
        {
            Debug.LogError("Cannot attach: PlayerTransform is null.", this);
        }
    }

    public void HandleAttachment()
    {
        transform.position = _playerTransformForAttachment.position + randomOffset;
    }
    

    public void PerformDetachmentFromPlayer()
    {
        Debug.Log("Hemanneken Detached from Player");
        if (_playerTransformForAttachment != null)
        {
            transform.SetParent(null); // Detach
            _playerTransformForAttachment = null;
        }
        Visuals.SetModelVisibility(true); // Show model again
        // Agent will be re-enabled by the next state if needed
    }

    public void ApplySlowToPlayer()
    {
        Debug.Log("Player Slowed");
        // Example: Sensor.PlayerTransform?.GetComponent<PlayerMovement>()?.ApplySpeedModifier(0.7f);
    }

    public void RemoveSlowFromPlayer()
    {
        Debug.Log("Player Slow Removed");
        // Example: Sensor.PlayerTransform?.GetComponent<PlayerMovement>()?.RemoveSpeedModifier(0.7f);
    }
    
    public bool IsPlayerSubmergedInWater()
    {
        // Placeholder: Implement logic to check if player is crouched in water
        // e.g., return PlayerStatus.Instance.IsSubmerged;
        Debug.LogWarning("IsPlayerSubmergedInWater check not implemented.");
        return false; 
    }

    public void PlayPlayerDefeatAnimation() { Debug.Log("GAME OVER: Playing player defeat animation"); /* Implement */ }
    public void TriggerGameOver() { Debug.Log("GAME OVER triggered"); Time.timeScale = 0; /* Show UI, etc. */ }

    public void DestroySelfAfterDelay(float delay)
    {
        Destroy(gameObject, delay);
    }
}