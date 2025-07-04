using System;
using UnityEngine;
using System.Collections;

public class LadyStateMachine : StateMachine
{
    // Public properties for states to access
    [HideInInspector] public LadyAIConfig Config;
    [HideInInspector] public GazeSystem GazeSystem;
    [HideInInspector] public FeedbackController FeedbackController;
    [HideInInspector] public Renderer AiRenderer;
    [HideInInspector] public Collider AiCollider;
    [HideInInspector] public Transform PlayerTransform;

    // NEW FMOD CHANGE
    public WLSoundController WLSoundController;
    // END FMOD CHANGE

    public Transform PullTargetTransform;

    public event Action OnLadyDie;

    // State-persistent timers
    public float ContinuousGazeTimer { get; set; }

    // State instances
    public CreepingState CreepingState { get; private set; }
    public SeenState SeenState { get; private set; }
    public DissipatedState DissipatedState { get; private set; }

    // Initial state property for the base StateMachine
    protected override State InitialState => CreepingState;
    
    private void Awake()
    {
        AiRenderer = GetComponentInChildren<Renderer>();
        AiCollider = GetComponent<Collider>();
        
        // Instantiate all states
        CreepingState = new CreepingState(this);
        SeenState = new SeenState(this);
        DissipatedState = new DissipatedState(this);

        // NEW FMOD CHANGE
        WLSoundController = GetComponent<WLSoundController>();
        // END FMOD CHANGE
    }
    
    public void Initialize(LadyAIConfig config, Transform playerTransform)
    {
        this.Config = config;
        this.PlayerTransform = playerTransform;
        
        // Find necessary player components. A more robust system might use a service locator.
        GazeSystem = FindObjectOfType<GazeSystem>();
        FeedbackController = FindObjectOfType<FeedbackController>();
        
        if (GazeSystem == null || FeedbackController == null)
        {
            Debug.LogError("WhiteLady_AIController could not find GazeSystem or FeedbackController in the scene!", this);
            Destroy(gameObject);
            return;
        }
        
        // Tell the gaze system what to look for
        GazeSystem.SetTarget(this.AiRenderer);
        
        // Pass the config to the feedback controller
        FeedbackController.Initialize(this.Config);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy(); // Calls OnExitState on current state
        
        // Clean up references
        if (GazeSystem != null)
        {
            GazeSystem.ClearTarget();
        }

        // This is a safety net in case de-spawning fails.
        if (GameManager.Instance.isWhiteLadyActive)
        {
            GameManager.Instance.isWhiteLadyActive = false;
        }
        
        OnLadyDie?.Invoke();
    }

    public void DeSpawn()
    {
        StartCoroutine(DeSpawnRoutine());
    }

    private IEnumerator DeSpawnRoutine()
    {
        // Req 3.3.2: Wait for the despawn delay
        yield return new WaitForSeconds(Config.despawnDelay);

        // Req 3.3.3: Final cleanup
        Debug.Log("White Lady de-spawning.");
        GameManager.Instance.isWhiteLadyActive = false;
        Destroy(gameObject);
    }
}