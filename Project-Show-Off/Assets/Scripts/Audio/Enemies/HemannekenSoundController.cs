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
    // NEW FMOD CHANGE
    [Tooltip("The time in seconds between each automatic 'Hey' response from the Hemanneken during its periodic loop.")]
    [SerializeField] private float periodicHeyInterval = 60f;
    // END FMOD CHANGE

    [Header("Distance Thresholds")]
    [Tooltip("Distance beyond which the 'Far Hey' sound is used for player callback.")]
    [SerializeField] private float farHeyResponseThreshold = 50f;

    private StudioEventEmitter idleEventEmitter;
    private Coroutine closeHeyCoroutineInstance;
    private FMOD.Studio.EventInstance closeHeyEventInstance;

    // NEW FMOD CHANGE
    private Coroutine periodicHeyCoroutineInstance;
    private Transform playerTransform; // Cache the player's transform for performance
    // END FMOD CHANGE

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

        // NEW FMOD CHANGE
        // Find the player's transform to be used by the periodic hey response.
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogError($"HemannekenSoundController on {gameObject.name}: Could not find a GameObject with the 'Player' tag. Periodic 'Hey' sounds will not work.");
        }
        // END FMOD CHANGE
    }

    // --- Public Methods ---

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
    /// <summary>
    /// Called when the Hemanneken should respond to the player's 'Hey!'.
    /// Plays either farHeySound or midHeySound based on distance to the player.
    /// </summary>
    /// <param name="playerTransform">The Transform of the player who shouted.</param>
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
        else // Mid-range or closer for the callback (but not the 'attached' closeHeySound)
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

    // NEW FMOD CHANGE
    #region Periodic Hey Loop
    /// <summary>
    /// Starts a loop that makes the Hemanneken automatically respond with a 'Hey' at a regular interval.
    /// </summary>
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

    /// <summary>
    /// Stops the periodic 'Hey' response loop.
    /// </summary>
    public void StopPeriodicHeyLoop()
    {
        if (periodicHeyCoroutineInstance != null)
        {
            Debug.Log($"<color=green>SOUND:</color> Stopping Periodic Hey Loop on {gameObject.name}.");
            StopCoroutine(periodicHeyCoroutineInstance);
            periodicHeyCoroutineInstance = null;
        }
    }

    private IEnumerator PeriodicHeyCoroutine()
    {
        while (true)
        {
            // Wait for the specified interval before playing the sound
            yield return new WaitForSeconds(periodicHeyInterval);

            // Call the existing response method, using the cached player transform
            RespondToPlayerHey(playerTransform);
        }
    }
    #endregion
    // END FMOD CHANGE

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
    public void PlayAttachSound() // Sound for the moment of attachment
    {
        if (!attachSound.IsNull)
            RuntimeManager.PlayOneShotAttached(attachSound, gameObject);
        else
            Debug.LogWarning($"HemannekenSoundController on {gameObject.name}: 'attachSound' FMOD Event is not assigned.");
    }

    public void StartCloseHeyLoop() // Periodic 'Hey' when attached
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
        // NEW FMOD CHANGE
        StopPeriodicHeyLoop();
        // END FMOD CHANGE
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
}