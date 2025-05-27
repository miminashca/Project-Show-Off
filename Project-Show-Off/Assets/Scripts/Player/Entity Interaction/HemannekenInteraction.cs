using System;
using UnityEngine;

public class HemannekenInteraction : MonoBehaviour
{
    private PlayerInput controls;
    private bool isHoldingLantern = false;
    [NonSerialized] public float lanternTimeCounter = 0;
    [NonSerialized] public bool countLanternTime = false;
    [NonSerialized] private PlayerMovement playerMovement;
    private float speedDecrease = 0.1f;
    private float finalSpeedModifier = 1f;

    // private bool isCountingTime; // This variable was declared but not used

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        controls = new PlayerInput();
    }

    private void OnEnable()
    {
        controls.Enable(); // Enable controls
        HemannekenEventBus.OnHemannekenAttached += HemAttached;
        HemannekenEventBus.OnHemannekenDetached += HemDetached;
    }

    private void OnDisable()
    {
        controls.Disable(); // Disable controls
        HemannekenEventBus.OnHemannekenAttached -= HemAttached;
        HemannekenEventBus.OnHemannekenDetached -= HemDetached;
    }

    private void Update()
    {
        // "Hey" input logic is GONE from here

        if (controls.Player.RaiseLantern.WasPressedThisFrame())
        {
            isHoldingLantern = true;
        }
        else if (controls.Player.RaiseLantern.WasReleasedThisFrame())
        {
            isHoldingLantern = false;
        }

        if (isHoldingLantern && countLanternTime)
        {
            lanternTimeCounter += Time.deltaTime;
        }
        else
        {
            lanternTimeCounter = 0;
        }
    }

    private void HemAttached()
    {
        finalSpeedModifier -= speedDecrease;
        if (playerMovement != null) playerMovement.speedModifier = finalSpeedModifier;
    }

    private void HemDetached()
    {
        finalSpeedModifier += speedDecrease;
        if (playerMovement != null) playerMovement.speedModifier = finalSpeedModifier;
    }
}