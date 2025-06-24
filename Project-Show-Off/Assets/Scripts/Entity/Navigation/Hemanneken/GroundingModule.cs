using UnityEngine;

/// <summary>
/// Encapsulates the logic for detecting the ground and snapping a position to it.
/// </summary>
public class GroundingModule
{
    private readonly LayerMask _groundLayerMask;
    private readonly float _groundOffset;
    private readonly float _raycastMaxDistance;
    private readonly float _raycastStartHeightOffset;

    public GroundingModule(LayerMask groundLayerMask, float groundOffset, float raycastMaxDistance, float raycastStartHeightOffset)
    {
        _groundLayerMask = groundLayerMask;
        _groundOffset = groundOffset;
        _raycastMaxDistance = raycastMaxDistance;
        _raycastStartHeightOffset = raycastStartHeightOffset;
    }

    /// <summary>
    /// Projects a position onto the ground via a downward raycast.
    /// </summary>
    /// <param name="position">The position to project.</param>
    /// <param name="referenceY">The Y-level to start the raycast from (plus an offset).</param>
    /// <returns>The ground position, or the original Y-value if no ground is hit.</returns>
    public Vector3 SnapToGround(Vector3 position, float referenceY)
    {
        Vector3 rayStart = new Vector3(position.x, referenceY + _raycastStartHeightOffset, position.z);
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, _raycastMaxDistance, _groundLayerMask))
        {
            return new Vector3(position.x, hit.point.y + _groundOffset, position.z);
        }
        return position; // Return original position if no ground found
    }

    public void DrawGizmos(Transform agentTransform)
    {
        Gizmos.color = Color.yellow;
        float refY = agentTransform.position.y;
        Vector3 rayStart = new Vector3(agentTransform.position.x, refY + _raycastStartHeightOffset, agentTransform.position.z);
        Vector3 rayEnd = rayStart + Vector3.down * _raycastMaxDistance;
        Gizmos.DrawLine(rayStart, rayEnd);

        Vector3 groundPos = SnapToGround(agentTransform.position, refY);
        Gizmos.DrawWireSphere(groundPos, 0.1f);
    }
}