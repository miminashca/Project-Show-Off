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

    private bool isCountingTime;
    
    private void OnEnable()
    {
        playerMovement = GetComponent<PlayerMovement>();
        controls = new PlayerInput();
        controls.Enable();
        HemannekenEventBus.OnHemannekenAttached += HemAttached;
        HemannekenEventBus.OnHemannekenDetached += HemDetached;
    }
    private void OnDisable()
    {
        controls.Disable();
        HemannekenEventBus.OnHemannekenAttached -= HemAttached;
        HemannekenEventBus.OnHemannekenDetached -= HemDetached;
    }
    private void Update()
    {
        if (controls.Hemanneken.Hey.triggered) HemannekenEventBus.TriggerHey();

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
            Debug.Log(lanternTimeCounter);
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
        playerMovement.speedModifier = finalSpeedModifier;
    }
    private void HemDetached()
    {
        finalSpeedModifier += speedDecrease;
        playerMovement.speedModifier = finalSpeedModifier;
    }
}
