using UnityEngine;
using UnityEngine.AI; // If using NavMeshAgent

public class HemannekenAI : MonoBehaviour
{
    public float repelDistance = 7f; // Should match or be slightly less than lantern's repel radius
    public float repelSpeed = 5f;
    public float stopRepelDistance = 10f; // Distance at which it stops actively running away
    public float repelDuration = 2.0f; // How long it stays "scared" after losing sight of raised lantern

    private NavMeshAgent agent; // Optional: For NavMesh movement
    private bool isRepelled = false;
    private Vector3 repelSourcePosition;
    private float repelTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (isRepelled)
        {
            repelTimer -= Time.deltaTime;
            float distanceToSource = Vector3.Distance(transform.position, repelSourcePosition);

            // Keep repelling if timer > 0 AND close enough
            if (repelTimer > 0 && distanceToSource < stopRepelDistance)
            {
                Vector3 directionAwayFromSource = (transform.position - repelSourcePosition).normalized;
                Vector3 targetPosition = transform.position + directionAwayFromSource * 2f; // Move 2 units away

                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.speed = repelSpeed; // Use faster speed when repelled
                    agent.SetDestination(targetPosition);
                }
                else
                {
                    // Simple movement if not using NavMesh
                    transform.position += directionAwayFromSource * repelSpeed * Time.deltaTime;
                }
            }
            else
            {
                // Stop being repelled
                isRepelled = false;
                Debug.Log($"{gameObject.name} stopped being repelled.");
                // Return to normal behavior (patrolling, idling, etc.)
                if (agent != null)
                {
                    // agent.speed = normalSpeed; // Reset speed
                    // agent.ResetPath(); or agent.SetDestination(patrolPoint);
                }

            }
        }
        else
        {
            // Normal AI behavior (patrolling, chasing if applicable, idling)
            // ... your existing AI logic ...
        }
    }

    // Called by LanternController when the raised lantern detects this AI
    public void Repel(Vector3 sourcePosition)
    {
        if (!isRepelled || Vector3.Distance(transform.position, sourcePosition) < Vector3.Distance(transform.position, repelSourcePosition))
        {
            // Update source if closer or wasn't repelled before
            repelSourcePosition = sourcePosition;
        }

        if (!isRepelled)
        {
            Debug.Log($"{gameObject.name} is being repelled by lantern!");
            isRepelled = true;
            // Optionally interrupt current action (e.g., attack)
        }

        // Reset timer each time the repel signal is received
        repelTimer = repelDuration;
    }
}