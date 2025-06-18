using UnityEngine;
using FMODUnity; // Required for FMOD types like EventReference and RuntimeManager
using System.Collections; // Required for Coroutines

public class HemannekenSoundController : MonoBehaviour
{
    [Header("FMOD Event Paths - Hemanneken")]
    [SerializeField] private EventReference idleSound;
    [SerializeField] private EventReference farHeySound;
    [SerializeField] private EventReference midHeySound;
    [SerializeField] private EventReference stunnedSound;
    [SerializeField] private EventReference deadSound;
    [SerializeField] private EventReference attachSound;
    [SerializeField] private EventReference closeHeySound;

    [Header("Sound Settings")]
    [SerializeField] private float closeHeyInterval = 5f; // How often the close hey sound plays when attached

    // For the looped idle sound (Option 1: Using an FMOD Studio Event Emitter component)
    private StudioEventEmitter idleEventEmitter;

    // For the repeating "Close Hey" sound
    private Coroutine closeHeyCoroutineInstance;
    private FMOD.Studio.EventInstance closeHeyEventInstance;

    void Awake()
    {
        idleEventEmitter = GetComponent<StudioEventEmitter>();
        if (idleEventEmitter != null)
        {
            // Warning if script's idleSound is not set AND emitter's EventReference is also not set.
            if (idleSound.IsNull && idleEventEmitter.EventReference.IsNull)
            {
                Debug.LogWarning($"HemannekenSoundController: Idle sound EventReference is not set in the script, and the attached StudioEventEmitter on {gameObject.name} also has no event assigned. Assign the event to the script's 'idleSound' field or directly to the emitter component's EventReference field.");
            }
        }
        else if (!idleSound.IsNull) // Script has an idle sound, but no emitter component found
        {
            Debug.LogWarning($"HemannekenSoundController: Idle sound EventReference is set in the script, but no StudioEventEmitter component found on {gameObject.name}. Idle sound might not play as intended. Consider adding an emitter component.");
        }
    }

    // --- Public Methods to be called by Hemanneken's AI/Logic Scripts ---

    #region Idle Sound
    public void StartIdleSound()
    {
        if (idleEventEmitter != null)
        {
            EventReference eventToPlay = new EventReference(); // Determine which event to use

            if (!idleSound.IsNull) // Prioritize the event set in this script
            {
                eventToPlay = idleSound;
            }
            else if (!idleEventEmitter.EventReference.IsNull) // Fallback to emitter's own EventReference if script's is null
            {
                eventToPlay = idleEventEmitter.EventReference;
                // Debug.Log($"HemannekenSoundController: Using idle sound directly assigned to StudioEventEmitter on {gameObject.name} as script's idleSound field is null.");
            }

            if (!eventToPlay.IsNull)
            {
                // If the emitter's current event is different, update it
                if (idleEventEmitter.EventReference.Path != eventToPlay.Path)
                {
                    if (idleEventEmitter.IsPlaying())
                    {
                        idleEventEmitter.Stop();
                    }
                    idleEventEmitter.EventReference = eventToPlay;
                }

                if (!idleEventEmitter.IsPlaying())
                {
                    idleEventEmitter.Play();
                }
            }
            else
            {
                Debug.LogWarning($"HemannekenSoundController on {gameObject.name}: Tried to start Idle sound, but no valid FMOD Event is assigned either in the script or on the StudioEventEmitter component.");
            }
        }
        else if (!idleSound.IsNull) // Emitter is null, but script has an event.
        {
            Debug.LogWarning($"HemannekenSoundController on {gameObject.name}: Tried to start Idle sound. 'idleSound' FMOD Event is assigned in script, but no StudioEventEmitter component found.");
        }
    }

    public void StopIdleSound()
    {
        if (idleEventEmitter != null && idleEventEmitter.IsPlaying())
        {
            idleEventEmitter.Stop();
        }
    }
    #endregion

    #region Hey Sounds
    public void PlayFarHeySound()
    {
        if (!farHeySound.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(farHeySound, gameObject);
        }
        else
        {
            Debug.LogWarning($"HemannekenSoundController on {gameObject.name}: 'farHeySound' FMOD Event is not assigned.");
        }
    }

    public void PlayMidHeySound()
    {
        if (!midHeySound.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(midHeySound, gameObject);
        }
        else
        {
            Debug.LogWarning($"HemannekenSoundController on {gameObject.name}: 'midHeySound' FMOD Event is not assigned.");
        }
    }
    #endregion

    #region State Sounds
    public void PlayStunnedSound()
    {
        if (!stunnedSound.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(stunnedSound, gameObject);
        }
        else
        {
            Debug.LogWarning($"HemannekenSoundController on {gameObject.name}: 'stunnedSound' FMOD Event is not assigned.");
        }
    }

    public void PlayDeadSound()
    {
        StopAllHemannekenSounds(); // Stop ongoing sounds before playing death sound

        if (!deadSound.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(deadSound, gameObject);
        }
        else
        {
            Debug.LogWarning($"HemannekenSoundController on {gameObject.name}: 'deadSound' FMOD Event is not assigned.");
        }
    }
    #endregion

    #region Attach Sounds
    public void PlayAttachSound()
    {
        if (!attachSound.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(attachSound, gameObject);
        }
        else
        {
            Debug.LogWarning($"HemannekenSoundController on {gameObject.name}: 'attachSound' FMOD Event is not assigned.");
        }
    }

    public void StartCloseHeyLoop()
    {
        if (closeHeyCoroutineInstance == null)
        {
            if (!closeHeySound.IsNull)
            {
                closeHeyCoroutineInstance = StartCoroutine(CloseHeyCoroutine());
            }
            else
            {
                Debug.LogWarning($"HemannekenSoundController on {gameObject.name}: 'closeHeySound' FMOD Event is not assigned. Cannot start loop.");
            }
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

    // --- Utility ---
    public void StopAllHemannekenSounds()
    {
        StopIdleSound();
        StopCloseHeyLoop();
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