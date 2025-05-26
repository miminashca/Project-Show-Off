using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum MovementStyle // Place this outside the class or in its own file if preferred
{
    Direct,
    SplineWave
}

public class AgentMovement : MonoBehaviour
{
    [Header("Runtime Movement Tweaks")]
    public float speed = 3.5f;
    public float rotationSpeed = 720f;
    public float stoppingDistance = 0.2f;

    [Header("Runtime Wave Path Tweaks (for SplineWave style)")]
    public float waveAmplitude = 1.0f;
    public float waveFrequency = 1.0f;
    public int wavePathResolution = 8;

    // --- Internal State ---
    private MovementStyle _currentMovementStyle = MovementStyle.Direct; // Default to direct
    private Vector3 _directTargetPosition; // Used for Direct movement style

    // --- Spline Specific State ---
    private List<Vector3> _currentFullPathPoints = new List<Vector3>();
    private int _currentSplineSegmentIndex;
    private float _splineSegmentProgress;

    // --- General State ---
    private bool _isActivelyMoving = false;
    private bool _needsNewPathSegmentForRoaming = true; // Specifically for roaming

    // Roaming
    private SpawnPointsManager _spManager;
    private List<Vector3> _mainPatrolPoints = new List<Vector3>();
    private int _currentMainPatrolIndex = -1;

    // Single waypoint pause
    private float _singleWaypointPauseTimer = 0f;
    private const float SINGLE_WAYPOINT_PAUSE_DURATION = 2.0f;

    private bool _patrolPointsInitialized = false;
    private const string LOG_PREFIX = "[AgentMovement_V15_Style] ";

    public void Initialize(SpawnPointsManager spawnPointsManager, HemannekenAIConfig aiConfig)
    {
        speed = aiConfig.defaultSpeed;
        rotationSpeed = aiConfig.rotationSpeed;
        stoppingDistance = aiConfig.stoppingDistance;
        waveAmplitude = aiConfig.waveAmplitude;
        waveFrequency = aiConfig.waveFrequency;
        wavePathResolution = aiConfig.wavePathResolution;

        _spManager = spawnPointsManager;
        if (_spManager != null)
        {
            _spManager.SpawnPointsInitialized -= InitMainPatrolPoints;
            _spManager.SpawnPointsInitialized += InitMainPatrolPoints;
            if (_spManager.SpawnPoints != null && _spManager.SpawnPoints.Count > 0 && !_patrolPointsInitialized)
            {
                InitMainPatrolPoints();
            }
        }
    }

    private void InitMainPatrolPoints()
    {
        _mainPatrolPoints.Clear();
        if (_spManager == null || _spManager.SpawnPoints == null || _spManager.SpawnPoints.Count == 0)
        {
            _patrolPointsInitialized = false; return;
        }
        foreach (SpawnPoint p in _spManager.SpawnPoints) _mainPatrolPoints.Add(p.transform.position);
        _patrolPointsInitialized = _mainPatrolPoints.Count > 0;
        _needsNewPathSegmentForRoaming = true;
    }

    void Update()
    {
        if (_singleWaypointPauseTimer > 0f)
        {
            _singleWaypointPauseTimer -= Time.deltaTime;
            if (_singleWaypointPauseTimer <= 0f)
            {
                _needsNewPathSegmentForRoaming = true;
                _isActivelyMoving = false;
            }
            return;
        }

        if (_isActivelyMoving)
        {
            if (_currentMovementStyle == MovementStyle.SplineWave)
            {
                if (_currentFullPathPoints.Count >= 4) // Need enough points for Catmull-Rom
                {
                    FollowSplinePath();
                }
                else // Not enough points for spline, likely an issue or direct path masquerading
                {
                    // Fallback or handle error: For now, just stop if spline mode has no path
                    // Debug.LogWarning(LOG_PREFIX + "In SplineWave mode but not enough path points. Stopping.");
                    ArrivedAtEndOfDirectPath(); // Treat as arrived
                }
            }
            else // MovementStyle.Direct
            {
                MoveDirectlyTowards(_directTargetPosition);
                if (Vector3.Distance(transform.position, _directTargetPosition) <= stoppingDistance)
                {
                    ArrivedAtEndOfDirectPath();
                }
            }
        }
    }

