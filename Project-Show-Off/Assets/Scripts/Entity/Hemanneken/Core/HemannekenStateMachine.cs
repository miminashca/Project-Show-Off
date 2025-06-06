using System;
using UnityEngine;

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

    // --- High-level Interaction Methods ---
    public void PerformAttachmentToPlayer()
    {
        _playerTransformForAttachment = Sensor.PlayerTransform; // Cache for detachment
        if (_playerTransformForAttachment != null)
        {
            Debug.Log("Hemanneken Attached to Player");
            transform.SetParent(_playerTransformForAttachment);
            // Define an attachment point or offset on the player, or use a fixed offset
            transform.localPosition = new Vector3(0, 1, -0.5f); // Example offset
            Visuals.SetModelVisibility(false); // Hide model as per HemannekenAttachedState logic
            //Movement.EnableAgent(false); // Ensure agent is fully stopped and disabled
        }
        else
        {
            Debug.LogError("Cannot attach: PlayerTransform is null.", this);
        }
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