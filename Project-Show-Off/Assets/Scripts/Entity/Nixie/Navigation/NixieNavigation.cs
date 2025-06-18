using UnityEngine;
using System.Collections.Generic;

public class NixieNavigation : MonoBehaviour
{
    [Header("Patrol Setup")]
    [Tooltip("A list of transforms defining the Nixie's patrol path within its water body.")]
    public List<Transform> PatrolNodes;

    [Header("Movement Speeds")]
    public float RoamingSpeed = 2f;
    public float ChasingSpeed = 6f;

    [Header("Avoidance")]
    [Tooltip("How far ahead the Nixie looks for obstacles.")]
    public float ObstacleRaycastDistance = 5f;
    [Tooltip("The layer(s) that count as obstacles (e.g., Terrain, Default).")]
    public LayerMask ObstacleLayers;

    [Header("Peeking Mechanic")]
    [Tooltip("The vertical offset from the water surface when submerged.")]
    public float SubmergedYOffset = -0.5f;
    [Tooltip("The vertical offset from the water surface when peeking.")]
    public float PeekingYOffset = 0.2f;

    // --- Private runtime variables ---
    private int currentPatrolIndex = -1;
    private Vector3 horizontalTargetPosition;
    private float currentSpeed;
    private bool isMoving = false;
    private bool isPeeking = false;

    private Collider nixieZoneCollider;
    private float baseSwimLevel;

    void Start()
    {
        // We need the AI script to find the zone
        NixieAI ai = GetComponent<NixieAI>();
        if (ai != null && ai.MyNixieZone != null)
        {
            nixieZoneCollider = ai.MyNixieZone.GetComponent<Collider>();
        }

        // FIX: Establish the base swimming level at the start of the game.
        baseSwimLevel = transform.position.y;
    }

    void Update()
    {
        // If we don't have a target, there's nothing to do.
        if (!isMoving) return;

        // --- FIX: Construct the full 3D target position on every frame ---
        // This makes the vertical movement (peeking) responsive.
        float desiredY = baseSwimLevel + (isPeeking ? PeekingYOffset : SubmergedYOffset);
        Vector3 finalTargetPosition = new Vector3(horizontalTargetPosition.x, desiredY, horizontalTargetPosition.z);

        // --- Calculate direction and apply avoidance ---
        Vector3 direction = (finalTargetPosition - transform.position).normalized;

        if (direction != Vector3.zero && Physics.Raycast(transform.position, direction, out RaycastHit hit, ObstacleRaycastDistance, ObstacleLayers))
        {
            // A simple avoidance: find a direction perpendicular to the obstacle's normal
            direction = Vector3.Cross(hit.normal, Vector3.up).normalized;
            // If the Nixie is moving straight into a wall, the cross product can be zero.
            // In that case, we pick an arbitrary direction to the side.
            if (direction == Vector3.zero) { direction = transform.right; }
        }

        // --- Calculate the next position and apply bounding ---
        Vector3 nextPosition = transform.position + direction * currentSpeed * Time.deltaTime;

        if (nixieZoneCollider != null && !nixieZoneCollider.bounds.Contains(nextPosition))
        {
            nextPosition = nixieZoneCollider.ClosestPoint(nextPosition);
        }

        // --- Apply movement and rotation ---
        transform.position = nextPosition;
        LookAt(finalTargetPosition);
    }

    public void MoveTo(Vector3 position, float speed)
    {
        horizontalTargetPosition = position;
        currentSpeed = speed;
        isMoving = true;
    }

    public void StopMoving()
    {
        isMoving = false;
    }

    public Transform GetNextPatrolNode()
    {
        if (PatrolNodes == null || PatrolNodes.Count == 0) return null;
        currentPatrolIndex = (currentPatrolIndex + 1) % PatrolNodes.Count;
        return PatrolNodes[currentPatrolIndex];
    }

    // FIX: This now correctly flags the vertical state. The Update loop handles the movement.
    public void SetPeeking(bool shouldPeek)
    {
        isPeeking = shouldPeek;
    }

    public void LookAt(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // Keep the Nixie level
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    // --- GIZMOS SECTION ---
    void OnDrawGizmosSelected()
    {
        // ... (Patrol node gizmos are fine) ...

        // --- FIX: Visualize the avoidance raycast ---
        if (Application.isPlaying && isMoving)
        {
            Vector3 finalTargetPosition = new Vector3(horizontalTargetPosition.x, transform.position.y, horizontalTargetPosition.z);
            Vector3 direction = (finalTargetPosition - transform.position).normalized;

            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, direction * ObstacleRaycastDistance);
        }

        // --- FIX: Visualize peeking heights relative to the base swim level ---
        float currentBaseLevel = Application.isPlaying ? baseSwimLevel : transform.position.y;
        Vector3 peekPos = new Vector3(transform.position.x, currentBaseLevel + PeekingYOffset, transform.position.z);
        Vector3 subPos = new Vector3(transform.position.x, currentBaseLevel + SubmergedYOffset, transform.position.z);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(peekPos, 0.5f); // Use a disc for better level visualization
        DrawGizmoLabel(peekPos + Vector3.right * 0.6f, "Peeking Y", Color.cyan);

        Gizmos.color = new Color(0, 0, 0.8f);
        Gizmos.DrawWireSphere(subPos, 0.5f); // Use a disc for better level visualization
        DrawGizmoLabel(subPos + Vector3.right * 0.6f, "Submerged Y", Gizmos.color);

        Gizmos.color = Color.gray;
        Gizmos.DrawLine(peekPos, subPos);
    }

    // Helper method to draw text labels in the scene view
    private void DrawGizmoLabel(Vector3 position, string text, Color color)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.Label(position, text);
#endif
    }
}