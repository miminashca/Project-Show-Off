// File: AgentMovement.cs
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

public enum MovementStyle
{
    Direct,
    SplineWave,
    Hop
}

[RequireComponent(typeof(HemannekenEventBus))] // Good practice to ensure it exists
public class AgentMovement : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private HemannekenAIConfig aiConfig;
    private SpawnPointsManager spawnPointsManager;
    
    [Header("Runtime State")]
    [SerializeField] private MovementStyle _currentMovementStyle = MovementStyle.Direct;
    [SerializeField] public bool _isGroundRestricted = false;
    [SerializeField] private bool _isActivelyMoving = false;

    // --- Core Modules & State ---
    private Dictionary<MovementStyle, IMovementStrategy> _movementStrategies;
    private IMovementStrategy _currentStrategy;
    private GroundingModule _groundingModule;
    public HemannekenEventBus _eventBus { get; private set; }

    // --- Roaming State ---
    private List<Vector3> _patrolPoints = new List<Vector3>();
    private int _currentPatrolIndex = -1;
    private bool _needsNewRoamTarget = true;
    private float _waypointPauseTimer = 0f;
    private const float WAYPOINT_PAUSE_DURATION = 2.0f;
    
    private bool _pauseOnArrival = true;
    
    public bool IsGroundRestricted => _isGroundRestricted;

    #region Initialization
    
    void Awake()
    {
        _eventBus = new HemannekenEventBus();
    }
    
    void Start()
    {
        if (aiConfig == null)
        {
            Debug.LogError($"[{gameObject.name}] AI Config is not assigned in AgentMovement.", this);
            this.enabled = false;
            return;
        }
        //InitializeFromConfig();
    }
    
    public void InitializeFromConfig()
    {
        // 1. Initialize Grounding Module
        _groundingModule = new GroundingModule(
            aiConfig.groundLayerMask,
            aiConfig.groundOffset,
            aiConfig.groundRaycastMaxDistance,
            aiConfig.groundRaycastStartHeightOffset
        );
        SetGroundRestriction(aiConfig.defaultRoamOnGround);

        // 2. Initialize Movement Strategies
        _movementStrategies = new Dictionary<MovementStyle, IMovementStrategy>
        {
            [MovementStyle.Direct] = new DirectMovementStrategy(aiConfig),
            [MovementStyle.SplineWave] = new SplineMovementStrategy(aiConfig, _groundingModule),
            [MovementStyle.Hop] = new HopMovementStrategy(aiConfig, _groundingModule, _eventBus)
        };
        
        // 3. Initialize Patrol Points
        SpawnPoint parentSpawnPoint = GetComponentInParent<SpawnPoint>();
        spawnPointsManager = parentSpawnPoint.gameObject.GetComponentInChildren<SpawnPointsManager>();
        if (spawnPointsManager != null && spawnPointsManager.SecondarySpawnPoints.Any())
        {
            _patrolPoints = spawnPointsManager.SecondarySpawnPoints.Select(p => p.transform.position).ToList();
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] No patrol points found or SpawnPointsManager not assigned.", this);
        }
    }

    #endregion

    #region Update Loop

    void Update()
    {
        if (_waypointPauseTimer > 0)
        {
            _waypointPauseTimer -= Time.deltaTime;
            return; // IMPORTANT: return here
        }

        if (_isActivelyMoving && _currentStrategy != null)
        {
            _currentStrategy.UpdateMovement(this);
            
            if (_isGroundRestricted)
            {
                transform.position = _groundingModule.SnapToGround(transform.position, transform.position.y);
            }
        }
        else if (_needsNewRoamTarget)
        {
            StartNextRoamSegment();
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Commands the agent to move to a specific destination using a given style.
    /// </summary>
    public void SetDestination(Vector3 destination, MovementStyle style, bool? groundRestricted = null)
    {
        StopAgentCompletely(false); // Stop current movement but don't reset roam state
        
        _currentMovementStyle = style;
        if (groundRestricted.HasValue)
        {
            SetGroundRestriction(groundRestricted.Value);
        }

        if (!_movementStrategies.ContainsKey(style))
        {
            Debug.LogError($"Movement strategy for '{style}' not initialized!", this);
            return;
        }
        
        _currentStrategy = _movementStrategies[style];
        _currentStrategy.OnArrival += OnDestinationArrival;

        Vector3 finalDestination = destination;
        if (_isGroundRestricted)
        {
            finalDestination = _groundingModule.SnapToGround(destination, destination.y);
        }

        _currentStrategy.SetDestination(this, finalDestination);
        _isActivelyMoving = true;
        _needsNewRoamTarget = false;
    }

    /// <summary>
    /// Starts the agent's roaming behavior.
    /// </summary>
    /// <param name="pauseAtWaypoints">If true, the agent will pause at each patrol point.</param>
    public void RoamWaypoints(MovementStyle style, bool groundRestricted, bool pauseAtWaypoints)
    {
        StopAgentCompletely(false);
        _currentMovementStyle = style;
        SetGroundRestriction(groundRestricted);
        
        // --- SET THE NEW FLAG ---
        _pauseOnArrival = pauseAtWaypoints;

        _needsNewRoamTarget = true;
    }


    /// <summary>
    /// Stops all movement immediately and clears the current path.
    /// </summary>
    /// <param name="resetRoaming">If true, the agent will forget its next roam target.</param>
    public void StopAgentCompletely(bool resetRoaming = true)
    {
        _isActivelyMoving = false;
        
        if (_currentStrategy != null)
        {
            _currentStrategy.OnArrival -= OnDestinationArrival;
            _currentStrategy.Stop();
            _currentStrategy = null;
        }

        if (resetRoaming)
        {
            _needsNewRoamTarget = true;
        }
    }

    /// <summary>
    /// Toggles whether the agent's Y position is snapped to the ground.
    /// </summary>
    public void SetGroundRestriction(bool isRestricted)
    {
        _isGroundRestricted = isRestricted;
        // NOTE: You may need to re-initialize strategies if their behavior depends
        // on this flag at construction time. For now, we assume it's checked at runtime.
    }

    #endregion

    #region Internal Logic

    private void StartNextRoamSegment()
    {
        if (!_patrolPoints.Any()) {
            _needsNewRoamTarget = false; // Nothing to do
            return;
        }

        _needsNewRoamTarget = false; // We are handling it now, prevent re-entry.

        Vector3 nextDestination = FindNextPatrolPoint();

        // Use the public API to start the movement.
        // The SetDestination method will handle the logic of starting the movement.
        SetDestination(nextDestination, _currentMovementStyle);
    }
    
    private Vector3 FindNextPatrolPoint()
    {
        if (!_patrolPoints.Any())
        {
            Debug.LogWarning("FindNextPatrolPoint called with no points available.", this);
            return transform.position; // Return current position as a fallback
        }

        if (_patrolPoints.Count == 1)
        {
            _currentPatrolIndex = 0;
            return _patrolPoints[0];
        }

        int nextIndex = _currentPatrolIndex;
        int attempts = 0;
        // Try to find a different point from the current one to avoid going back and forth
        while (nextIndex == _currentPatrolIndex && attempts < _patrolPoints.Count * 2)
        {
            nextIndex = Random.Range(0, _patrolPoints.Count);
            attempts++;
        }
        _currentPatrolIndex = nextIndex;
        return _patrolPoints[_currentPatrolIndex];
    }

    /// <summary>
    /// Event handler called by the current movement strategy when it arrives.
    /// </summary>
    private void OnDestinationArrival()
    {
        _isActivelyMoving = false;

        if (_currentStrategy != null)
        {
            _currentStrategy.OnArrival -= OnDestinationArrival;
            _currentStrategy = null;
        }
        
        // --- THIS IS THE KEY CHANGE ---
        // Only start the pause timer IF the command requested it.
        if (_pauseOnArrival)
        {
            _waypointPauseTimer = WAYPOINT_PAUSE_DURATION;
        }

        // We always need a new roam target after arrival, whether we paused or not.
        // If we don't pause, the Update loop will immediately call StartNextRoamSegment on the next frame.
        _needsNewRoamTarget = true;
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // Delegate gizmo drawing to the current strategy
        _currentStrategy?.DrawGizmos();

        // Draw common gizmos
        if (_isGroundRestricted)
        {
            _groundingModule?.DrawGizmos(transform);
        }
        
        if (_isActivelyMoving) {
             Gizmos.color = Color.red;
             Gizmos.DrawSphere(transform.position, 0.2f);
        }
    }
    
    #endregion
}