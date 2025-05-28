using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    public bool IsInWaterZone { get; set; } = false;
    public bool IsCrouching { get; set; } = false; // For potential tall grass logic
    public bool IsSubmerged(Vector3 checkPosition, float waterSurfaceYLevel)
    {
        return IsInWaterZone && checkPosition.y < waterSurfaceYLevel;
    }
}