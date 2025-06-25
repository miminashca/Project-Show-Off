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

    [Header("Peeking Mechanic")]
    [Tooltip("The vertical offset from the NavMesh when submerged.")]
    public float SubmergedYOffset = -0.5f;
    [Tooltip("The vertical offset from the NavMesh when peeking.")]
    public float PeekingYOffset = 0.2f;

    // --- Private runtime variables ---
    private int currentPatrolIndex = -1;
    private NavMeshAgent _agent; // <-- Reference to the NavMeshAgent

    void Awake() // <-- Use Awake instead of Start to ensure it's ready for other scripts
    {
        _agent = GetComponent<NavMeshAgent>();

        // Disable agent's own rotation updates if we want to control it manually with LookAt
        _agent.updateRotation = false;
    }

    void Update()
    {
        // If the agent is moving, we want it to look where it's going.
        // We do this manually because we disabled agent.updateRotation.
        if (_agent.velocity.sqrMagnitude > 0.1f)
        {
            LookAt(_agent.steeringTarget);
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
        // The NavMeshAgent's baseOffset is the perfect tool for this!
        _agent.baseOffset = shouldPeek ? PeekingYOffset : SubmergedYOffset;
    }

    public void LookAt(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // Keep the Nixie level (don't have it pitch up/down)
        if (direction != Vector3.zero)
        {
            // Use Slerp for smoother rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * _agent.angularSpeed);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Don't draw anything if the list is null or empty.
        if (PatrolNodes == null || PatrolNodes.Count == 0)
        {
            return;
        }

        // Set the color for our patrol path gizmos.
        Gizmos.color = Color.green;

        // Loop through all patrol nodes.
        for (int i = 0; i < PatrolNodes.Count; i++)
        {
            Transform currentNode = PatrolNodes[i];

            // Check if the reference to the node is not broken.
            if (currentNode != null)
            {
                // Draw the wire sphere at the node's position.
                Gizmos.DrawWireSphere(currentNode.position, 1.0f);

                // Find the next node in the list.
                // The modulo (%) operator makes the path wrap around, connecting the last node to the first.
                Transform nextNode = PatrolNodes[(i + 1) % PatrolNodes.Count];

                // If the next node also exists, draw a line between them.
                if (nextNode != null)
                {
                    Gizmos.DrawLine(currentNode.position, nextNode.position);
                }
            }
        }
    }
}