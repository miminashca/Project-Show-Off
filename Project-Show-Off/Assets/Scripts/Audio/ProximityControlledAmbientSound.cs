// ProximityControlledAmbientSound.cs (For looping/continuous sounds like crickets)
using System.Collections;
using UnityEngine;
using FMODUnity;
using FMOD.Studio; // IMPORTANT: Add this for EventInstance

public class ProximityControlledAmbientSound : MonoBehaviour
{
    [Header("FMOD Event Settings")]
    public EventReference fmodEvent; // Drag your FMOD event here
    public float minDelay = 5f; // Delay before starting if player leaves quiet zone
    public float maxDelay = 10f; // Max delay before starting if player leaves quiet zone

    [Header("Player Proximity Settings")]
    public float quietZoneDistance = 40f; // Sound stops if player is closer than this distance

    // Reference to the player's Transform.
    private Transform playerTransform;
    private Coroutine soundManagementRoutine; // Renamed for clarity

    // --- FMOD Event Instance for continuous control ---
    private EventInstance soundInstance;
    private bool isSoundPlaying = false; // Script's internal tracking of sound state

    void Awake()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogError($"ProximityControlledAmbientSound ({gameObject.name}): Player GameObject with tag 'Player' not found! " +
                           "Please ensure your player has the 'Player' tag or assign 'playerTransform' manually in the inspector. " +
                           "Distance checks will not function correctly.");
        }
    }

    void Start()
    {
        // Create the FMOD EventInstance once
        if (!fmodEvent.IsNull)
        {
            soundInstance = RuntimeManager.CreateInstance(fmodEvent);
            // Set 3D attributes immediately, as this instance will persist and move with the emitter
            soundInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
        }
        else
        {
            Debug.LogError($"ProximityControlledAmbientSound ({gameObject.name}): FMOD Event Reference is NOT assigned. This emitter will not function.");
            enabled = false; // Disable script if no event is assigned
            return;
        }

        soundManagementRoutine = StartCoroutine(ManageSoundProximityAndRandomIntervals());
    }

    void Update()
    {
        // Continuously update 3D attributes for persistent instances, as emitter might move
        if (soundInstance.isValid())
        {
            soundInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
        }
    }

    IEnumerator ManageSoundProximityAndRandomIntervals()
    {
        while (true)
        {
            if (playerTransform == null)
            {
                yield return new WaitForSeconds(1f); // Wait if player not found
                continue;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (distanceToPlayer < quietZoneDistance)
            {
                // Player is too close, stop the sound if it's currently playing
                if (isSoundPlaying)
                {
                    soundInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    isSoundPlaying = false; // Update our internal state
                    // Debug.Log($"ProximityControlledAmbientSound ({gameObject.name}): Player entered quiet zone ({distanceToPlayer:F2}m). Sound stopped.");
                }
            }
            else // Player is outside the quiet zone
            {
                // Player is far enough, try to play the sound if it's not already playing
                if (!isSoundPlaying)
                {
                    float delay = Random.Range(minDelay, maxDelay);
                    // Debug.Log($"ProximityControlledAmbientSound ({gameObject.name}): Player left quiet zone ({distanceToPlayer:F2}m). Waiting {delay:F2}s before playing.");
                    yield return new WaitForSeconds(delay);

                    // IMPORTANT: Re-check distance after the delay, as player might have moved back
                    distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                    if (distanceToPlayer >= quietZoneDistance)
                    {
                        soundInstance.start();
                        isSoundPlaying = true; // Update our internal state
                        // Debug.Log($"ProximityControlledAmbientSound ({gameObject.name}): Sound started. Distance: {distanceToPlayer:F2}m.");
                    }
                    else
                    {
                        // Debug.Log($"ProximityControlledAmbientSound ({gameObject.name}): Player re-entered quiet zone during delay. Skipping start.");
                    }
                }
            }
            yield return null; // Continue checking every frame
        }
    }

    void OnDisable()
    {
        // First, stop the coroutine to prevent further management calls
        StopSoundManagement();

        // Then, ensure the FMOD instance is stopped and released
        if (soundInstance.isValid())
        {
            soundInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            soundInstance.release();
            soundInstance.clearHandle(); // Clear handle (good practice for re-enabling)
            // Debug.Log($"ProximityControlledAmbientSound ({gameObject.name}): FMOD instance stopped and released on disable.");
        }
    }

    void OnDestroy()
    {
        // OnDisable also handles OnDestroy, but explicit release is good practice
        // if you might disable before destroying.
        OnDisable(); // Ensure cleanup happens
    }

    public void StopSoundManagement()
    {
        if (soundManagementRoutine != null)
        {
            StopCoroutine(soundManagementRoutine);
            soundManagementRoutine = null;
            // Debug.Log($"ProximityControlledAmbientSound ({gameObject.name}): Stopped sound management coroutine.");
        }
    }
}