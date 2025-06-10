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
    public Transform HeadVisibilityPoint;
    public Transform TorsoVisibilityPoint;
    public Transform FeetVisibilityPoint; // Optional but good for crouching/low cover

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
        if (CurrentWaterZone != null)
        {
            // Accesses the SurfaceYLevel property of the unified WaterZone
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