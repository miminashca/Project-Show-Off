using UnityEngine;
using FMODUnity; // Required for FMOD types like EventReference and RuntimeManager
using System.Collections; // Required for Coroutines

public class HemannekenSoundController : MonoBehaviour
{
    [Header("FMOD Event Paths - Hemanneken")]
    [SerializeField] private EventReference idleSound;
    [SerializeField] private EventReference farHeySound; // Used by RespondToPlayerHey
    [SerializeField] private EventReference midHeySound; // Used by RespondToPlayerHey
    [SerializeField] private EventReference stunnedSound;
    [SerializeField] private EventReference deadSound;
    [SerializeField] private EventReference attachSound;
    [SerializeField] private EventReference closeHeySound; // For when attached

    [Header("Sound Settings")]
    [SerializeField] private float closeHeyInterval = 5f;

    // --- MODIFIED: Changed to a min/max range for more natural timing ---
    [Tooltip("The minimum time (in seconds) between each automatic 'Hey' call.")]
    [SerializeField] private float minPeriodicHeyInterval = 45f;
    [Tooltip("The maximum time (in seconds) between each automatic 'Hey' call.")]
    [SerializeField] private float maxPeriodicHeyInterval = 75f;
    // --- END MODIFIED ---

    [Header("Distance Thresholds")]
    [Tooltip("Distance beyond which the 'Far Hey' sound is used for player callback.")]
    [SerializeField] private float farHeyResponseThreshold = 50f;

    private StudioEventEmitter idleEventEmitter;
    private Coroutine closeHeyCoroutineInstance;
    private FMOD.Studio.EventInstance closeHeyEventInstance;

    private Coroutine periodicHeyCoroutineInstance;
    private Transform playerTransform; // Cache the player's transform for performance

    void Awake()
    {
        idleEventEmitter = GetComponent<StudioEventEmitter>();
        if (idleEventEmitter != null)
        {
            if (idleSound.IsNull && idleEventEmitter.EventReference.IsNull)
            {
                Debug.LogWarning($"HemannekenSoundController: Idle sound EventReference is not set in the script, and the attached StudioEventEmitter on {gameObject.name} also has no event assigned.");
            }
        }
        else if (!idleSound.IsNull)
        {
            Debug.LogWarning($"HemannekenSoundController: Idle sound EventReference is set, but no StudioEventEmitter component found on {gameObject.name}.");
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogError($"HemannekenSoundController on {gameObject.name}: Could not find a GameObject with the 'Player' tag. Periodic 'Hey' sounds will not work.");
        }
    }

    #region Idle Sound
    public void StartIdleSound()
    {
        if (idleEventEmitter != null)
        {
            EventReference eventToPlay = idleSound.IsNull ? idleEventEmitter.EventReference : idleSound;
            if (!eventToPlay.IsNull)
            {
                if (idleEventEmitter.EventReference.Guid != eventToPlay.Guid)
                {
                    if (idleEventEmitter.IsPlaying()) idleEventEmitter.Stop();
                    idleEventEmitter.EventReference = eventToPlay;
                }
                if (!idleEventEmitter.IsPlaying()) idleEventEmitter.Play();
            }
            else Debug.LogWarning($"HemannekenSoundController on {gameObject.name}: No Idle FMOD Event assigned.");
        }
        else if (!idleSound.IsNull)
            Debug.LogWarning($"HemannekenSoundController on {gameObject.name}: 'idleSound' assigned, but no StudioEventEmitter component found.");
    }

    public void StopIdleSound()
    {
        if (idleEventEmitter != null && idleEventEmitter.IsPlaying())
        {
            idleEventEmitter.Stop();
        }
    }
    #endregion

    #region Player Hey! Callback
    public void RespondToPlayerHey(Transform playerTransform)
    {
        if (playerTransform == null)
        {
            Debug.LogWarning($"HemannekenSoundController on {gameObject.name}: PlayerTransform is null in RespondToPlayerHey. Cannot determine distance.");
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer > farHeyResponseThreshold)
        {
            if (!farHeySound.IsNull)
            {
                RuntimeManager.PlayOneShotAttached(farHeySound, gameObject);
            }
            else
            {
                Debug.LogWarning($"HemannekenSoundController on {gameObject.name}: 'farHeySound' FMOD Event is not assigned for player callback.");
            }
        }
        else
        {
            if (!midHeySound.IsNull)
            {
                RuntimeManager.PlayOneShotAttached(midHeySound, gameObject);
            }
            else
            {
                Debug.LogWarning($"HemannekenSoundController on {gameObject.name}: 'midHeySound' FMOD Event is not assigned for player callback.");
            }
        }
    }
    #endregion

