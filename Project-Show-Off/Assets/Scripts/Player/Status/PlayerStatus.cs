using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    public bool IsCrouching { get; set; } = false; 
    public bool IsInTallGrass { get; set; } = false;
    public bool IsLanternRaised { get; set; } = false;
    public WaterZone CurrentWaterZone { get; set; }

    private PlayerMovement _playerMovement;
    public bool IsMoving => _playerMovement != null && _playerMovement.isMoving;

    // For dynamic water level check by Hunter
    public bool IsSubmerged(Vector3 checkPosition)
    {
        if (CurrentWaterZone != null)
        {
            return checkPosition.y < CurrentWaterZone.SurfaceYLevel;
        }
        return false;
    }

    public bool IsSubmerged(Vector3 checkPosition, float specificWaterSurfaceY)
    {
        bool inAnyWater = (CurrentWaterZone != null) || (GetComponent<WaterSensor>() != null && GetComponent<WaterSensor>().IsPlayerUnderwater());
        return inAnyWater && checkPosition.y < specificWaterSurfaceY;
    }


    void Awake()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        if (_playerMovement == null)
        {
            Debug.LogError("PlayerStatus: PlayerMovement component not found!", this);
        }
    }
}