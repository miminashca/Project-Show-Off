using System;
using UnityEngine;

public class PlayerStateController : MonoBehaviour
{
    private PlayerInput controls;
    private PlayerStatus playerStatus;

    private bool isHoldingLantern = false;
    [NonSerialized] public float lanternTimeCounter = 0;
    [NonSerialized] public bool countLanternTime = false;

    [Header("Speed Modifiers")]
    [Tooltip("How much speed is reduced when Hemanneken is attached (e.g., 0.1 for 10% reduction).")]
    [SerializeField] private float hemannekenSpeedDecrease = 0.1f; // Renamed for clarity
    [Tooltip("How much speed is reduced when underwater (e.g., 0.4 for 40% reduction).")]
    [SerializeField] private float waterSpeedDecrease = 0.4f;

    [NonSerialized] private PlayerMovement playerMovement;
    private float finalSpeedModifier = 1f;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("PlayerMovement component not found on this GameObject!", this);
        }
        controls = new PlayerInput();

        // --- GET THE COMPONENT ---
        playerStatus = GetComponent<PlayerStatus>();
        if (playerStatus == null)
        {
            Debug.LogError("PlayerStatus component not found on this GameObject!", this);
        }
    }

    private void OnEnable()
    {
        controls.Enable();
        HemannekenEventBus.OnHemannekenAttached += HandleHemAttached; // Renamed handler
        HemannekenEventBus.OnHemannekenDetached += HandleHemDetached; // Renamed handler
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
        if (controls.Player.RaiseLantern.WasPressedThisFrame())
        {
            isHoldingLantern = true;
        }
        else if (controls.Player.RaiseLantern.WasReleasedThisFrame())
        {
            isHoldingLantern = false;
        }

        if (playerStatus != null) playerStatus.IsLanternOn = isHoldingLantern;

        if (isHoldingLantern && countLanternTime)
        {
            lanternTimeCounter += Time.deltaTime;
        }
        else
        {
            lanternTimeCounter = 0;
        }
    }

    private void UpdatePlayerSpeed()
    {
        if (playerMovement != null)
        {
            playerMovement.speedModifier = finalSpeedModifier;
            // Debug.Log($"Player speed modifier updated to: {finalSpeedModifier}");
        }
    }

    private void HandleHemAttached() // Renamed
    {
        //Debug.Log("PSC: Hemanneken Attached - Speed Decreased");
        finalSpeedModifier -= hemannekenSpeedDecrease;
        UpdatePlayerSpeed();
    }

    private void HandleHemDetached() // Renamed
    {
        //Debug.Log("PSC: Hemanneken Detached - Speed Restored");
        finalSpeedModifier += hemannekenSpeedDecrease;
        UpdatePlayerSpeed();
    }

    private void HandlePlayerSubmerge()
    {
        //Debug.Log("PSC: Player Submerged - Speed Decreased");
        finalSpeedModifier -= waterSpeedDecrease;
        UpdatePlayerSpeed();
    }

    private void HandlePlayerEmerge()
    {
        //Debug.Log("PSC: Player Emerged - Speed Restored");
        finalSpeedModifier += waterSpeedDecrease;
        UpdatePlayerSpeed();
    }
}