    #region Periodic Hey Loop
    public void StartPeriodicHeyLoop()
    {
        if (periodicHeyCoroutineInstance == null)
        {
            if (playerTransform != null)
            {
                Debug.Log($"<color=green>SOUND:</color> Starting Periodic Hey Loop on {gameObject.name}.");
                periodicHeyCoroutineInstance = StartCoroutine(PeriodicHeyCoroutine());
            }
            else
            {
                Debug.LogWarning($"HemannekenSoundController on {gameObject.name}: Cannot start PeriodicHeyLoop because Player Transform was not found.");
            }
        }
    }

    public void StopPeriodicHeyLoop()
    {
        if (periodicHeyCoroutineInstance != null)
        {
            Debug.Log($"<color=green>SOUND:</color> Stopping Periodic Hey Loop on {gameObject.name}.");
            StopCoroutine(periodicHeyCoroutineInstance);
            periodicHeyCoroutineInstance = null;
        }
    }

    // --- MODIFIED COROUTINE ---
    private IEnumerator PeriodicHeyCoroutine()
    {
        // 1. Initial random delay to de-synchronize all Hemanneken at the start.
        // This waits for a random time between 1 and 20 seconds before the first call.
        yield return new WaitForSeconds(Random.Range(100.0f, 200.0f));

        while (true)
        {
            // Call the existing response method, using the cached player transform
            RespondToPlayerHey(playerTransform);

            // 2. Wait for a new random interval before the next call.
            float waitTime = Random.Range(minPeriodicHeyInterval, maxPeriodicHeyInterval);
            yield return new WaitForSeconds(waitTime);
        }
    }
    // --- END MODIFIED ---
    #endregion

    #region State Sounds
    public void PlayStunnedSound()
    {
        if (!stunnedSound.IsNull)
            RuntimeManager.PlayOneShotAttached(stunnedSound, gameObject);
        else
            Debug.LogWarning($"HemannekenSoundController on {gameObject.name}: 'stunnedSound' FMOD Event is not assigned.");
    }

    public void PlayDeadSound()
    {
        StopAllHemannekenSounds();
        if (!deadSound.IsNull)
            RuntimeManager.PlayOneShotAttached(deadSound, gameObject);
        else
            Debug.LogWarning($"HemannekenSoundController on {gameObject.name}: 'deadSound' FMOD Event is not assigned.");
    }
    #endregion

    #region Attach Sounds (Periodic Close Hey)
    public void PlayAttachSound()
    {
        if (!attachSound.IsNull)
            RuntimeManager.PlayOneShotAttached(attachSound, gameObject);
        else
            Debug.LogWarning($"HemannekenSoundController on {gameObject.name}: 'attachSound' FMOD Event is not assigned.");
    }

    public void StartCloseHeyLoop()
    {
        if (closeHeyCoroutineInstance == null)
        {
            if (!closeHeySound.IsNull)
                closeHeyCoroutineInstance = StartCoroutine(CloseHeyCoroutine());
            else
                Debug.LogWarning($"HemannekenSoundController on {gameObject.name}: 'closeHeySound' FMOD Event is not assigned. Cannot start loop.");
        }
    }

    public void StopCloseHeyLoop()
    {
        if (closeHeyCoroutineInstance != null)
        {
            StopCoroutine(closeHeyCoroutineInstance);
            closeHeyCoroutineInstance = null;
        }
        if (closeHeyEventInstance.isValid())
        {
            closeHeyEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            closeHeyEventInstance.release();
        }
    }

    private IEnumerator CloseHeyCoroutine()
    {
        while (true)
        {
            if (closeHeyEventInstance.isValid())
            {
                closeHeyEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                closeHeyEventInstance.release();
            }
            closeHeyEventInstance = RuntimeManager.CreateInstance(closeHeySound);
            RuntimeManager.AttachInstanceToGameObject(closeHeyEventInstance, gameObject);
            closeHeyEventInstance.start();
            yield return new WaitForSeconds(closeHeyInterval);
        }
    }
    #endregion

    public void StopAllHemannekenSounds()
    {
        StopIdleSound();
        StopCloseHeyLoop();
        StopPeriodicHeyLoop();
    }

    void OnDestroy()
    {
        StopAllHemannekenSounds();
        if (closeHeyEventInstance.isValid())
        {
            closeHeyEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            closeHeyEventInstance.release();
        }
    }

    // --- ADDED: OnValidate to keep inspector values logical ---
    private void OnValidate()
    {
        if (minPeriodicHeyInterval < 1.0f)
        {
            minPeriodicHeyInterval = 1.0f;
        }
        if (maxPeriodicHeyInterval < minPeriodicHeyInterval)
        {
            maxPeriodicHeyInterval = minPeriodicHeyInterval + 1.0f;
        }
    }
}