using UnityEngine;
using FMODUnity; // Required for FMOD event references and RuntimeManager
using FMOD.Studio; // Required for EventInstance (though less direct management now)

public class PlayerWaterInteraction : MonoBehaviour
{
    [Header("Detection Settings")]
    public Transform headTransform;     // Assign your player's camera or head reference
    public LayerMask waterSurfaceLayer; // Set this to your "WaterSurface" layer
    public float raycastDistance = 1.0f; // How far up from the head to check for water

    [Header("FMOD Events")]
    [Tooltip("Looping ambience event. Ensure the LPF snapshot is triggered by this event in FMOD Studio.")]
    public EventReference underwaterAmbienceEvent;
    public EventReference submergeSoundEvent;
    public EventReference emergeSoundEvent;

    private bool isUnderwater = false;
    private bool wasUnderwaterLastFrame = false;

    private EventInstance underwaterAmbienceInstance;

    void Start()
    {
        // --- Essential: Head Transform ---
        if (headTransform == null)
        {
            if (Camera.main != null)
            {
                headTransform = Camera.main.transform;
                Debug.Log("PlayerWaterInteraction: Head transform automatically set to Main Camera.");
            }
            else
            {
                Debug.LogError("PlayerWaterInteraction: 'Head Transform' is not assigned, and Main Camera could not be found! Disabling script.");
                enabled = false; // Disable the script if no head transform
                return;
            }
        }

        // --- FMOD Setup ---
        // Validate EventReferences (optional but good practice)
        if (underwaterAmbienceEvent.IsNull)
            Debug.LogError("PlayerWaterInteraction: 'Underwater Ambience Event' is not assigned!");
        if (submergeSoundEvent.IsNull)
            Debug.LogError("PlayerWaterInteraction: 'Submerge Sound Event' is not assigned!");
        if (emergeSoundEvent.IsNull)
            Debug.LogError("PlayerWaterInteraction: 'Emerge Sound Event' is not assigned!");

        // Pre-create the instance for the looping underwater ambience
        // This allows us to start/stop it and attach it.
        if (!underwaterAmbienceEvent.IsNull)
        {
            underwaterAmbienceInstance = RuntimeManager.CreateInstance(underwaterAmbienceEvent);
            // Attach the ambience sound to the player (or headTransform) so it moves with them
            RuntimeManager.AttachInstanceToGameObject(underwaterAmbienceInstance, headTransform.gameObject, GetComponent<Rigidbody>());

        }
    }

    void Update()
    {
        if (headTransform == null) return; // Should be caught by Start, but good for safety

        wasUnderwaterLastFrame = isUnderwater;
        CheckIfUnderwater();

        // --- Handle State Transitions ---
        if (isUnderwater && !wasUnderwaterLastFrame)
        {
            OnEnterWater();
        }
        else if (!isUnderwater && wasUnderwaterLastFrame)
        {
            OnExitWater();
        }
    }

    void CheckIfUnderwater()
    {
        // Shoot a raycast upwards from the headTransform's position
        // It checks if it hits anything on the 'waterSurfaceLayer' within 'raycastDistance'
        isUnderwater = Physics.Raycast(headTransform.position, Vector3.up, raycastDistance, waterSurfaceLayer);

        // --- Optional: Visualize the Raycast in Scene View ---
#if UNITY_EDITOR
        Color rayColor = isUnderwater ? Color.cyan : Color.yellow;
        Debug.DrawRay(headTransform.position, Vector3.up * raycastDistance, rayColor);
#endif
    }

    void OnEnterWater()
    {
        Debug.Log("Player SUBMERGED");

        // Play the one-shot submerge sound
        if (!submergeSoundEvent.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(submergeSoundEvent, gameObject);
        }

        // Start the looping underwater ambience (which should trigger the LPF snapshot in FMOD)
        if (underwaterAmbienceInstance.isValid())
        {
            underwaterAmbienceInstance.start();
        }
    }

    void OnExitWater()
    {
        Debug.Log("Player EMERGED");

        // Play the one-shot emerge sound
        if (!emergeSoundEvent.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(emergeSoundEvent, gameObject);
        }

        // Stop the looping underwater ambience (which should stop the LPF snapshot's influence)
        if (underwaterAmbienceInstance.isValid())
        {
            // Allow FMOD's AHDSR envelope to fade out the sound and snapshot naturally
            underwaterAmbienceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
    }

    void OnDestroy()
    {
        // --- Clean up FMOD instances when this GameObject is destroyed ---
        if (underwaterAmbienceInstance.isValid())
        {
            // Stop immediately and release resources
            underwaterAmbienceInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            underwaterAmbienceInstance.release();
        }
    }
}