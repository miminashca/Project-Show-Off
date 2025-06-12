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

        float targetY = shouldPeek ? PeekingYPosition : SubmergedYPosition;
        Vector3 newLocalPos = HeadModelTransform.localPosition;
        newLocalPos.y = targetY;
        HeadModelTransform.localPosition = newLocalPos;
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
}