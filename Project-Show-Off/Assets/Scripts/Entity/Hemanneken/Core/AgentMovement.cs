using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum MovementStyle
{
    Direct,
    SplineWave,
    Hop 
}

// Ensure MovementStyle enum is defined (e.g., in its own file or above this class)
// public enum MovementStyle
// {
//    Direct,
//    SplineWave
// }

// Add these fields to your HemannekenAIConfig class:
// namespace YourNamespace // If you use one for HemannekenAIConfig
// {
//     [CreateAssetMenu(fileName = "NewHemannekenAIConfig", menuName = "AI/Hemanneken AI Config")]
//     public class HemannekenAIConfig : ScriptableObject
//     {
//         // ... your existing fields ...
//         [Header("Movement - Ground Restriction")]
//         public LayerMask groundLayerMask = 1; // Default to layer 0 "Default". Configure this!
//         public float groundOffset = 0.1f;
//         public float groundRaycastMaxDistance = 20f;
//         public float groundRaycastStartHeightOffset = 1f;
//         public bool defaultRoamOnGround = false;
//         // ... other fields ...
//     }
// }


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

    // These will be set by Initialize from HemannekenAIConfig
    private LayerMask _groundLayerMask;
    private float _groundOffset;
    private float _groundRaycastMaxDistance;
    private float _groundRaycastStartHeightOffset;

    // --- Internal State ---
    private MovementStyle _currentMovementStyle = MovementStyle.Direct;
    private Vector3 _directTargetPosition;

    // --- Spline Specific State ---
    private List<Vector3> _currentFullPathPoints = new List<Vector3>();
    private int _currentSplineSegmentIndex;
    private float _splineSegmentProgress;
    
    // --- Hop Specific State ---
    private Vector3 _currentHopSeriesTargetWaypoint; // The main waypoint we are hopping towards in a series
    private Vector3 _currentSingleHopTargetPosition; // The target for the current individual hop action
    private float _hopWaitTimer = 0f;                // Timer for the waiting phase between hops
    private bool _isCurrentlyMidHop = false;         // True if the agent is in the movement part of a hop

    // --- General State ---
    private bool _isActivelyMoving = false;
    private bool _needsNewPathSegmentForRoaming = true;
    private bool _isGroundRestricted = false; // Controls if Y axis is snapped to ground

    // Roaming
    private SpawnPointsManager _spManager;
    private List<Vector3> _mainPatrolPoints = new List<Vector3>();
    private int _currentMainPatrolIndex = -1;

    // Single waypoint pause
    private float _singleWaypointPauseTimer = 0f;
    private const float SINGLE_WAYPOINT_PAUSE_DURATION = 2.0f;

    private HemannekenAIConfig aiConfig;

    private bool _patrolPointsInitialized = false;
    private const string LOG_PREFIX = "[AgentMovement_V17_Ground] ";

    // Assume HemannekenAIConfig is defined elsewhere and has the new ground-related fields
    public void Initialize(SpawnPointsManager spawnPointsManager, HemannekenAIConfig pAiConfig)
    {
        aiConfig = pAiConfig;
        
        speed = aiConfig.defaultSpeed;
        rotationSpeed = aiConfig.rotationSpeed;
        stoppingDistance = aiConfig.stoppingDistance;
        waveAmplitude = aiConfig.waveAmplitude;
        waveFrequency = aiConfig.waveFrequency;
        wavePathResolution = aiConfig.wavePathResolution;

        _groundLayerMask = aiConfig.groundLayerMask;
        _groundOffset = aiConfig.groundOffset;
        _groundRaycastMaxDistance = aiConfig.groundRaycastMaxDistance;
        _groundRaycastStartHeightOffset = aiConfig.groundRaycastStartHeightOffset;
        SetGroundRestriction(aiConfig.defaultRoamOnGround);

        _spManager = spawnPointsManager;
        if (_spManager != null)
        {
            Debug.Log("Found partol points manager for " + gameObject.name + " with " + _spManager.SecondarySpawnPoints.Count + " patrol points.");
            if (_spManager.SecondarySpawnPoints != null && _spManager.SecondarySpawnPoints.Count > 0 && !_patrolPointsInitialized)
            {
                Debug.Log("Initializing partol points for " + gameObject.name);
                InitMainPatrolPoints(_spManager.SecondarySpawnPoints);
            }
        }
    }

    private void InitMainPatrolPoints(List<SpawnPoint> patrolPoints)
    {
        _mainPatrolPoints.Clear();
       
        foreach (SpawnPoint p in patrolPoints)
        {
            if (p != null) _mainPatrolPoints.Add(p.transform.position);
        }
        _patrolPointsInitialized = _mainPatrolPoints.Count > 0;
        _needsNewPathSegmentForRoaming = true;

        // if(!_patrolPointsInitialized) Debug.LogWarning(LOG_PREFIX + gameObject.name + ": Patrol points initialization failed or resulted in zero points.");
    }

    public void SetGroundRestriction(bool restricted)
    {
        _isGroundRestricted = restricted;
    }

    private float ProjectToGroundY(float x, float z, float referenceY, float fallbackY)
    {
        Vector3 rayStart = new Vector3(x, referenceY + _groundRaycastStartHeightOffset, z);
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, _groundRaycastMaxDistance, _groundLayerMask))
        {
            return hit.point.y + _groundOffset;
        }
        return fallbackY;
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

        // Inside your Update() method:
