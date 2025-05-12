using UnityEngine;
using UnityEngine.AI;

public class NixieAI : MonoBehaviour
{
    public float attractSpeed = 4f;
    public float investigationDuration = 5.0f; // How long it moves towards the last known spot

    private NavMeshAgent agent; // Optional
    private bool isAttracted = false;
    private Vector3 attractionPoint;
    private float investigationTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (isAttracted)
        {
            investigationTimer -= Time.deltaTime;
            float distanceToTarget = Vector3.Distance(transform.position, attractionPoint);

            // Keep moving towards target if timer > 0 OR haven't reached it yet
            // (Add a small threshold like 1.0f to prevent jittering at destination)
            if (investigationTimer > 0 || distanceToTarget > agent.stoppingDistance + 0.5f)
            {
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.speed = attractSpeed;
                    // Check if destination needs updating (if attractionPoint changed significantly)
                    if (Vector3.Distance(agent.destination, attractionPoint) > 1.0f)
                    {
                        agent.SetDestination(attractionPoint);
                    }
                }
                else
                {
                    // Simple movement
                    Vector3 directionToTarget = (attractionPoint - transform.position).normalized;
                    transform.position += directionToTarget * attractSpeed * Time.deltaTime;
                }
            }
            else
            {
                // Reached destination or timer ran out
                isAttracted = false;
                Debug.Log($"{gameObject.name} finished investigating lantern spot.");
                // Return to normal behavior
                if (agent != null)
                {
                    // agent.speed = normalSpeed;
                }
            }
        }
        else
        {
            // Normal AI behavior
            // ...
        }
    }

    // Called by LanternController when the raised lantern detects this AI
    public void Attract(Vector3 sourcePosition)
    {
        if (!isAttracted)
        {
            Debug.Log($"{gameObject.name} is attracted to lantern!");
            isAttracted = true;
            // Optionally interrupt current action
        }

        // Always update the target point and reset the timer
        attractionPoint = sourcePosition;
        investigationTimer = investigationDuration;

        // Immediately set destination if using NavMesh and attracted
        if (isAttracted && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(attractionPoint);
        }
    }
}