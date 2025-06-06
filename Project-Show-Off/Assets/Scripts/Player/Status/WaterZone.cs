using UnityEngine;

public class WaterZone : MonoBehaviour
{
    [Tooltip("Assign the Transform of the actual water surface plane for this zone. Its Y position will be used as the SurfaceYLevel.")]
    public Transform waterSurfacePlane;

    private Collider _collider;

    /// <summary>
    /// The Y-coordinate of this water body's surface, derived from waterSurfacePlane.
    /// </summary>
    public float SurfaceYLevel
    {
        get
        {
            if (waterSurfacePlane != null)
            {
                return waterSurfacePlane.position.y;
            }
            Debug.LogWarning($"WaterZone '{gameObject.name}' is missing 'waterSurfacePlane' assignment. Using transform.position.y as fallback SurfaceYLevel.", this);
            return transform.position.y;
        }
    }

    void Awake()
    {
        _collider = GetComponent<Collider>();

        if (waterSurfacePlane == null)
        {
            Debug.LogError($"WaterZone on '{gameObject.name}' needs its 'Water Surface Plane' assigned in the Inspector!", this);
        }

        if (_collider == null)
        {
            Debug.LogWarning($"WaterZone on '{gameObject.name}' is missing a Collider component. It won't be able to detect the player.", this);
        }
        else if (!_collider.isTrigger)
        {
            Debug.LogWarning($"WaterZone on '{gameObject.name}'s Collider is not set to 'Is Trigger'. Player detection might not work as expected.", this);
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            if (_collider == null) _collider = GetComponent<Collider>(); 
            if (_collider == null) return;

            // Use the SurfaceYLevel property, which derives from waterSurfacePlane
            float currentSurfaceY = (waterSurfacePlane != null) ? waterSurfacePlane.position.y : transform.position.y;

            Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.2f); // Semi-transparent blue
            Bounds bounds = _collider.bounds;
            Vector3 center = new Vector3(bounds.center.x, currentSurfaceY, bounds.center.z);
            Vector3 size = new Vector3(bounds.size.x, 0.01f, bounds.size.z); // Very thin
            Gizmos.DrawCube(center, size);

            if (waterSurfacePlane == null && _collider != null) // Add a warning line if plane not set
            {
                Gizmos.color = Color.yellow;
                Vector3 warningLineStart = new Vector3(bounds.min.x, currentSurfaceY, bounds.min.z);
                Vector3 warningLineEnd = new Vector3(bounds.max.x, currentSurfaceY, bounds.max.z);
                Gizmos.DrawLine(warningLineStart, warningLineEnd);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(center + Vector3.up * 0.5f, "WaterZone: Assign waterSurfacePlane!");
#endif
            }
        }
    }
}