// ... (other update logic like _singleWaypointPauseTimer) ...

        if (_isActivelyMoving)
        {
            Vector3 preMovePosition = transform.position;
            if (_currentMovementStyle == MovementStyle.SplineWave)
            {
                FollowSplinePath();
            }
            else if (_currentMovementStyle == MovementStyle.Hop) // <-- ADD THIS BLOCK
            {
                HandleHopMovement();
            }
            else // MovementStyle.Direct
            {
                MoveDirectlyTowards(_directTargetPosition);
                if (Vector3.Distance(transform.position, _directTargetPosition) <= stoppingDistance)
                {
                    ArrivedAtEndOfDirectPath();
                }
            }

            // Apply ground snapping after movement logic if restricted and still considered moving
            if (_isGroundRestricted && _isActivelyMoving)
            {
                // Only snap if position actually changed
                if (transform.position != preMovePosition) {
                    float currentY = transform.position.y;
                    float groundY = ProjectToGroundY(transform.position.x, transform.position.z, currentY, currentY);
                    transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
                }
            }
        }
    }
    
    public void RoamWaypoints(MovementStyle style)
    {
        _currentMovementStyle = style; // Set the style for this roaming session
        // Decide on ground restriction: either use current, or set a default, or add another parameter
        // For simplicity, let's assume it uses the current _isGroundRestricted setting.
        RoamWaypointsInternal();
    }

    public void RoamWaypoints(MovementStyle style, bool groundRestricted)
    {
        _currentMovementStyle = style; // Set the style for this roaming session
        SetGroundRestriction(groundRestricted);
        RoamWaypointsInternal();
    }

    private void MoveDirectlyTowards(Vector3 targetPosition)
    {
        Vector3 directionToTarget = targetPosition - transform.position;
        if (directionToTarget.magnitude < 0.01f) return;

        Vector3 movementThisFrame = directionToTarget.normalized * speed * Time.deltaTime;
        if (movementThisFrame.magnitude > directionToTarget.magnitude)
        {
            movementThisFrame = directionToTarget;
        }
        
        transform.position += movementThisFrame;

        Vector3 lookDirection = directionToTarget.normalized;
        if (_isGroundRestricted)
        {
            lookDirection.y = 0;
            if (lookDirection.sqrMagnitude < 0.001f && directionToTarget.sqrMagnitude > 0.001f) // Avoid zero if target is directly up/down but still away
            {
                 lookDirection = transform.forward; // Maintain current forward if look direction is vertical
            }
            else if (lookDirection.sqrMagnitude < 0.001f) // if target is *really* close / on top
            {
                return; // No clear look direction
            }
        }

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void ArrivedAtEndOfDirectPath()
    {
        Vector3 finalPos = _directTargetPosition;
        if (_isGroundRestricted && _directTargetPosition != Vector3.zero)
        {
            // Project final destination Y to ground using its own Y as reference
            finalPos.y = ProjectToGroundY(finalPos.x, finalPos.z, _directTargetPosition.y, _directTargetPosition.y);
        }
        if (_directTargetPosition != Vector3.zero) transform.position = finalPos;

        _isActivelyMoving = false;
        _needsNewPathSegmentForRoaming = true;
    }

    private void FollowSplinePath()
    {
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
        Vector3 directionToSplineTarget = targetSplinePosition - transform.position;

        if (directionToSplineTarget.magnitude < 0.01f) return;

        Vector3 movementThisFrame = directionToSplineTarget.normalized * speed * Time.deltaTime;
        if (movementThisFrame.magnitude > directionToSplineTarget.magnitude)
        {
            movementThisFrame = directionToSplineTarget;
        }
        transform.position += movementThisFrame;

        Vector3 lookDirection = directionToSplineTarget.normalized;
         if (_isGroundRestricted)
        {
            lookDirection.y = 0;
             if (lookDirection.sqrMagnitude < 0.001f && directionToSplineTarget.sqrMagnitude > 0.001f)
            {
                 lookDirection = transform.forward;
            }
            else if (lookDirection.sqrMagnitude < 0.001f)
            {
                return; 
            }
        }
        
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        Vector3 finalDestinationOnSpline = _currentFullPathPoints.Last();
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
             if (_isGroundRestricted) // Final snap should respect ground restriction
            {
                // Project final point Y using its own Y as reference
                finalPoint.y = ProjectToGroundY(finalPoint.x, finalPoint.z, finalPoint.y, finalPoint.y);
            }
            transform.position = finalPoint;
        }
        _isActivelyMoving = false;
        _needsNewPathSegmentForRoaming = true;
        _currentFullPathPoints.Clear();
    }
    
    public void SetDestination(Vector3 destination, MovementStyle style = MovementStyle.Direct)
    {
        _singleWaypointPauseTimer = 0f;
        _currentFullPathPoints.Clear();
        _currentMovementStyle = style;

        Vector3 finalDestination = destination;
        if (_isGroundRestricted)
        {
            // Project destination Y using its own Y as reference
            finalDestination.y = ProjectToGroundY(destination.x, destination.z, destination.y, destination.y);
        }

        if (style == MovementStyle.Direct)
        {
            _directTargetPosition = finalDestination;
            _needsNewPathSegmentForRoaming = false;
            if (Vector3.Distance(transform.position, _directTargetPosition) > stoppingDistance * 0.5f)
            {
                _isActivelyMoving = true;
            }
            else
            {
                _isActivelyMoving = false;
                ArrivedAtEndOfDirectPath();
            }
        }
        else if (style == MovementStyle.Hop) // <-- ADD THIS BLOCK
        {
            _currentHopSeriesTargetWaypoint = finalDestination;
            _needsNewPathSegmentForRoaming = false; // Explicit destination

            if (Vector3.Distance(transform.position, _currentHopSeriesTargetWaypoint) > stoppingDistance)
            {
                _isActivelyMoving = true;
                _isCurrentlyMidHop = true; // Start by waiting or preparing the first hop
                _hopWaitTimer = 0f;    // Set a tiny timer to trigger the first hop planning cycle in HandleHopMovement
                // Or set to `hopWaitDuration` to wait fully before the first hop.
            }
            else
            {
                _isActivelyMoving = false; // Already at destination
                ArrivedAtHopSeriesTarget();    // Call arrival logic
            }
        }
        else // MovementStyle.SplineWave
        {
            GenerateWavePathToPoint(finalDestination); // finalDestination Y is now processed
            _needsNewPathSegmentForRoaming = false;
        }
    }

    public void RoamWaypoints()
    {
        RoamWaypointsInternal();
    }

    public void RoamWaypoints(bool groundRestricted)
    {
        SetGroundRestriction(groundRestricted);
        RoamWaypointsInternal();
    }

    private void RoamWaypointsInternal()
    {
        if (!_patrolPointsInitialized || _mainPatrolPoints.Count == 0) {
            _needsNewPathSegmentForRoaming = true;
            return;
        }
        if (_singleWaypointPauseTimer > 0f) return;
        
        if (_needsNewPathSegmentForRoaming && !_isActivelyMoving) {
            if (_currentMovementStyle == MovementStyle.SplineWave)
            {
                GenerateNextSplinePatrolRouteForRoaming();
            }
            else if (_currentMovementStyle == MovementStyle.Hop) // <-- ADD THIS
            {
                GenerateNextHopSeriesTargetForRoaming();
            }
        }
    }
    
    private void GenerateWavePathToPoint(Vector3 targetPoint) // targetPoint Y is assumed to be appropriately set by caller
    {
        _isActivelyMoving = false;
        _currentFullPathPoints.Clear();

        Vector3 segmentStartPoint = transform.position;
        if (_isGroundRestricted)
        {
            // Project current agent position Y using its own Y as reference
            segmentStartPoint.y = ProjectToGroundY(segmentStartPoint.x, segmentStartPoint.z, segmentStartPoint.y, segmentStartPoint.y);
        }

        _currentFullPathPoints.Add(segmentStartPoint); 
        _currentFullPathPoints.Add(segmentStartPoint); 

        Vector3 pathDirectionXZ = (targetPoint - segmentStartPoint);
        pathDirectionXZ.y = 0; 
        float totalPathDistanceXZ = pathDirectionXZ.magnitude;
        
        float currentWaveAmplitude = waveAmplitude;
        float currentWaveFrequency = waveFrequency;

        if (totalPathDistanceXZ > stoppingDistance * 0.5f && currentWaveAmplitude > 0.01f && wavePathResolution > 1)
        {
            Vector3 pathDirectionNormalizedXZ = pathDirectionXZ.normalized;
            Vector3 perpendicularDirection = Vector3.Cross(pathDirectionNormalizedXZ, Vector3.up).normalized;
            if (Random.value > 0.5f) perpendicularDirection *= -1f;

            for (int i = 1; i <= wavePathResolution -1; i++)
            {
                float t = (float)i / wavePathResolution;
                Vector3 pointOnDirectLineXZ = segmentStartPoint + pathDirectionNormalizedXZ * (t * totalPathDistanceXZ);
                float sineOffset = Mathf.Sin(t * currentWaveFrequency * 2f * Mathf.PI) * currentWaveAmplitude;
                Vector3 waveOffsetPlanar = perpendicularDirection * sineOffset;

                float wavePointX = pointOnDirectLineXZ.x + waveOffsetPlanar.x;
                float wavePointZ = pointOnDirectLineXZ.z + waveOffsetPlanar.z;
                float wavePointY;

                if (_isGroundRestricted)
                {
                    float lerpedGroundYFallback = Mathf.Lerp(segmentStartPoint.y, targetPoint.y, t);
                    // Project wavy XZ point Y using the lerped Y (between grounded start/target) as reference
                    wavePointY = ProjectToGroundY(wavePointX, wavePointZ, lerpedGroundYFallback, lerpedGroundYFallback);
                }
                else
                {
                    wavePointY = Mathf.Lerp(segmentStartPoint.y, targetPoint.y, t);
                }
                
                _currentFullPathPoints.Add(new Vector3(wavePointX, wavePointY, wavePointZ));
            }
        }
        
        _currentFullPathPoints.Add(targetPoint);
        _currentFullPathPoints.Add(targetPoint);

        if (_currentFullPathPoints.Count >= 4)
        {
            _currentSplineSegmentIndex = 1;
            _splineSegmentProgress = 0f;
            _isActivelyMoving = true;
        }
        else
        {
            // Fallback for very short paths or insufficient resolution
            _currentMovementStyle = MovementStyle.Direct;
            _directTargetPosition = targetPoint; // Use the already processed targetPoint
             if (Vector3.Distance(transform.position, _directTargetPosition) > stoppingDistance * 0.5f)
            {
                _isActivelyMoving = true;
            } else {
                 ArrivedAtEndOfDirectPath();
            }
        }
    }

    private void GenerateNextSplinePatrolRouteForRoaming()
    {
        _needsNewPathSegmentForRoaming = false;
        _isActivelyMoving = false;

        if (!_patrolPointsInitialized || _mainPatrolPoints.Count == 0) {
            _needsNewPathSegmentForRoaming = true;
            return;
        }

        int previousMainIndex = _currentMainPatrolIndex;
        int nextMainIndex = previousMainIndex;

        if (_mainPatrolPoints.Count > 1) { 
            int attempts = 0; 
            do { 
                nextMainIndex = Random.Range(0, _mainPatrolPoints.Count); 
                attempts++; 
            }
            while (nextMainIndex == previousMainIndex && attempts < _mainPatrolPoints.Count * 3);

            if (nextMainIndex == previousMainIndex)
                nextMainIndex = (previousMainIndex + 1) % _mainPatrolPoints.Count;
        } else { 
            nextMainIndex = 0;
            Vector3 singlePatrolPoint = _mainPatrolPoints[0];
            float singlePatrolPointEffectiveY = singlePatrolPoint.y;
            if (_isGroundRestricted)
            {
                // Project patrol point Y using its own Y as reference
                singlePatrolPointEffectiveY = ProjectToGroundY(singlePatrolPoint.x, singlePatrolPoint.z, singlePatrolPoint.y, singlePatrolPoint.y);
            }

            // Use current agent's Y (which should be grounded if restricted) for comparison
            float agentCurrentY = transform.position.y;
            if (_isGroundRestricted) // Ensure agent's Y is fresh from ground for this check
            {
                agentCurrentY = ProjectToGroundY(transform.position.x, transform.position.z, transform.position.y, transform.position.y);
            }


            bool yCloseEnough = Mathf.Abs(agentCurrentY - singlePatrolPointEffectiveY) < stoppingDistance * 0.75f;
            bool xzCloseEnough = Vector3.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(singlePatrolPoint.x, singlePatrolPoint.z)) <= stoppingDistance * 1.1f;

            if (yCloseEnough && xzCloseEnough)
            {
                _singleWaypointPauseTimer = SINGLE_WAYPOINT_PAUSE_DURATION;
                _currentFullPathPoints.Clear();
                _isActivelyMoving = false;
                return;
            }
        }
        _currentMainPatrolIndex = nextMainIndex;
        Vector3 mainDestination = _mainPatrolPoints[_currentMainPatrolIndex];

        if (_isGroundRestricted)
        {
            // Project destination Y using its own Y as reference
            mainDestination.y = ProjectToGroundY(mainDestination.x, mainDestination.z, mainDestination.y, mainDestination.y);
        }
        
        GenerateWavePathToPoint(mainDestination);
    }
    
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
                } else {
                    _needsNewPathSegmentForRoaming = true; // No current path, flag for new roam path if RoamWaypoints is called
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

    void OnDrawGizmosSelected() { 
        if (!Application.isPlaying) return;

        if (_currentMovementStyle == MovementStyle.SplineWave && _currentFullPathPoints != null && _currentFullPathPoints.Count > 0) {
            Gizmos.color = Color.cyan;
            for(int i=0; i < _currentFullPathPoints.Count -1; i++) {
                Gizmos.DrawLine(_currentFullPathPoints[i], _currentFullPathPoints[i+1]);
                Gizmos.DrawSphere(_currentFullPathPoints[i], 0.05f);
            }
            if(_currentFullPathPoints.Count > 0) Gizmos.DrawSphere(_currentFullPathPoints.Last(), 0.05f);

            if (_currentFullPathPoints.Count >= 4) {
                 Gizmos.color = Color.magenta;
                 Vector3 prevSplinePoint = GetCatmullRomPosition(0, _currentFullPathPoints[0], _currentFullPathPoints[1], _currentFullPathPoints[2], 
                                                               (_currentFullPathPoints.Count > 3 ? _currentFullPathPoints[3] : _currentFullPathPoints[2]));

                for (int seg = 1; seg < _currentFullPathPoints.Count - 2; seg++) {
                    Vector3 P0_g = _currentFullPathPoints[seg - 1];
                    Vector3 P1_g = _currentFullPathPoints[seg];
                    Vector3 P2_g = _currentFullPathPoints[seg + 1];
                    Vector3 P3_g = (seg + 2 >= _currentFullPathPoints.Count) ? _currentFullPathPoints.Last() : _currentFullPathPoints[seg + 2];
                    
                    int gizmoResolution = 20;
                    for(int t_step = 1; t_step <= gizmoResolution; t_step++) { 
                        float t = (float)t_step / gizmoResolution;
                        Vector3 pointOnSpline = GetCatmullRomPosition(t, P0_g, P1_g, P2_g, P3_g);
                        Gizmos.DrawLine(prevSplinePoint, pointOnSpline);
                        prevSplinePoint = pointOnSpline;
                    }
                }
            }
        } else if (_currentMovementStyle == MovementStyle.Direct && _directTargetPosition != Vector3.zero) {
             Gizmos.color = Color.green;
             Gizmos.DrawLine(transform.position, _directTargetPosition);
             Gizmos.DrawSphere(_directTargetPosition, 0.15f);
        }

         if (_isActivelyMoving) {
             Gizmos.color = Color.red;
             Gizmos.DrawSphere(transform.position, 0.2f);
        }

        if (_isGroundRestricted)
        {
            Gizmos.color = Color.yellow;
            float refY = transform.position.y; // Use current Y as reference for gizmo
            Vector3 rayStart = new Vector3(transform.position.x, refY + _groundRaycastStartHeightOffset, transform.position.z);
            Vector3 rayEnd = rayStart + Vector3.down * _groundRaycastMaxDistance;
            Gizmos.DrawLine(rayStart, rayEnd);
            
            float groundY = ProjectToGroundY(transform.position.x, transform.position.z, refY, transform.position.y);
            Gizmos.DrawWireSphere(new Vector3(transform.position.x, groundY, transform.position.z), 0.1f);
        }
    }
    
    public void HandleHopMovement()
    {
        if (_currentHopSeriesTargetWaypoint == Vector3.zero && _isActivelyMoving) {
            // This case should ideally be prevented by StopAgentCompletely or similar
            // ensuring _currentHopSeriesTargetWaypoint is set before _isActivelyMoving is true for Hop.
            // Debug.LogWarning(LOG_PREFIX + gameObject.name + ": Hop movement active with no target. Stopping.");
            StopAgentCompletely(); // Safety stop
            return;
        }

        if (_hopWaitTimer > 0f) // Phase 1: Waiting between hops
        {
            _hopWaitTimer -= Time.deltaTime;

            // Optional: Implement rotation towards the main target during wait
            if (_currentHopSeriesTargetWaypoint != Vector3.zero)
            {
                Vector3 dirToFinalTarget = _currentHopSeriesTargetWaypoint - transform.position;
            
                // If ground restricted, only consider XZ direction for rotation
                // Otherwise, allow looking up/down towards the target.
                if (_isGroundRestricted)
                {
                    dirToFinalTarget.y = 0;
                }

                // Only rotate if there's a meaningful direction
                if (dirToFinalTarget.sqrMagnitude > 0.001f) // Use a small threshold to avoid issues with zero vectors
                {
                    Quaternion targetRot = Quaternion.LookRotation(dirToFinalTarget.normalized);
                
                    // --- CHANGE THIS LINE FOR ROTATION SPEED ---
                    // For fast rotation, use the full rotationSpeed or a large fraction of it.
                    // Example 1: Full speed
                    // transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                
                    // Example 2: Very fast, maybe even faster if needed (e.g., double speed)
                    // transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, (rotationSpeed * 2.0f) * Time.deltaTime);

                    // Let's go with full rotationSpeed for "fast"
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                }
            }


            if (_hopWaitTimer <= 0f)
            {
                // ... (rest of the wait timer expired logic)
            }


            if (_hopWaitTimer <= 0f)
            {
                // Wait finished. Check if we've arrived at the main target or need another hop.
                float distanceToFinalTarget = Vector3.Distance(transform.position, _currentHopSeriesTargetWaypoint);
                if (distanceToFinalTarget <= stoppingDistance)
                {
                    ArrivedAtHopSeriesTarget();
                }
                else
                {
                    // Prepare for the next hop
                    _isCurrentlyMidHop = true;
                    Vector3 directionToWaypoint = (_currentHopSeriesTargetWaypoint - transform.position).normalized;
                    
                    // Ensure we don't overshoot the final target in this single hop
                    float hopTravelDistance = Mathf.Min(aiConfig.hopDistance, distanceToFinalTarget);
                    _currentSingleHopTargetPosition = transform.position + directionToWaypoint * hopTravelDistance;

                    if (_isGroundRestricted)
                    {
                        // Project the immediate hop target point to the ground.
                        // Use the hop target's current Y as a reference for the raycast start height.
                        _currentSingleHopTargetPosition.y = ProjectToGroundY(
                            _currentSingleHopTargetPosition.x,
                            _currentSingleHopTargetPosition.z,
                            _currentSingleHopTargetPosition.y, // Reference Y for raycast
                            _currentSingleHopTargetPosition.y  // Fallback Y if no ground found
                        );
                    }
                }
            }
        }
        else if (_isCurrentlyMidHop) // Phase 2: Actively hopping
        {
            Vector3 directionToSingleHopTarget = _currentSingleHopTargetPosition - transform.position;

            // Check if we've reached the target of this single hop
            if (directionToSingleHopTarget.magnitude <= stoppingDistance * 0.5f) // Use a smaller threshold for individual hops
            {
                transform.position = _currentSingleHopTargetPosition; // Snap to exact hop target
                _isCurrentlyMidHop = false;
                _hopWaitTimer = aiConfig.hopWaitDuration; // Start waiting period

                // After completing a hop, re-check distance to the *final* series target
                if (Vector3.Distance(transform.position, _currentHopSeriesTargetWaypoint) <= stoppingDistance)
                {
                    ArrivedAtHopSeriesTarget(); // Arrived at overall target
                }
                return; // Exit, wait processing will happen next frame
            }

            // Move towards the single hop target
            Vector3 movementThisFrame = directionToSingleHopTarget.normalized * aiConfig.hopSpeed * Time.deltaTime;
            if (movementThisFrame.magnitude > directionToSingleHopTarget.magnitude)
            {
                movementThisFrame = directionToSingleHopTarget; // Prevent overshooting
            }
            transform.position += movementThisFrame;

            // Rotation during the hop (face the direction of the current hop)
            Vector3 lookDirection = directionToSingleHopTarget.normalized;
            if (_isGroundRestricted)
            {
                lookDirection.y = 0;
                // Avoid zero vector if looking straight up/down but still moving horizontally
                if (lookDirection.sqrMagnitude < 0.001f && directionToSingleHopTarget.y != 0 && (directionToSingleHopTarget.x != 0 || directionToSingleHopTarget.z != 0))
                {
                     lookDirection = new Vector3(directionToSingleHopTarget.x, 0, directionToSingleHopTarget.z).normalized;
                }
                else if (lookDirection.sqrMagnitude < 0.001f) // Truly no horizontal direction
                {
                     // Maintain current forward if look direction is purely vertical or zero
                     lookDirection = transform.forward;
                     lookDirection.y = 0; // Ensure it's flat if using current forward
                     if(lookDirection.sqrMagnitude < 0.001f) lookDirection = Vector3.forward; // Absolute fallback
                }
            }
            
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // After moving, re-check distance to the *final* series target, in case this hop reached it
            if (Vector3.Distance(transform.position, _currentHopSeriesTargetWaypoint) <= stoppingDistance)
            {
                 ArrivedAtHopSeriesTarget();
            }
        }
        // If not waiting and not mid-hop (e.g., _hopWaitTimer <= 0 and !_isCurrentlyMidHop):
        // This state is typically the very start of a hop sequence.
        // RoamWaypointsInternal or SetDestination should have set _hopWaitTimer to a small positive
        // value (like 0.01f) or hopWaitDuration to kick off the cycle.
        // If HandleHopMovement is entered in this state, it means the timer just expired (handled above)
        // or it's the very first frame of a newly set hop destination.
        // The logic above (if _hopWaitTimer <= 0f) will then prepare the first hop.
    }

    // Call this when the agent arrives at the main waypoint after a series of hops
    private void ArrivedAtHopSeriesTarget()
    {
        Vector3 finalPos = _currentHopSeriesTargetWaypoint;
        // The Y of _currentHopSeriesTargetWaypoint should have been projected to ground when it was set.
        if (_currentHopSeriesTargetWaypoint != Vector3.zero)
        {
            transform.position = finalPos; // Snap to the final target position
        }

        _isActivelyMoving = false;
        _isCurrentlyMidHop = false;
        _needsNewPathSegmentForRoaming = true; // Signal that a new roaming target is needed
        _hopWaitTimer = 0f; // Reset timer
        _currentHopSeriesTargetWaypoint = Vector3.zero; // Clear target

        // If roaming with a single patrol point, the _singleWaypointPauseTimer logic
        // in GenerateNextHopSeriesTargetForRoaming will handle the pause.
    }
    
    private void GenerateNextHopSeriesTargetForRoaming()
    {
        _needsNewPathSegmentForRoaming = false;
        // _isActivelyMoving will be set true below if a valid target is found

        if (!_patrolPointsInitialized || _mainPatrolPoints.Count == 0) {
            _needsNewPathSegmentForRoaming = true;
            _isActivelyMoving = false;
            return;
        }

        int previousMainIndex = _currentMainPatrolIndex;
        int nextMainIndex = previousMainIndex;

        if (_mainPatrolPoints.Count > 1) {
            int attempts = 0;
            do {
                nextMainIndex = Random.Range(0, _mainPatrolPoints.Count);
                attempts++;
            } // Ensure it tries to pick a different point, with a limit on attempts
            while (nextMainIndex == previousMainIndex && attempts < _mainPatrolPoints.Count * 3);

            if (nextMainIndex == previousMainIndex) { // If still same (e.g., 2 points and randomly picked same)
                nextMainIndex = (previousMainIndex + 1) % _mainPatrolPoints.Count;
            }
        } else { // Single patrol point logic
            nextMainIndex = 0;
            Vector3 singlePatrolPoint = _mainPatrolPoints[0];
            float singlePatrolPointEffectiveY = singlePatrolPoint.y;
            if (_isGroundRestricted)
            {
                singlePatrolPointEffectiveY = ProjectToGroundY(singlePatrolPoint.x, singlePatrolPoint.z, singlePatrolPoint.y, singlePatrolPoint.y);
            }

            float agentCurrentY = transform.position.y;
             if (_isGroundRestricted) // Ensure agent's Y is fresh from ground for this check
            {
                agentCurrentY = ProjectToGroundY(transform.position.x, transform.position.z, transform.position.y, transform.position.y);
            }

            bool yCloseEnough = Mathf.Abs(agentCurrentY - singlePatrolPointEffectiveY) < stoppingDistance * 0.75f;
            bool xzCloseEnough = Vector3.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(singlePatrolPoint.x, singlePatrolPoint.z)) <= stoppingDistance * 1.1f;

            if (yCloseEnough && xzCloseEnough)
            {
                _singleWaypointPauseTimer = SINGLE_WAYPOINT_PAUSE_DURATION;
                _isActivelyMoving = false;
                _needsNewPathSegmentForRoaming = true; // Will try to get new path after pause
                return;
            }
        }
        _currentMainPatrolIndex = nextMainIndex;
        _currentHopSeriesTargetWaypoint = _mainPatrolPoints[_currentMainPatrolIndex];

        if (_isGroundRestricted)
        {
            _currentHopSeriesTargetWaypoint.y = ProjectToGroundY(
                _currentHopSeriesTargetWaypoint.x,
                _currentHopSeriesTargetWaypoint.z,
                _currentHopSeriesTargetWaypoint.y, // Use waypoint's own Y as reference
                _currentHopSeriesTargetWaypoint.y  // Fallback
            );
        }

        // Initiate the hop sequence
        _isActivelyMoving = true;
        _isCurrentlyMidHop = false; // Start by preparing for the first hop (via wait timer)
        _hopWaitTimer = 0.01f;    // Tiny wait to kickstart the hop cycle in HandleHopMovement
                                  // Or: hopWaitDuration to have a full wait before the first hop
        // Debug.Log(LOG_PREFIX + gameObject.name + " starting new HOP series to: " + _currentHopSeriesTargetWaypoint);
    }
    
}