using System;
using UnityEngine;

public class PlayerStateController : MonoBehaviour
{
    private PlayerInput controls;
    private PlayerStatus playerStatus;

    [Header("Speed Modifiers")]
    [Tooltip("How much speed is reduced when Hemanneken is attached (e.g., 0.1 for 10% reduction).")]
    [SerializeField] private float hemannekenSpeedDecrease = 0.1f;
    [Tooltip("How much speed is reduced when underwater (e.g., 0.4 for 40% reduction).")]
    [SerializeField] private float waterSpeedDecrease = 0.4f;

    [NonSerialized] private PlayerMovement playerMovement;

    // --- NEW: State flags for robust calculation ---
    private bool isHemannekenAttached = false;
    private bool isSubmerged = false;
    private bool isAdrenalineActive = false;
    private float adrenalineSpeedBoostValue = 0f;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("PlayerMovement component not found on this GameObject!", this);
        }
        controls = new PlayerInput();

        playerStatus = GetComponent<PlayerStatus>();
        if (playerStatus == null)
        {
            Debug.LogError("PlayerStatus component not found on this GameObject!", this);
        }
    }

    private void OnEnable()
    {
        controls.Enable();
        HemannekenEventBus.OnHemannekenAttached += HandleHemAttached;
        HemannekenEventBus.OnHemannekenDetached += HandleHemDetached;
        WaterEventBus.OnPlayerSubmerge += HandlePlayerSubmerge;
        WaterEventBus.OnPlayerEmerge += HandlePlayerEmerge;
    }

    private void OnDisable()
    {
        controls.Disable();
        HemannekenEventBus.OnHemannekenAttached -= HandleHemAttached;
        HemannekenEventBus.OnHemannekenDetached -= HandleHemDetached;
        WaterEventBus.OnPlayerSubmerge -= HandlePlayerSubmerge;
        WaterEventBus.OnPlayerEmerge -= HandlePlayerEmerge;
    }

    private void Update()
    {
        // The Update method is now empty, which is fine
    }

    /// <summary>
    /// Called by PlayerHealth to enable/disable the speed boost from adrenaline.
    /// </summary>
    public void SetAdrenalineBoost(float boostValue, bool isActive)
    {
        isAdrenalineActive = isActive;
        adrenalineSpeedBoostValue = boostValue;
        RecalculateAndApplySpeed();
    }

    /// <summary>
    /// REFACTORED: Recalculates the final speed modifier from scratch based on current states.
    /// This is more robust than adding/subtracting values.
    /// </summary>
    private void RecalculateAndApplySpeed()
    {
        if (playerMovement == null) return;

        float finalSpeedModifier = 1f; // Start at 100%

        if (isHemannekenAttached)
        {
            finalSpeedModifier -= hemannekenSpeedDecrease;
        }
        if (isSubmerged)
        {
            finalSpeedModifier -= waterSpeedDecrease;
        }
        if (isAdrenalineActive)
        {
            finalSpeedModifier += adrenalineSpeedBoostValue;
        }

        // Ensure speed doesn't become negative
        playerMovement.speedModifier = Mathf.Max(0, finalSpeedModifier);
    }

    private void HandleHemAttached()
    {
        isHemannekenAttached = true;
        RecalculateAndApplySpeed();
    }

    private void HandleHemDetached()
    {
        isHemannekenAttached = false;
        RecalculateAndApplySpeed();
    }

    private void HandlePlayerSubmerge()
    {
        isSubmerged = true;
        RecalculateAndApplySpeed();
    }

    private void HandlePlayerEmerge()
    {
        isSubmerged = false;
        RecalculateAndApplySpeed();
    }
}