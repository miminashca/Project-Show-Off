// WaterSensor.cs
using System;
using UnityEngine;

public class WaterSensor : MonoBehaviour
{
    private bool isCurrentlyUnderwater = false; // Renamed for clarity
    private bool wasUnderwaterLastFrame = false;
    private float timeUnderwater = 0f;

    [Tooltip("Assign the Layer that your water trigger colliders are on.")]
    [SerializeField] private LayerMask waterMask;

    void Update()
    {
        // Check for state change to fire events
        if (isCurrentlyUnderwater && !wasUnderwaterLastFrame)
        {
            // Just submerged
            //Debug.Log("SENSOR: Player SUBMERGED event fired.");
            WaterEventBus.InvokeSubmerge();
        }
        else if (!isCurrentlyUnderwater && wasUnderwaterLastFrame)
        {
            // Just emerged
            Debug.Log("SENSOR: Player EMERGED event fired.");
            WaterEventBus.InvokeEmerge();
        }

        // Update wasUnderwaterLastFrame for the next frame's comparison
        wasUnderwaterLastFrame = isCurrentlyUnderwater;

        // Manage time underwater
        if (isCurrentlyUnderwater)
        {
            timeUnderwater += Time.deltaTime;
        }
        else
        {
            timeUnderwater = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & waterMask.value) != 0)
        {
            // Check if already underwater to prevent multiple submerge events if overlapping water triggers
            if (!isCurrentlyUnderwater)
            {
                //Debug.Log($"SENSOR: Entered trigger with '{other.gameObject.name}'. Setting isCurrentlyUnderwater = true.");
                isCurrentlyUnderwater = true;
                // Event will be fired in Update based on state change
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & waterMask.value) != 0)
        {
            // Let's assume for a "thin surface" that exiting means you are out.
            //Debug.Log($"SENSOR: Exited trigger with '{other.gameObject.name}'. Setting isCurrentlyUnderwater = false.");
            isCurrentlyUnderwater = false;
            // Event will be fired in Update based on state change
        }
    }

    // --- Optional: For more robust check on exit if you have overlapping water triggers ---
    // (More complex, use OnTriggerStay or count trigger entries/exits if needed)
    /*
    private void OnTriggerStay(Collider other)
    {
        if (((1 << other.gameObject.layer) & waterMask.value) != 0)
        {
            // If we are staying in a water trigger, ensure we are marked as underwater
            // This helps if the OnTriggerExit might have prematurely set isCurrentlyUnderwater to false
            // due to exiting one trigger while still in another.
            if (!isCurrentlyUnderwater)
            {
                 isCurrentlyUnderwater = true; // Re-affirm we are in water
            }
        }
    }
    */


    public bool IsPlayerUnderwater() // Renamed for clarity
    {
        return isCurrentlyUnderwater;
    }

    public float GetTimeUnderwater()
    {
        return timeUnderwater; 
    }
}