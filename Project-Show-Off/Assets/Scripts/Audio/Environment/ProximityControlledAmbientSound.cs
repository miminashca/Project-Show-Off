// ProximityControlledAmbientSound.cs (For looping/continuous sounds like crickets)
using System.Collections;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class ProximityControlledAmbientSound : MonoBehaviour
{
    [Header("FMOD Event Settings")]
    public EventReference fmodEvent;
    public float minDelay = 5f;
    public float maxDelay = 10f;

    [Header("Player Proximity Settings")]
    public float quietZoneDistancePlayer = 40f;

    // NEW CHANGE
    [Header("Hunter Proximity Settings")]
    [Tooltip("Layer(s) the Hunter GameObjects are on. Leave empty (Nothing) to disable hunter checks.")]
    public LayerMask hunterLayer;
    [Tooltip("The maximum radius around this sound emitter to scan for Hunters on the specified layer.")]
    public float hunterDetectionRadius = 50f; // How far to even look for a hunter
    [Tooltip("Sound stops if the closest detected Hunter is closer than this distance.")]
    public float quietZoneDistanceHunter = 30f;
    [Tooltip("How often (in seconds) to scan for the Hunter if not currently found or if the reference is lost.")]
    public float hunterScanInterval = 2.0f;

    private Transform hunterTransform; // Will store the transform of the *closest* detected hunter
    private float lastHunterScanTime;
    private static Collider[] hunterCollidersCache = new Collider[10]; // Cache for OverlapSphere results, adjust size if needed
    // END CHANGE

    private Transform playerTransform;
    private Coroutine soundManagementRoutine;

    private EventInstance soundInstance;
    private bool isSoundPlaying = false;

    void Awake()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogWarning($"ProximityControlledAmbientSound ({gameObject.name}): Player GameObject with tag 'Player' not found! " +
                           "Player distance checks will not function. The script will still attempt hunter checks if a hunter layer is set.");
        }

        // NEW CHANGE
        if (hunterLayer != 0) // LayerMask is 0 if 'Nothing' is selected
        {
            TryFindHunter();
        }
        else
        {
            Debug.Log($"ProximityControlledAmbientSound ({gameObject.name}): Hunter Layer is not set (set to 'Nothing'). Hunter proximity checks will be disabled.");
        }
        lastHunterScanTime = Time.time;
        // END CHANGE
    }

    void Start()
    {
        if (!fmodEvent.IsNull)
        {
            soundInstance = RuntimeManager.CreateInstance(fmodEvent);
            soundInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
        }
        else
        {
            Debug.LogError($"ProximityControlledAmbientSound ({gameObject.name}): FMOD Event Reference is NOT assigned. This emitter will not function.");
            enabled = false;
            return;
        }
        soundManagementRoutine = StartCoroutine(ManageSoundProximityAndRandomIntervals());
    }

    // NEW CHANGE
    void TryFindHunter()
    {
        if (hunterLayer == 0) // 0 means 'Nothing' layer
        {
            hunterTransform = null;
            return;
        }

        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, hunterDetectionRadius, hunterCollidersCache, hunterLayer);
        Transform closestHunter = null;
        float minDistanceSq = float.MaxValue;

        if (numColliders > 0)
        {
            for (int i = 0; i < numColliders; i++)
            {
                // Ensure we don't consider this GameObject itself if it's somehow on the hunter layer
                if (hunterCollidersCache[i].transform == transform) continue;

                float distanceSq = (hunterCollidersCache[i].transform.position - transform.position).sqrMagnitude;
                if (distanceSq < minDistanceSq)
                {
                    minDistanceSq = distanceSq;
                    closestHunter = hunterCollidersCache[i].transform;
                }
            }
        }

        if (hunterTransform != closestHunter) // Only log if it changes
        {
            // if (closestHunter != null)
            // {
            //     Debug.Log($"ProximityControlledAmbientSound ({gameObject.name}): Found closest Hunter '{closestHunter.name}' on layer.");
            // }
            // else if (hunterTransform != null) // Was tracking one, but now it's gone (or out of range)
            // {
            //     Debug.Log($"ProximityControlledAmbientSound ({gameObject.name}): Previously tracked Hunter is no longer the closest or within detection range.");
            // }
        }
        hunterTransform = closestHunter;
    }
    // END CHANGE

    void Update()
    {
        if (soundInstance.isValid())
        {
            soundInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
        }

        // NEW CHANGE
        if (hunterLayer != 0 && Time.time > lastHunterScanTime + hunterScanInterval)
        {
            // Always rescan to find the *currently closest* hunter or if the previous one was destroyed/moved
            TryFindHunter();
            lastHunterScanTime = Time.time;
        }
        // END CHANGE
    }

    IEnumerator ManageSoundProximityAndRandomIntervals()
    {
        while (true)
        {
            bool shouldBeQuiet = false;

            // Player Check
            if (playerTransform != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                if (distanceToPlayer < quietZoneDistancePlayer)
                {
                    shouldBeQuiet = true;
                    // Debug.Log($"ProximityControlledAmbientSound ({gameObject.name}): Player in quiet zone ({distanceToPlayer:F2}m).");
                }
            }

            // NEW CHANGE
            // Hunter Check (only if not already quieted by player and hunter tracking is active)
            if (!shouldBeQuiet && hunterLayer != 0 && hunterTransform != null)
            {
                // hunterTransform is already the closest one found within hunterDetectionRadius by TryFindHunter()
                float distanceToHunter = Vector3.Distance(transform.position, hunterTransform.position);
                if (distanceToHunter < quietZoneDistanceHunter)
                {
                    shouldBeQuiet = true;
                    // Debug.Log($"ProximityControlledAmbientSound ({gameObject.name}): Hunter '{hunterTransform.name}' in quiet zone ({distanceToHunter:F2}m).");
                }
            }
            // END CHANGE

            if (playerTransform == null && (hunterLayer == 0 || hunterTransform == null))
            {
                if (isSoundPlaying)
                {
                    soundInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    isSoundPlaying = false;
                }
                yield return new WaitForSeconds(hunterScanInterval > 0 ? hunterScanInterval : 2.0f);
                continue;
            }

            if (shouldBeQuiet)
            {
                if (isSoundPlaying)
                {
                    if (soundInstance.isValid()) soundInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    isSoundPlaying = false;
                    // Debug.Log($"ProximityControlledAmbientSound ({gameObject.name}): Sound stopped due to proximity.");
                }
            }
            else
            {
                if (!isSoundPlaying)
                {
                    float delay = Random.Range(minDelay, maxDelay);
                    // Debug.Log($"ProximityControlledAmbientSound ({gameObject.name}): Outside quiet zones. Waiting {delay:F2}s before playing.");
                    yield return new WaitForSeconds(delay);

                    bool stillSafeToPlay = true;
                    if (playerTransform != null)
                    {
                        if (Vector3.Distance(transform.position, playerTransform.position) < quietZoneDistancePlayer)
                        {
                            stillSafeToPlay = false;
                            // Debug.Log($"ProximityControlledAmbientSound ({gameObject.name}): Player re-entered quiet zone during delay.");
                        }
                    }

                    // NEW CHANGE
                    if (stillSafeToPlay && hunterLayer != 0)
                    {
                        // Re-evaluate closest hunter after delay, as it might have changed or moved
                        TryFindHunter(); // Quick re-scan for current closest
                        if (hunterTransform != null)
                        {
                            if (Vector3.Distance(transform.position, hunterTransform.position) < quietZoneDistanceHunter)
                            {
                                stillSafeToPlay = false;
                                // Debug.Log($"ProximityControlledAmbientSound ({gameObject.name}): Hunter '{hunterTransform.name}' re-entered quiet zone during delay.");
                            }
                        }
                    }
                    // END CHANGE

                    if (stillSafeToPlay)
                    {
                        if (soundInstance.isValid())
                        {
                            soundInstance.start();
                            isSoundPlaying = true;
                            // Debug.Log($"ProximityControlledAmbientSound ({gameObject.name}): Sound started.");
                        }
                        else
                        {
                            Debug.LogWarning($"ProximityControlledAmbientSound ({gameObject.name}): Sound instance was invalid before start. Attempting to re-create.");
                            if (!fmodEvent.IsNull)
                            {
                                soundInstance = RuntimeManager.CreateInstance(fmodEvent);
                                soundInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
                                soundInstance.start();
                                isSoundPlaying = true;
                            }
                        }
                    }
                    // else
                    // {
                    // Debug.Log($"ProximityControlledAmbientSound ({gameObject.name}): Conditions changed during delay. Skipping start.");
                    // }
                }
            }
            yield return null;
        }
    }

    void OnDisable()
    {
        StopSoundManagement();
        if (soundInstance.isValid())
        {
            soundInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            soundInstance.release();
        }
        isSoundPlaying = false;
    }

    void OnDestroy()
    {
        OnDisable();
    }

    public void StopSoundManagement()
    {
        if (soundManagementRoutine != null)
        {
            StopCoroutine(soundManagementRoutine);
            soundManagementRoutine = null;
        }
    }
}