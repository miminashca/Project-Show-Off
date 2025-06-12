using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    [Header("State Properties")]
    public bool IsCrouching { get; set; } = false;
    public bool IsInTallGrass { get; set; } = false;
    public bool IsLanternRaised { get; set; } = false;
    public WaterZone CurrentWaterZone { get; set; }

    public bool IsLanternOn { get; set; } = false;

    private PlayerMovement _playerMovement;
    public bool IsMoving => _playerMovement != null && _playerMovement.isMoving;

    [Header("AI Visibility")]
    [Tooltip("Assign the 'Head' child transform.")]
    public Transform HeadVisibilityPoint;
    [Tooltip("Assign the 'Torso' child transform.")]
    public Transform TorsoVisibilityPoint;
    [Tooltip("Assign the 'Feet' child transform.")]
    public Transform FeetVisibilityPoint;
    private Transform[] _visibilityPoints;

    void Awake()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        if (_playerMovement == null)
        {
            Debug.LogError("PlayerStatus: PlayerMovement component not found!", this);
        }

        // Initialize the cached array.
        _visibilityPoints = new Transform[] { HeadVisibilityPoint, TorsoVisibilityPoint, FeetVisibilityPoint };
    }

    /// <summary>
    /// Checks if a given world position is below the current water zone's surface.
    /// </summary>
    /// <param name="checkPosition">The position to check.</param>
    /// <returns>True if submerged, false otherwise.</returns>
    public bool IsSubmerged(Vector3 checkPosition)
    {
        // If we are not in a water zone, we cannot be submerged.
        // If we are, we just check the Y-level against the zone's surface.
        if (CurrentWaterZone != null)
        {
            return checkPosition.y < CurrentWaterZone.SurfaceYLevel;
        }
        return false;
    }

    /// <summary>
    /// Returns a cached array of visibility points for AI line-of-sight checks.
    /// </summary>
    public Transform[] GetVisibilityPoints()
    {
        return _visibilityPoints;
    }
}