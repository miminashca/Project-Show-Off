using UnityEngine;
using UnityEngine.AI; // <-- Add this namespace
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))] // <-- Ensure NavMeshAgent is present
public class NixieNavigation : MonoBehaviour
{
    [Header("Patrol Setup")]
    [Tooltip("A list of transforms defining the Nixie's patrol path within its water body.")]
    public List<Transform> PatrolNodes;

    [Header("Movement Speeds")]
    public float RoamingSpeed = 2f;
    public float ChasingSpeed = 6f;

    [Header("Movement Style")]
    [Tooltip("How frequently the Nixie weaves side-to-side while moving.")]
    public float WeaveFrequency = 2f;
    [Tooltip("How far the Nixie weaves side-to-side from its central path.")]
    public float WeaveAmplitude = 0.5f;

    [Header("Peeking Mechanic")]
    [Tooltip("The vertical offset from the NavMesh when submerged.")]
    public float SubmergedYOffset = -0.5f;
    [Tooltip("The vertical offset from the NavMesh when peeking.")]
    public float PeekingYOffset = 0.2f;

    [Header("Visuals")]
    [Tooltip("The child transform containing the Nixie's model/renderer.")]
    public Transform VisualsTransform;
    [Tooltip("How smoothly the visuals follow the agent's position. Higher is faster.")]
    public float VisualsLerpSpeed = 5f;

    [Header("Gizmo Toggles")]
    public bool ShowMovementGizmos = true;

    // --- Public property for State Machine control ---
    public bool IsLockedToSurface { get; set; } = false;

    // --- Private runtime variables ---
    private int currentPatrolIndex = -1;
    private NavMeshAgent _agent;
    private float targetYOffset;
    private NixieAI nixieAI;

    private Vector3 lastCalculatedVisualTarget;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        nixieAI = GetComponent<NixieAI>();

        _agent.updateRotation = false;

        if (nixieAI == null)
        {
            Debug.LogError("NixieNavigation: Could not find the required NixieAI component!", this);
            enabled = false;
            return;
        }

        if (VisualsTransform == null)
        {
            Debug.LogError("NixieNavigation: VisualsTransform is not assigned!", this);
            enabled = false;
            return;
        }

        // Initialize the target offset to the submerged value
        targetYOffset = SubmergedYOffset;
    }

    void Update()
    {
        Vector3 agentPosition = transform.position;

        // --- Only weave if not locked to surface and moving ---
        Vector3 weaveOffset = Vector3.zero;
        if (!IsLockedToSurface && _agent.velocity.sqrMagnitude > 0.1f)
        {
            Vector3 perpendicular = Vector3.Cross(_agent.velocity.normalized, Vector3.up);
            weaveOffset = perpendicular * Mathf.Sin(Time.time * WeaveFrequency) * WeaveAmplitude;
        }

        // --- REVISED: Visual Positioning Logic ---
        Vector3 targetVisualPosition;

        // --- THIS IS THE KEY CHANGE ---
        // We now check for MyWaterZone instead of MyNixieZone
        if (IsLockedToSurface && nixieAI.MyWaterZone != null)
        {
            // If locked, use the WaterZone's surface Y level.
            float surfaceY = nixieAI.MyWaterZone.SurfaceYLevel;
            // The PeekingYOffset now acts as the height above the surface.
            targetVisualPosition = new Vector3(agentPosition.x, surfaceY + PeekingYOffset, agentPosition.z);
        }
        else
        {
            // The normal logic: follow the agent's Y plus our offset.
            targetVisualPosition = new Vector3(agentPosition.x, agentPosition.y + targetYOffset, agentPosition.z) + weaveOffset;
        }

        VisualsTransform.position = Vector3.Lerp(VisualsTransform.position, targetVisualPosition, Time.deltaTime * VisualsLerpSpeed);

        lastCalculatedVisualTarget = targetVisualPosition;

        // --- ROTATION LOGIC ---
        // Let the state machine handle LookAt calls for more specific control.
        if (_agent.velocity.sqrMagnitude > 0.1f && !IsLockedToSurface)
        {
            // Only auto-look where we're going if we aren't in a special state like Staring.
            LookAt(_agent.steeringTarget + weaveOffset);
        }
    }

    public void MoveTo(Vector3 position, float speed)
    {
        _agent.speed = speed;
        _agent.SetDestination(position);
        _agent.isStopped = false; // Ensure agent is set to move
    }

    public void StopMoving()
    {
        if (_agent.isOnNavMesh) // Prevent errors if called at a bad time
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }
    }

    public Transform GetNextPatrolNode()
    {
        if (PatrolNodes == null || PatrolNodes.Count == 0) return null;
        currentPatrolIndex = (currentPatrolIndex + 1) % PatrolNodes.Count;
        return PatrolNodes[currentPatrolIndex];
    }

    public void SetPeeking(bool shouldPeek)
    {
        // Instead of setting baseOffset, we now control our custom targetYOffset.
        targetYOffset = shouldPeek ? PeekingYOffset : SubmergedYOffset;
    }

    public void LookAt(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - VisualsTransform.position).normalized; // <-- Look from the visuals' position
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            // Apply rotation to the VISUALS, not the parent agent.
            VisualsTransform.rotation = Quaternion.Slerp(VisualsTransform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * _agent.angularSpeed);
        }
    }

    void OnDrawGizmos()
    {
        if (!ShowMovementGizmos || !Application.isPlaying || _agent == null) return;

        // --- 1. Draw the NavMeshAgent's current path ---
        if (_agent.hasPath)
        {
            Gizmos.color = Color.cyan;
            var corners = _agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
                Gizmos.DrawSphere(corners[i], 0.1f);
            }
            if (corners.Length > 0)
                Gizmos.DrawSphere(corners[corners.Length - 1], 0.1f);
        }

        // --- 2. Draw where the Visuals are trying to go ---
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(lastCalculatedVisualTarget, 0.25f);

        // --- 3. Draw a line from the actual agent to the visual target ---
        // This line shows the smoothing + weave offset clearly.
        Gizmos.DrawLine(VisualsTransform.position, lastCalculatedVisualTarget);

        // --- 4. Draw a line from agent to the actual visual's position
        Gizmos.color = new Color(0, 0.5f, 0); // Dark Green
        Gizmos.DrawLine(transform.position, VisualsTransform.position);
    }
}