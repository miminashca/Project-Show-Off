using UnityEngine;

public class WaterZone : MonoBehaviour
{
    [Tooltip("The Y-coordinate of this water body's surface.")]
    public float SurfaceYLevel;

    // Optional: Visual aid in editor
    void OnDrawGizmos() // Use OnDrawGizmos to see it even when not selected
    {
        if (!Application.isPlaying) // Only draw in editor, not during play if not needed
        {
            Collider col = GetComponent<Collider>();
            if (col == null) return;

            Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.2f); // Semi-transparent blue
            // Draw a plane at SurfaceYLevel matching the collider's XZ bounds
            Bounds bounds = col.bounds;
            Vector3 center = new Vector3(bounds.center.x, SurfaceYLevel, bounds.center.z);
            Vector3 size = new Vector3(bounds.size.x, 0.01f, bounds.size.z); // Very thin
            Gizmos.DrawCube(center, size);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStatus playerStatus = other.GetComponent<PlayerStatus>();
            if (playerStatus != null)
            {
                playerStatus.CurrentWaterZone = this;
                // Debug.Log($"Player entered water zone: {gameObject.name}, Surface: {SurfaceYLevel}");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStatus playerStatus = other.GetComponent<PlayerStatus>();
            if (playerStatus != null && playerStatus.CurrentWaterZone == this)
            {
                playerStatus.CurrentWaterZone = null;
                // Debug.Log($"Player exited water zone: {gameObject.name}");
            }
        }
    }
}