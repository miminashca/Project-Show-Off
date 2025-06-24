using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class NixieNavigation : MonoBehaviour
{
    [Header("Patrol Setup")]
    [Tooltip("A list of transforms defining the Nixie's patrol path within its water body.")]
    public List<Transform> PatrolNodes;

    [Header("Movement Speeds")]
    public float RoamingSpeed = 2f;
    public float ChasingSpeed = 6f;

    [Header("Vertical Positioning (Base Offset)")]
    [Tooltip("The vertical offset from the NavMesh when submerged. Lifts the model off the ground.")]
    public float SubmergedBaseOffset = 1.2f;
    [Tooltip("The vertical offset from the NavMesh when peeking. Lifts the model higher.")]
    public float PeekingBaseOffset = 1.8f;

    [Header("Wavy Movement Parameters")]
    [Tooltip("How far the Nixie will sway side-to-side from its direct path when chasing.")]
    public float WaveAmplitude = 1.5f;
    [Tooltip("How frequently the Nixie will sway. Higher values mean more wiggles over the same distance.")]
    public float WaveFrequency = 0.5f;
    [Tooltip("How many points to generate for the wavy path. More points = smoother wave.")]
    [Range(4, 40)]
    public int WavePathResolution = 20;


    // --- Public Enum for Movement Styles ---
    public enum MoveStyle { Straight, Wavy }

    // --- Private runtime variables ---
    private int currentPatrolIndex = -1;
    private NavMeshAgent _agent;
    private List<Vector3> _currentWavyPath = new List<Vector3>();
    private int _currentWavyPathIndex = 0;
    private Coroutine _followingPathCoroutine;


    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        // We will control rotation manually for a more fluid feel.
        _agent.updateRotation = false;
    }

    void Update()
    {
        // If the agent has a path, we want it to look where it's going.
        // This gives us smooth, Slerp-like rotation.
        if (_agent.hasPath && _agent.velocity.sqrMagnitude > 0.1f)
        {
            LookAt(_agent.steeringTarget);
        }
    }

    /// <summary>
    /// The main movement command. Can now handle different styles of movement.
    /// </summary>
    public void MoveTo(Vector3 position, float speed, MoveStyle style = MoveStyle.Straight)
    {
        StopMoving(); // Clear any previous pathfinding
        _agent.speed = speed;

        if (style == MoveStyle.Wavy)
        {
            GenerateWavyPath(position);
            if (_currentWavyPath.Count > 0)
            {
                _followingPathCoroutine = StartCoroutine(FollowPathCoroutine());
            }
        }
        else // Straight
        {
            _agent.SetDestination(position);
            _agent.isStopped = false;
        }
    }

    public void StopMoving()
    {
        if (_followingPathCoroutine != null)
        {
            StopCoroutine(_followingPathCoroutine);
            _followingPathCoroutine = null;
        }
        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }
        _currentWavyPath.Clear();
        _currentWavyPathIndex = 0;
    }

    private void GenerateWavyPath(Vector3 destination)
    {
        _currentWavyPath.Clear();
        NavMeshPath path = new NavMeshPath();

        // 1. Calculate the base path using Unity's NavMesh system. This gives us our corners.
        if (_agent.CalculatePath(destination, path) && path.status == NavMeshPathStatus.PathComplete)
        {
            if (path.corners.Length < 2) return;

            // 2. Post-process the path to add the "wobble".
            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                Vector3 startPoint = path.corners[i];
                Vector3 endPoint = path.corners[i + 1];
                Vector3 segmentDirection = (endPoint - startPoint).normalized;
                float segmentLength = Vector3.Distance(startPoint, endPoint);
                Vector3 perpendicular = Vector3.Cross(segmentDirection, Vector3.up).normalized;

                int resolution = Mathf.Max(2, (int)(segmentLength / (ChasingSpeed / 2f) * WavePathResolution / 5f));

                for (int j = 0; j < resolution; j++)
                {
                    float t = (float)j / resolution;
                    Vector3 pointOnLine = Vector3.Lerp(startPoint, endPoint, t);

                    // Add the sine wave offset
                    float sineOffset = Mathf.Sin(t * Mathf.PI * WaveFrequency * (segmentLength / ChasingSpeed)) * WaveAmplitude;
                    Vector3 wavePoint = pointOnLine + perpendicular * sineOffset;

                    _currentWavyPath.Add(wavePoint);
                }
            }
            _currentWavyPath.Add(path.corners[path.corners.Length - 1]); // Ensure we end exactly at the final corner
        }
    }

    private IEnumerator FollowPathCoroutine()
    {
        _currentWavyPathIndex = 0;
        _agent.isStopped = false;

        while (_currentWavyPathIndex < _currentWavyPath.Count)
        {
            _agent.SetDestination(_currentWavyPath[_currentWavyPathIndex]);

            // Wait until the agent gets close to the current waypoint
            while (Vector3.Distance(transform.position, _currentWavyPath[_currentWavyPathIndex]) > _agent.stoppingDistance + 0.5f)
            {
                yield return null;
            }

            _currentWavyPathIndex++;
        }

        // Coroutine finished, path is complete
        StopMoving();
    }


    public Transform GetNextPatrolNode()
    {
        if (PatrolNodes == null || PatrolNodes.Count == 0) return null;
        currentPatrolIndex = (currentPatrolIndex + 1) % PatrolNodes.Count;
        return PatrolNodes[currentPatrolIndex];
    }

    /// <summary>
    /// Uses NavMeshAgent.baseOffset to smoothly control the Nixie's height.
    /// </summary>
    public void SetPeeking(bool shouldPeek)
    {
        _agent.baseOffset = shouldPeek ? PeekingBaseOffset : SubmergedBaseOffset;
    }

    public void LookAt(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // Keep the Nixie level
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * _agent.angularSpeed);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw Patrol Path
        if (PatrolNodes != null && PatrolNodes.Count > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < PatrolNodes.Count; i++)
            {
                Transform currentNode = PatrolNodes[i];
                if (currentNode != null)
                {
                    Gizmos.DrawWireSphere(currentNode.position, 1.0f);
                    Transform nextNode = PatrolNodes[(i + 1) % PatrolNodes.Count];
                    if (nextNode != null)
                    {
                        Gizmos.DrawLine(currentNode.position, nextNode.position);
                    }
                }
            }
        }

        // Draw Current Wavy Path
        if (_currentWavyPath != null && _currentWavyPath.Count > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < _currentWavyPath.Count - 1; i++)
            {
                Gizmos.DrawLine(_currentWavyPath[i], _currentWavyPath[i + 1]);
                Gizmos.DrawSphere(_currentWavyPath[i], 0.1f);
            }
            Gizmos.DrawSphere(_currentWavyPath[_currentWavyPath.Count - 1], 0.1f);
        }
    }
}