    // --- Direct Movement Logic ---
    private void MoveDirectlyTowards(Vector3 targetPosition)
    {
        float agentCurrentY = transform.position.y;
        Vector3 directionToTarget = targetPosition - transform.position;
        directionToTarget.y = 0;

        if (directionToTarget.magnitude < 0.01f) return;

        Vector3 movementThisFrame = directionToTarget.normalized * speed * Time.deltaTime;
        if (movementThisFrame.magnitude > directionToTarget.magnitude)
        {
            movementThisFrame = directionToTarget;
        }
        
        transform.position += movementThisFrame;
        transform.position = new Vector3(transform.position.x, agentCurrentY, transform.position.z);

        if (directionToTarget.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void ArrivedAtEndOfDirectPath()
    {
        if (_directTargetPosition != Vector3.zero) // Check if target was valid
             transform.position = new Vector3(_directTargetPosition.x, transform.position.y, _directTargetPosition.z);
        _isActivelyMoving = false;
        // For direct paths, arriving means it might need a new roam path if it was an intermediate step
        // or the state machine will issue a new command.
        // If RoamWaypoints was what triggered this direct move (which it shouldn't now),
        // then _needsNewPathSegmentForRoaming should be true.
        // If SetDestination was called, the calling state handles what's next.
        // For simplicity, let's assume direct paths are terminal for AgentMovement itself.
        // The state machine decides the next action.
        // Debug.Log(LOG_PREFIX + "Arrived at end of Direct path.");
    }


    // --- Spline Movement Logic (from V14) ---
    private void FollowSplinePath()
    {
        float agentCurrentY = transform.position.y;
        float distanceToMoveThisFrame = speed * Time.deltaTime;
        
        Vector3 p1_spline = _currentFullPathPoints[_currentSplineSegmentIndex];
        Vector3 p2_spline = _currentFullPathPoints[_currentSplineSegmentIndex + 1];
        float estimatedSegmentLength = Vector3.Distance(p1_spline, p2_spline);
        if (estimatedSegmentLength < 0.01f) estimatedSegmentLength = 0.01f;

        _splineSegmentProgress += distanceToMoveThisFrame / estimatedSegmentLength;

        while (_splineSegmentProgress >= 1.0f)
        {
            _splineSegmentProgress -= 1.0f;
            _currentSplineSegmentIndex++;

            if (_currentSplineSegmentIndex >= _currentFullPathPoints.Count - 2)
            {
                ArrivedAtEndOfFullPathSpline();
                return;
            }
            p1_spline = _currentFullPathPoints[_currentSplineSegmentIndex];
            p2_spline = _currentFullPathPoints[_currentSplineSegmentIndex + 1];
            estimatedSegmentLength = Vector3.Distance(p1_spline, p2_spline);
            if (estimatedSegmentLength < 0.01f) estimatedSegmentLength = 0.01f;
        }

        Vector3 P0 = (_currentSplineSegmentIndex == 0) ? _currentFullPathPoints[0] : _currentFullPathPoints[_currentSplineSegmentIndex - 1];
        Vector3 P1 = _currentFullPathPoints[_currentSplineSegmentIndex];
        Vector3 P2 = _currentFullPathPoints[_currentSplineSegmentIndex + 1];
        Vector3 P3 = (_currentSplineSegmentIndex + 2 >= _currentFullPathPoints.Count) ? _currentFullPathPoints.Last() : _currentFullPathPoints[_currentSplineSegmentIndex + 2];

        Vector3 targetSplinePosition = GetCatmullRomPosition(_splineSegmentProgress, P0, P1, P2, P3);
        targetSplinePosition.y = agentCurrentY;

        Vector3 directionToSplineTarget = targetSplinePosition - transform.position;
        directionToSplineTarget.y = 0;

        if (directionToSplineTarget.magnitude < 0.01f) return;

        Vector3 movementThisFrame = directionToSplineTarget.normalized * speed * Time.deltaTime;
        if (movementThisFrame.magnitude > directionToSplineTarget.magnitude)
        {
            movementThisFrame = directionToSplineTarget;
        }

        transform.position += movementThisFrame;
        transform.position = new Vector3(transform.position.x, agentCurrentY, transform.position.z);

        if (directionToSplineTarget.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToSplineTarget.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        Vector3 finalDestinationOnSpline = _currentFullPathPoints.Last();
        finalDestinationOnSpline.y = agentCurrentY;
        if (Vector3.Distance(transform.position, finalDestinationOnSpline) <= stoppingDistance && 
            _currentSplineSegmentIndex >= _currentFullPathPoints.Count - 3)
        {
            ArrivedAtEndOfFullPathSpline();
        }
    }
    
    public static Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float t2 = t * t; float t3 = t2 * t;
        return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    private void ArrivedAtEndOfFullPathSpline()
    {
        if (_currentFullPathPoints.Count > 0) {
            Vector3 finalPoint = _currentFullPathPoints.Last();
            transform.position = new Vector3(finalPoint.x, transform.position.y, finalPoint.z);
        }
        _isActivelyMoving = false;
        _needsNewPathSegmentForRoaming = true; // Ready for a new roam path
        _currentFullPathPoints.Clear();
        // Debug.Log(LOG_PREFIX + "Arrived at end of full SplineWave path.");
    }
    
    // --- Public Methods to Control Movement ---
    public void SetDestination(Vector3 destination, MovementStyle style = MovementStyle.Direct)
    {
        _singleWaypointPauseTimer = 0f;
        _currentFullPathPoints.Clear(); // Clear any previous spline path
        _currentMovementStyle = style;

        if (style == MovementStyle.Direct)
        {
            _directTargetPosition = destination;
            _needsNewPathSegmentForRoaming = false; // Not roaming when direct destination is set
            if (Vector3.Distance(transform.position, _directTargetPosition) > stoppingDistance * 0.5f)
            {
                _isActivelyMoving = true;
            }
            else // Already at destination
            {
                _isActivelyMoving = false;
                // If called by a state, that state knows it has "arrived"
            }
        }
        else // MovementStyle.SplineWave (e.g., for Chasing)
        {
            // For chasing with a wave, we generate a wave towards the destination
            // This is a simplified chase wave; a real chase might dynamically update this.
            GenerateWavePathToPoint(destination);
            _needsNewPathSegmentForRoaming = false; // This is a specific chase/wave path
        }
    }

    public void RoamWaypoints() // Will always use SplineWave style
    {
        if (!_patrolPointsInitialized || _mainPatrolPoints.Count == 0) return;
        if (_singleWaypointPauseTimer > 0f) return;

        if (_needsNewPathSegmentForRoaming && !_isActivelyMoving)
        {
            _currentMovementStyle = MovementStyle.SplineWave; // Ensure roam uses spline
            GenerateNextSplinePatrolRouteForRoaming();
        }
    }

    private void GenerateWavePathToPoint(Vector3 targetPoint) // Used by SetDestination for SplineWave chase
    {
        _isActivelyMoving = false; // Stop current movement before generating new path
        _currentFullPathPoints.Clear();
        Vector3 segmentStartPoint = transform.position;

        // Add padding for Catmull-Rom
        _currentFullPathPoints.Add(segmentStartPoint); 
        _currentFullPathPoints.Add(segmentStartPoint); 

        Vector3 pathDirectionGlobal = (targetPoint - segmentStartPoint);
        pathDirectionGlobal.y = 0;
        float totalPathDistance = pathDirectionGlobal.magnitude;
        
        // Use slightly reduced amplitude/frequency for a chase wave, or make these params too
        float chaseWaveAmplitude = waveAmplitude * 0.5f; // Example: half amplitude for chase
        float chaseWaveFrequency = waveFrequency * 0.75f; // Example: slightly less wiggly

        if (totalPathDistance > stoppingDistance * 0.5f && chaseWaveAmplitude > 0.01f && wavePathResolution > 0)
        {
            Vector3 pathDirectionNormalized = pathDirectionGlobal.normalized;
            Vector3 perpendicularDirection = Vector3.Cross(pathDirectionNormalized, Vector3.up).normalized;
            if (Random.value > 0.5f) perpendicularDirection *= -1f;

            for (int i = 1; i <= wavePathResolution - 1; i++)
            {
                float t = (float)i / wavePathResolution;
                Vector3 pointOnDirectLine = segmentStartPoint + pathDirectionNormalized * (t * totalPathDistance);
                float sineOffset = Mathf.Sin(t * chaseWaveFrequency * 2f * Mathf.PI) * chaseWaveAmplitude;
                Vector3 wavePoint = pointOnDirectLine + perpendicularDirection * sineOffset;
                wavePoint.y = segmentStartPoint.y;
                _currentFullPathPoints.Add(wavePoint);
            }
        }
        
        _currentFullPathPoints.Add(new Vector3(targetPoint.x, segmentStartPoint.y, targetPoint.z));
        _currentFullPathPoints.Add(new Vector3(targetPoint.x, segmentStartPoint.y, targetPoint.z));

        if (_currentFullPathPoints.Count >= 4)
        {
            _currentSplineSegmentIndex = 1;
            _splineSegmentProgress = 0f;
            _isActivelyMoving = true;
        }
        else
        {
            _needsNewPathSegmentForRoaming = true; // Fallback if path gen fails
        }
    }


    private void GenerateNextSplinePatrolRouteForRoaming()
    {
        _needsNewPathSegmentForRoaming = false; // Attempting to generate
        _isActivelyMoving = false;

        // ... (Logic to pick nextMainIndex - same as V14's GenerateNextPatrolRoute) ...
        if (!_patrolPointsInitialized || _mainPatrolPoints.Count == 0) { _needsNewPathSegmentForRoaming = true; return; }
        int previousMainIndex = _currentMainPatrolIndex;
        int nextMainIndex = previousMainIndex;
        if (_mainPatrolPoints.Count > 1) { /* ... pick different nextMainIndex ... */ 
            int attempts = 0; do { nextMainIndex = Random.Range(0, _mainPatrolPoints.Count); attempts++; }
            while (nextMainIndex == previousMainIndex && attempts < _mainPatrolPoints.Count * 3);
            if (nextMainIndex == previousMainIndex) nextMainIndex = (previousMainIndex + 1) % _mainPatrolPoints.Count;
        } else { /* ... handle single point pause ... */ 
            nextMainIndex = 0;
            Vector3 singlePointTarget = new Vector3(_mainPatrolPoints[0].x, transform.position.y, _mainPatrolPoints[0].z);
            if (Vector3.Distance(transform.position, singlePointTarget) <= stoppingDistance * 1.1f) {
                _singleWaypointPauseTimer = SINGLE_WAYPOINT_PAUSE_DURATION; _currentFullPathPoints.Clear(); return;
            }
        }
        _currentMainPatrolIndex = nextMainIndex;
        Vector3 mainDestination = _mainPatrolPoints[_currentMainPatrolIndex];

        // Generate the full wave path using the main waveAmplitude and waveFrequency
        GenerateWavePathToPoint(mainDestination); // Re-use the wave generation, but it will use the main config values
    }
    
    // EnableMovement, StopAgentCompletely, OnDestroy, OnDrawGizmosSelected (mostly same as V14, ensure they respect movement style or reset appropriately)
    public void EnableMovement(bool enable)
    {
        if (enable)
        {
            _singleWaypointPauseTimer = 0f;
            // If a path was active (either spline or direct)
            if (!_isActivelyMoving) { // Only try to resume if not already flagged as moving
                if (_currentMovementStyle == MovementStyle.SplineWave && _currentFullPathPoints.Count >=4 && _currentSplineSegmentIndex < _currentFullPathPoints.Count -2) {
                     _isActivelyMoving = true;
                } else if (_currentMovementStyle == MovementStyle.Direct && _directTargetPosition != Vector3.zero) { // Vector3.zero is a poor check for uninitialized
                     if(Vector3.Distance(transform.position, _directTargetPosition) > stoppingDistance * 0.1f) _isActivelyMoving = true;
                }
                // If it needs a new roam path, RoamWaypoints will handle it.
            }
        }
        else 
        {
            _isActivelyMoving = false;
        }
    }

    public void StopAgentCompletely()
    {
        _isActivelyMoving = false;
        _needsNewPathSegmentForRoaming = true; 
        _currentFullPathPoints.Clear();
        _directTargetPosition = Vector3.zero; // Clear direct target too
        _singleWaypointPauseTimer = 0f;
    }

    void OnDestroy()
    {
        if (_spManager != null) _spManager.SpawnPointsInitialized -= InitMainPatrolPoints;
    }

    void OnDrawGizmosSelected() { 
        // ... (Gizmos from V14 - should still be helpful) ...
        if (_currentMovementStyle == MovementStyle.SplineWave && _currentFullPathPoints != null && _currentFullPathPoints.Count > 0) {
            Gizmos.color = Color.cyan;
            for(int i=0; i < _currentFullPathPoints.Count -1; i++) {
                Gizmos.DrawLine(_currentFullPathPoints[i], _currentFullPathPoints[i+1]);
                Gizmos.DrawSphere(_currentFullPathPoints[i], 0.05f);
            }
            if(_currentFullPathPoints.Count > 0) Gizmos.DrawSphere(_currentFullPathPoints.Last(), 0.05f);

            if (_currentFullPathPoints.Count >= 4) {
                 Gizmos.color = Color.magenta;
                 Vector3 prevSplinePoint = GetCatmullRomPosition(0, _currentFullPathPoints[0], _currentFullPathPoints[1], _currentFullPathPoints[2], (_currentFullPathPoints.Count > 3 ? _currentFullPathPoints[3] : _currentFullPathPoints[2]));
                 prevSplinePoint.y = transform.position.y;


                for (int seg = 1; seg < _currentFullPathPoints.Count - 2; seg++) { // Iterate through P1 points
                    Vector3 P0_g = _currentFullPathPoints[seg - 1];
                    Vector3 P1_g = _currentFullPathPoints[seg];
                    Vector3 P2_g = _currentFullPathPoints[seg + 1];
                    Vector3 P3_g = (seg + 2 >= _currentFullPathPoints.Count) ? _currentFullPathPoints.Last() : _currentFullPathPoints[seg + 2];
                    
                    for(float t=0.05f; t <= 1.0f; t += 0.05f) { 
                        Vector3 pointOnSpline = GetCatmullRomPosition(t, P0_g, P1_g, P2_g, P3_g);
                        pointOnSpline.y = transform.position.y; 
                        Gizmos.DrawLine(prevSplinePoint, pointOnSpline);
                        prevSplinePoint = pointOnSpline;
                    }
                }
            }
        } else if (_currentMovementStyle == MovementStyle.Direct && _isActivelyMoving && _directTargetPosition != Vector3.zero) {
             Gizmos.color = Color.green; // Green for direct target
             Gizmos.DrawLine(transform.position, _directTargetPosition);
             Gizmos.DrawSphere(_directTargetPosition, 0.15f);
        }

         if (_isActivelyMoving) {
             Gizmos.color = Color.red; // Agent's current position while moving
             Gizmos.DrawSphere(transform.position, 0.2f);
        }
    }
}