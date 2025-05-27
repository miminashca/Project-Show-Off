using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum MovementStyle // Place this outside the class or in its own file if preferred
{
    Direct,
    SplineWave
}

// MovementStyle Enum should be defined (as in V15)

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
    private MovementStyle _currentMovementStyle = MovementStyle.Direct;
    private Vector3 _directTargetPosition;

    // --- Spline Specific State ---
    private List<Vector3> _currentFullPathPoints = new List<Vector3>();
    private int _currentSplineSegmentIndex;
    private float _splineSegmentProgress;

    // --- General State ---
    private bool _isActivelyMoving = false;
    private bool _needsNewPathSegmentForRoaming = true;

    // Roaming
    private SpawnPointsManager _spManager;
    private List<Vector3> _mainPatrolPoints = new List<Vector3>();
    private int _currentMainPatrolIndex = -1;

    // Single waypoint pause
    private float _singleWaypointPauseTimer = 0f;
    private const float SINGLE_WAYPOINT_PAUSE_DURATION = 2.0f;

    private bool _patrolPointsInitialized = false;
    private const string LOG_PREFIX = "[AgentMovement_V16_Vertical] ";

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
        foreach (SpawnPoint p in _spManager.SpawnPoints) _mainPatrolPoints.Add(p.transform.position); // Store full 3D position
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
                if (_currentFullPathPoints.Count >= 4)
                {
                    FollowSplinePath();
                }
                else
                {
                    ArrivedAtEndOfDirectPath(); // Fallback
                }
            }
            else // MovementStyle.Direct
            {
                MoveDirectlyTowards(_directTargetPosition);
                // Use 3D distance for arrival check if moving in 3D
                if (Vector3.Distance(transform.position, _directTargetPosition) <= stoppingDistance)
                {
                    ArrivedAtEndOfDirectPath();
                }
            }
        }
    }

    // --- Direct Movement Logic (Now 3D) ---
    private void MoveDirectlyTowards(Vector3 targetPosition)
    {
        Vector3 directionToTarget = targetPosition - transform.position; // Full 3D direction

        if (directionToTarget.magnitude < 0.01f) return;

        Vector3 movementThisFrame = directionToTarget.normalized * speed * Time.deltaTime;
        if (movementThisFrame.magnitude > directionToTarget.magnitude)
        {
            movementThisFrame = directionToTarget;
        }
        
        transform.position += movementThisFrame; // Move in 3D

        // Rotation can still be primarily on XZ plane, or aim slightly up/down
        Vector3 lookDirection = directionToTarget.normalized;
        // Optional: If you want XZ-only rotation:
        // Vector3 lookDirectionXZ = new Vector3(lookDirection.x, 0, lookDirection.z).normalized;
        // if (lookDirectionXZ.sqrMagnitude > 0.001f) lookDirection = lookDirectionXZ;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void ArrivedAtEndOfDirectPath()
    {
        if (_directTargetPosition != Vector3.zero)
             transform.position = _directTargetPosition; // Snap to full 3D target
        _isActivelyMoving = false;
    }

    // --- Spline Movement Logic (Now 3D) ---
    private void FollowSplinePath()
    {
        // agentCurrentY is no longer strictly enforced, Y comes from spline points
        float distanceToMoveThisFrame = speed * Time.deltaTime;
        
        Vector3 p1_spline = _currentFullPathPoints[_currentSplineSegmentIndex];
        Vector3 p2_spline = _currentFullPathPoints[_currentSplineSegmentIndex + 1];
        float estimatedSegmentLength = Vector3.Distance(p1_spline, p2_spline); // 3D distance
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

        Vector3 targetSplinePosition = GetCatmullRomPosition(_splineSegmentProgress, P0, P1, P2, P3); // This is a full 3D point

        Vector3 directionToSplineTarget = targetSplinePosition - transform.position; // Full 3D direction

        if (directionToSplineTarget.magnitude < 0.01f) return;

        Vector3 movementThisFrame = directionToSplineTarget.normalized * speed * Time.deltaTime;
        if (movementThisFrame.magnitude > directionToSplineTarget.magnitude)
        {
            movementThisFrame = directionToSplineTarget;
        }

        transform.position += movementThisFrame; // Move in 3D

        // Rotation (same as direct movement, can be 3D or XZ-planar for look)
        Vector3 lookDirection = directionToSplineTarget.normalized;
        // Optional: XZ-only rotation
        // Vector3 lookDirectionXZ = new Vector3(lookDirection.x, 0, lookDirection.z).normalized;
        // if (lookDirectionXZ.sqrMagnitude > 0.001f) lookDirection = lookDirectionXZ;
        
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        Vector3 finalDestinationOnSpline = _currentFullPathPoints.Last(); // Full 3D final point
        if (Vector3.Distance(transform.position, finalDestinationOnSpline) <= stoppingDistance && 
            _currentSplineSegmentIndex >= _currentFullPathPoints.Count - 3)
        {
            ArrivedAtEndOfFullPathSpline();
        }
    }
    
    public static Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) // No change needed
    {
        float t2 = t * t; float t3 = t2 * t;
        return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    private void ArrivedAtEndOfFullPathSpline()
    {
        if (_currentFullPathPoints.Count > 0) {
            Vector3 finalPoint = _currentFullPathPoints.Last();
            transform.position = finalPoint; // Snap to full 3D final point
        }
        _isActivelyMoving = false;
        _needsNewPathSegmentForRoaming = true;
        _currentFullPathPoints.Clear();
    }
    
    public void SetDestination(Vector3 destination, MovementStyle style = MovementStyle.Direct) // destination is full 3D
    {
        _singleWaypointPauseTimer = 0f;
        _currentFullPathPoints.Clear();
        _currentMovementStyle = style;

        if (style == MovementStyle.Direct)
        {
            _directTargetPosition = destination;
            _needsNewPathSegmentForRoaming = false;
            if (Vector3.Distance(transform.position, _directTargetPosition) > stoppingDistance * 0.5f)
            {
                _isActivelyMoving = true;
            }
            else
            {
                _isActivelyMoving = false;
            }
        }
        else // MovementStyle.SplineWave
        {
            GenerateWavePathToPoint(destination); // destination is full 3D
            _needsNewPathSegmentForRoaming = false;
        }
    }

    public void RoamWaypoints()
    {
        if (!_patrolPointsInitialized || _mainPatrolPoints.Count == 0) return;
        if (_singleWaypointPauseTimer > 0f) return;

        if (_needsNewPathSegmentForRoaming && !_isActivelyMoving)
        {
            _currentMovementStyle = MovementStyle.SplineWave;
            GenerateNextSplinePatrolRouteForRoaming();
        }
    }

    // Generates a wave path considering Y values of start and targetPoint
    private void GenerateWavePathToPoint(Vector3 targetPoint) 
    {
        _isActivelyMoving = false;
        _currentFullPathPoints.Clear();
        Vector3 segmentStartPoint = transform.position; // Full 3D start

        _currentFullPathPoints.Add(segmentStartPoint); 
        _currentFullPathPoints.Add(segmentStartPoint); 

        Vector3 pathDirectionXZ = (targetPoint - segmentStartPoint);
        pathDirectionXZ.y = 0; // For XZ distance and perpendicular calculation
        float totalPathDistanceXZ = pathDirectionXZ.magnitude;
        
        float chaseWaveAmplitude = waveAmplitude * 0.75f; // Slightly less amplitude for chase/direct wave
        float chaseWaveFrequency = waveFrequency;

        if (totalPathDistanceXZ > stoppingDistance * 0.5f && chaseWaveAmplitude > 0.01f && wavePathResolution > 0)
        {
            Vector3 pathDirectionNormalizedXZ = pathDirectionXZ.normalized;
            Vector3 perpendicularDirection = Vector3.Cross(pathDirectionNormalizedXZ, Vector3.up).normalized;
            if (Random.value > 0.5f) perpendicularDirection *= -1f;

            for (int i = 1; i <= wavePathResolution - 1; i++)
            {
                float t = (float)i / wavePathResolution;
                // Interpolate XZ along the direct line
                Vector3 pointOnDirectLineXZ = segmentStartPoint + pathDirectionNormalizedXZ * (t * totalPathDistanceXZ);
                // Interpolate Y linearly between start and target Y
                float currentY = Mathf.Lerp(segmentStartPoint.y, targetPoint.y, t);

                float sineOffset = Mathf.Sin(t * chaseWaveFrequency * 2f * Mathf.PI) * chaseWaveAmplitude;
                
                Vector3 wavePoint = new Vector3(
                    pointOnDirectLineXZ.x + perpendicularDirection.x * sineOffset,
                    currentY,
                    pointOnDirectLineXZ.z + perpendicularDirection.z * sineOffset
                );
                _currentFullPathPoints.Add(wavePoint);
            }
        }
        
        _currentFullPathPoints.Add(targetPoint); // Use the full 3D targetPoint
        _currentFullPathPoints.Add(targetPoint); // Pad end

        if (_currentFullPathPoints.Count >= 4)
        {
            _currentSplineSegmentIndex = 1;
            _splineSegmentProgress = 0f;
            _isActivelyMoving = true;
        }
        else
        {
            _needsNewPathSegmentForRoaming = true;
        }
    }

    private void GenerateNextSplinePatrolRouteForRoaming()
    {
        _needsNewPathSegmentForRoaming = false;
        _isActivelyMoving = false;

        if (!_patrolPointsInitialized || _mainPatrolPoints.Count == 0) { _needsNewPathSegmentForRoaming = true; return; }
        int previousMainIndex = _currentMainPatrolIndex;
        int nextMainIndex = previousMainIndex;
        if (_mainPatrolPoints.Count > 1) { 
            int attempts = 0; do { nextMainIndex = Random.Range(0, _mainPatrolPoints.Count); attempts++; }
            while (nextMainIndex == previousMainIndex && attempts < _mainPatrolPoints.Count * 3);
            if (nextMainIndex == previousMainIndex) nextMainIndex = (previousMainIndex + 1) % _mainPatrolPoints.Count;
        } else { 
            nextMainIndex = 0;
            // Compare Y of main patrol point with current agent Y for single point pause
            if (Mathf.Abs(transform.position.y - _mainPatrolPoints[0].y) < stoppingDistance * 0.5f && // Check Y proximity
                Vector3.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(_mainPatrolPoints[0].x, _mainPatrolPoints[0].z)) <= stoppingDistance * 1.1f) // Check XZ proximity
            {
                _singleWaypointPauseTimer = SINGLE_WAYPOINT_PAUSE_DURATION; _currentFullPathPoints.Clear(); return;
            }
        }
        _currentMainPatrolIndex = nextMainIndex;
        Vector3 mainDestination = _mainPatrolPoints[_currentMainPatrolIndex]; // This is full 3D

        // Generate wave path using the main configured waveAmplitude and waveFrequency from current pos to mainDestination
        GenerateWavePathToPoint(mainDestination); 
    }
    
    // EnableMovement, StopAgentCompletely, OnDestroy, OnDrawGizmosSelected (mostly same as V15)
    public void EnableMovement(bool enable)
    {
        if (enable)
        {
            _singleWaypointPauseTimer = 0f;
            if (!_isActivelyMoving) {
                if (_currentMovementStyle == MovementStyle.SplineWave && _currentFullPathPoints.Count >=4 && _currentSplineSegmentIndex < _currentFullPathPoints.Count -2) {
                     _isActivelyMoving = true;
                } else if (_currentMovementStyle == MovementStyle.Direct && _directTargetPosition != Vector3.zero) {
                     if(Vector3.Distance(transform.position, _directTargetPosition) > stoppingDistance * 0.1f) _isActivelyMoving = true;
                }
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
        _directTargetPosition = Vector3.zero;
        _singleWaypointPauseTimer = 0f;
    }

    void OnDestroy()
    {
        if (_spManager != null) _spManager.SpawnPointsInitialized -= InitMainPatrolPoints;
    }

    void OnDrawGizmosSelected() { 
        // Gizmos should still work, points are now 3D
        if (_currentMovementStyle == MovementStyle.SplineWave && _currentFullPathPoints != null && _currentFullPathPoints.Count > 0) {
            Gizmos.color = Color.cyan;
            for(int i=0; i < _currentFullPathPoints.Count -1; i++) {
                Gizmos.DrawLine(_currentFullPathPoints[i], _currentFullPathPoints[i+1]);
                Gizmos.DrawSphere(_currentFullPathPoints[i], 0.05f);
            }
            if(_currentFullPathPoints.Count > 0) Gizmos.DrawSphere(_currentFullPathPoints.Last(), 0.05f);

            if (_currentFullPathPoints.Count >= 4) {
                 Gizmos.color = Color.magenta;
                 // Draw first point of the very first conceptual segment for prevSplinePoint init
                 Vector3 P0_first = _currentFullPathPoints[0];
                 Vector3 P1_first = _currentFullPathPoints[1];
                 Vector3 P2_first = _currentFullPathPoints[2];
                 Vector3 P3_first = (_currentFullPathPoints.Count > 3 ? _currentFullPathPoints[3] : _currentFullPathPoints[2]);
                 Vector3 prevSplinePoint = GetCatmullRomPosition(0, P0_first, P1_first, P2_first, P3_first);

                for (int seg = 1; seg < _currentFullPathPoints.Count - 2; seg++) {
                    Vector3 P0_g = _currentFullPathPoints[seg - 1];
                    Vector3 P1_g = _currentFullPathPoints[seg];
                    Vector3 P2_g = _currentFullPathPoints[seg + 1];
                    Vector3 P3_g = (seg + 2 >= _currentFullPathPoints.Count) ? _currentFullPathPoints.Last() : _currentFullPathPoints[seg + 2];
                    
                    for(float t=0.05f; t <= 1.0f; t += 0.05f) { 
                        Vector3 pointOnSpline = GetCatmullRomPosition(t, P0_g, P1_g, P2_g, P3_g);
                        Gizmos.DrawLine(prevSplinePoint, pointOnSpline);
                        prevSplinePoint = pointOnSpline;
                    }
                }
            }
        } else if (_currentMovementStyle == MovementStyle.Direct && _isActivelyMoving && _directTargetPosition != Vector3.zero) {
             Gizmos.color = Color.green;
             Gizmos.DrawLine(transform.position, _directTargetPosition);
             Gizmos.DrawSphere(_directTargetPosition, 0.15f);
        }

         if (_isActivelyMoving) {
             Gizmos.color = Color.red;
             Gizmos.DrawSphere(transform.position, 0.2f);
        }
    }
}