using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    public bool IsCrouching { get; set; } = false; 
    public bool IsInTallGrass { get; set; } = false;
    public bool IsLanternRaised { get; set; } = false;
    public WaterZone CurrentWaterZone { get; set; }

    private PlayerMovement _playerMovement;
    public bool IsMoving => _playerMovement != null && _playerMovement.isMoving;

    [Header("AI Visibility")]
    [Tooltip("Assign the 'Head' child transform. PlayerMovement will move it.")]
    public Transform HeadVisibilityPoint;
    [Tooltip("Assign the 'Torso' child transform. PlayerMovement will move it.")]
    public Transform TorsoVisibilityPoint;
    [Tooltip("Assign the 'Feet' child transform. This is usually static.")]
    public Transform FeetVisibilityPoint;

    void Awake()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        if (_playerMovement == null)
        {
            Debug.LogError("PlayerStatus: PlayerMovement component not found!", this);
        }
    }

    public bool IsSubmerged(Vector3 checkPosition)
    {
        // The logic is simple: if we are not in a water zone, nothing can be submerged.
        // If we are, we just check the Y-level.
        if (CurrentWaterZone != null)
        {
            return checkPosition.y < CurrentWaterZone.SurfaceYLevel;
        }
        return false;
    }

    public bool IsSubmerged(Vector3 checkPosition, float specificWaterSurfaceY)
    {
        bool inAnyManagedWaterSystem = (CurrentWaterZone != null && checkPosition.y < CurrentWaterZone.SurfaceYLevel);

        WaterSensor sensor = GetComponent<WaterSensor>(); // GetComponent can be slow in frequent calls, consider caching if performance is an issue
        bool inSensorWater = (sensor != null && sensor.IsPlayerUnderwater() && checkPosition.y < specificWaterSurfaceY);


        float authoritativeSurfaceY = specificWaterSurfaceY;
        if (CurrentWaterZone != null)
        {
            authoritativeSurfaceY = CurrentWaterZone.SurfaceYLevel;
        }

        bool inWaterByAnySystem = (CurrentWaterZone != null) || (sensor != null && sensor.IsPlayerUnderwater());

        return inWaterByAnySystem && checkPosition.y < authoritativeSurfaceY;
    }


    // A helper property to easily access them
    public Transform[] GetVisibilityPoints()
    {
        // You can add logic here if some points shouldn't be active
        return new Transform[] { HeadVisibilityPoint, TorsoVisibilityPoint, FeetVisibilityPoint };
    }
    
}