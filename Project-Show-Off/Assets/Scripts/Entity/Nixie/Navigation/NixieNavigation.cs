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

    [Header("Peeking Mechanic")]
    [Tooltip("The GameObject representing the Nixie's head that peeks above water.")]
    public Transform HeadModelTransform;
    [Tooltip("The local Y position of the head when fully submerged.")]
    public float SubmergedYPosition = -0.5f;
    [Tooltip("The local Y position of the head when peeking above the surface.")]
    public float PeekingYPosition = 0.2f;

    private int currentPatrolIndex = -1;
    private Vector3 currentTargetPosition;
    private float currentSpeed;
    private bool isMoving = false;

    private Coroutine peekingCoroutine;

    void Update()
    {
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, currentTargetPosition, currentSpeed * Time.deltaTime);
        }
    }

    public void MoveTo(Vector3 position, float speed)
    {
        currentTargetPosition = position;
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

    public void SetPeeking(bool shouldPeek)
    {
        if (HeadModelTransform == null) return;

        if (peekingCoroutine != null)
        {
            StopCoroutine(peekingCoroutine);
        }

        peekingCoroutine = StartCoroutine(AnimateHead(shouldPeek));
    }

    private System.Collections.IEnumerator AnimateHead(bool shouldPeek)
    {
        float targetY = shouldPeek ? PeekingYPosition : SubmergedYPosition;
        Vector3 startPos = HeadModelTransform.localPosition;
        Vector3 endPos = new Vector3(startPos.x, targetY, startPos.z);
        float duration = 0.5f; // Animation takes half a second
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            HeadModelTransform.localPosition = Vector3.Lerp(startPos, endPos, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // Ensure it ends at the exact target position
        HeadModelTransform.localPosition = endPos;
    }

    public void LookAt(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // Keep the Nixie level, don't have it tilt up or down
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    // --- GIZMOS SECTION ---
    void OnDrawGizmosSelected()
    {
        // --- Visualize Patrol Path ---
        if (PatrolNodes != null && PatrolNodes.Count > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < PatrolNodes.Count; i++)
            {
                Transform node = PatrolNodes[i];
                if (node == null) continue;

                // Draw a sphere at the node's position
                Gizmos.DrawWireSphere(node.position, 0.5f);
                DrawGizmoLabel(node.position + Vector3.up * 0.6f, $"Node {i}", Color.green);

                // Draw a line to the next node in the list
                if (PatrolNodes.Count > 1)
                {
                    Transform nextNode = PatrolNodes[(i + 1) % PatrolNodes.Count];
                    if (nextNode != null)
                    {
                        Gizmos.DrawLine(node.position, nextNode.position);
                    }
                }
            }
        }

        // --- Visualize Peeking Heights ---
        // These are local offsets, so we draw them relative to the main transform's position.
        Vector3 peekPosition = transform.position + new Vector3(0, PeekingYPosition, 0);
        Vector3 submergedPosition = transform.position + new Vector3(0, SubmergedYPosition, 0);

        // Peeking Height (Cyan Disc)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(peekPosition, 0.5f);
        DrawGizmoLabel(peekPosition + Vector3.right * 0.6f, "Peeking Y", Color.cyan);

        // Submerged Height (Dark Blue Disc)
        Gizmos.color = new Color(0, 0, 0.8f);
        Gizmos.DrawWireSphere(submergedPosition, 0.5f);
        DrawGizmoLabel(submergedPosition + Vector3.right * 0.6f, "Submerged Y", Gizmos.color);

        // Line connecting the two heights for clarity
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(peekPosition, submergedPosition);
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