using System;
using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(Collider))]
public class FuelPickup : MonoBehaviour
{
    private VisualEffect fire;
    private Transform fireHolder;

    public String fadeToggleName = "IsFadingUp";
    public String fadeIntensityName = "DISAPPEAR";
    private Vector3 fireInitialPos;
    private Vector3 fireEndPos;

    private bool isFading = false;
    private float timer = 0;
    private float threshold = 0;
    private float currentIntensity = 0;

    void Start()
    {
        // Ensure the collider is set to be a trigger
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning($"FuelPickup on {gameObject.name} needs its collider set to 'Is Trigger'. Setting it now.", this);
            col.isTrigger = true;
        }

        fire = GetComponentInChildren<VisualEffect>();
        fireHolder = fire.gameObject.transform.parent;
        fireInitialPos = fire.transform.position;
        fireEndPos = fireInitialPos;
        fireEndPos.y += 0.5f;
        fire.SetFloat(fadeIntensityName, 0f);
    }

    public void Refill(float pThreshold)
    {
        threshold = pThreshold;
        StartFade();
        ClueEventManager.Instance.PickUpFuel();
    }

    private void FixedUpdate()
    {
        if (isFading && Time.time - timer >= threshold)
        {
            StopFade();
        }
        if (isFading)
        {
            Fade();
        }
    }

    private void StartFade()
    {
        isFading = true;
        fire.SetBool(fadeToggleName, true);
        timer = Time.time;
    }
    private void StopFade()
    {
        isFading = false;
        fireHolder.transform.position = fireInitialPos;
        currentIntensity = 0f;
        fire.SetFloat(fadeIntensityName, currentIntensity);
        fire.SetBool(fadeToggleName, false);
    }
    private void Fade()
    {
        fireHolder.transform.position = Vector3.Lerp(fireHolder.transform.position, fireEndPos, Time.deltaTime*0.5f);
        if (currentIntensity < 10f)
        {
            currentIntensity += 0.03f;
            fire.SetFloat(fadeIntensityName, currentIntensity);
        }
    }
    
    // void OnTriggerEnter(Collider other)
    // {
    //     // Check if the object entering the trigger is the Player
    //     if (other.CompareTag("Player")) // Make sure your player GameObject has the "Player" tag
    //     {
    //         LanternController lantern = other.GetComponent<LanternController>();
    //         if (lantern != null)
    //         {
    //             Debug.Log("Player picked up fuel.");
    //             lantern.RefillFuel();
    //
    //             // Optional: Play pickup sound
    //             // AudioSource.PlayClipAtPoint(pickupSound, transform.position);
    //
    //             // Destroy the fuel bottle object
    //             Destroy(gameObject);
    //         }
    //         else
    //         {
    //             Debug.LogWarning($"Player tagged object entered FuelPickup trigger, but no LanternController found on {other.name}.", other);
    //         }
    //     }
    // }
}