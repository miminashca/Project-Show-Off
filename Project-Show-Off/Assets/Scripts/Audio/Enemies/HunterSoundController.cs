using UnityEngine;
using FMODUnity; // Required for FMOD integration
using System.Collections; // Required for Coroutines (for Idle Grunts)

public class HunterSoundController : MonoBehaviour
{
    [Header("FMOD Event Paths - Hunter")]

    [Header("Ambient/State Sounds")]
    [SerializeField] private EventReference poemMonologueSound;
    [SerializeField] private EventReference aggressiveYellSound;
    [SerializeField] private EventReference idleGruntSound; // This should be a multi-sound in FMOD
    [SerializeField] private EventReference investigativeGruntSound;
    [SerializeField] private EventReference chaseYellSound;

    [Header("Combat Sounds")]
    [SerializeField] private EventReference focusGruntSound;
    [SerializeField] private EventReference gunCockSound;
    [SerializeField] private EventReference gunFireSound;
    [SerializeField] private EventReference damnMissedSound; // After miss
    [SerializeField] private EventReference gotchaHitSound;   // After hit

    // --- Emitter for Looped Sounds (Poem Monologue) ---
    // Option 1: Assign an FMOD Studio Event Emitter component in the Inspector
    [Header("Emitters (Assign in Inspector if used)")]
    [SerializeField] private StudioEventEmitter poemMonologueEmitter;
    // Option 2: Or manage instance manually (more code, but sometimes more control)
    // private FMOD.Studio.EventInstance _poemMonologueInstance;

    // NEW FMOD CHANGE
    // --- Instance variable to hold our controllable sound ---
    private FMOD.Studio.EventInstance _investigativeGruntInstance;
    // END FMOD CHANGE

    // --- Idle Grunt Logic ---
    [Header("Idle Grunt Settings")]
    [SerializeField] private float minIdleGruntInterval = 6f;
    [SerializeField] private float maxIdleGruntInterval = 12f;
    private Coroutine _idleGruntCoroutine;
    private bool _isIdleGrunting = false;

    void Start()
    {
        // Example: If you want the poem to start immediately (you'll likely control this from AI)
        // StartPoemMonologue();

        // Example: If you want idle grunts to start immediately (also likely AI controlled)
        // StartIdleGrunts();
    }

    // --- Public Methods to Trigger Sounds ---

    #region Ambient and State Sounds

    public void StartPoemMonologue()
    {
        if (poemMonologueEmitter != null && !poemMonologueSound.IsNull)
        {
            // Check if the currently assigned event is different from the desired one
            if (poemMonologueEmitter.EventReference.Guid != poemMonologueSound.Guid)
            {
                // CORRECTED LINE: Assign the new EventReference directly
                poemMonologueEmitter.EventReference = poemMonologueSound;
                // If the emitter was playing a different event, assigning a new EventReference
                // will typically stop the old event and start the new one if Play() is called
                // or if it was already playing.
            }

            // If the emitter is not currently playing, start it.
            if (!poemMonologueEmitter.IsPlaying())
            {
                poemMonologueEmitter.Play();
            }
        }
        else if (poemMonologueEmitter == null && !poemMonologueSound.IsNull)
        {
            Debug.LogWarning($"HunterSoundController: Poem Monologue Emitter not assigned on {gameObject.name} but sound is set. Cannot play.");
        }
        // Manual Instance Management (Alternative)
        // if (!_poemMonologueInstance.isValid() && !poemMonologueSound.IsNull)
        // {
        //     _poemMonologueInstance = RuntimeManager.CreateInstance(poemMonologueSound);
        //     RuntimeManager.AttachInstanceToGameObject(_poemMonologueInstance, transform);
        //     _poemMonologueInstance.start();
        // }
    }

    public void StopPoemMonologue()
    {
        if (poemMonologueEmitter != null && poemMonologueEmitter.IsPlaying())
        {
            poemMonologueEmitter.Stop();
        }
        // Manual Instance Management (Alternative)
        // if (_poemMonologueInstance.isValid())
        // {
        //     _poemMonologueInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        //     _poemMonologueInstance.release();
        // }
    }

    public void PlayAggressiveYell()
    {
        if (!aggressiveYellSound.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(aggressiveYellSound, gameObject);
            StopIdleGrunts(); // Likely stop idle grunts when becoming aggressive
        }
    }

    public void PlayFocusGrunt()
    {
        if (!focusGruntSound.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(focusGruntSound, gameObject);
        }
    }

    public void StartIdleGrunts()
    {
        if (_isIdleGrunting) return; // Already grunting

        if (!idleGruntSound.IsNull)
        {
            _isIdleGrunting = true;
            if (_idleGruntCoroutine != null)
            {
                StopCoroutine(_idleGruntCoroutine);
            }
            _idleGruntCoroutine = StartCoroutine(IdleGruntRoutine());
        }
    }

    public void StopIdleGrunts()
    {
        _isIdleGrunting = false;
        if (_idleGruntCoroutine != null)
        {
            StopCoroutine(_idleGruntCoroutine);
            _idleGruntCoroutine = null;
        }
    }

    private IEnumerator IdleGruntRoutine()
    {
        while (_isIdleGrunting)
        {
            float waitTime = Random.Range(minIdleGruntInterval, maxIdleGruntInterval);
            yield return new WaitForSeconds(waitTime);

            if (_isIdleGrunting) // Check again, in case it was stopped during wait
            {
                RuntimeManager.PlayOneShotAttached(idleGruntSound, gameObject);
            }
        }
    }

    public void PlayInvestigativeGrunt()
    {
        // Stop the previous instance if it's somehow still playing.
        StopInvestigativeGrunt();

        if (!investigativeGruntSound.IsNull)
        {
            // Create the instance but don't play it yet
            _investigativeGruntInstance = RuntimeManager.CreateInstance(investigativeGruntSound);
            // Attach it to the Hunter GameObject for 3D positioning
            RuntimeManager.AttachInstanceToGameObject(_investigativeGruntInstance, gameObject);
            // Start the sound
            _investigativeGruntInstance.start();
            // Release the instance. This is crucial! It tells FMOD to automatically
            // clean up the memory once the sound finishes playing or is stopped.
            _investigativeGruntInstance.release();
        }
    }

    // This is our new method to stop the sound prematurely.
    public void StopInvestigativeGrunt()
    {
        // Check if the instance is valid (i.e., it exists and hasn't been fully destroyed)
        if (_investigativeGruntInstance.isValid())
        {
            // Stop the sound immediately, without any fade out.
            _investigativeGruntInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        }
    }
    // END FMOD CHANGE

    public void PlayChaseYell()
    {
        if (!chaseYellSound.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(chaseYellSound, gameObject);
            StopIdleGrunts(); // Definitely stop idle grunts when chasing
        }
    }

    #endregion

    #region Combat Sounds


    public void PlayGunCockSound()
    {
        if (!gunCockSound.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(gunCockSound, gameObject);
        }
    }

    public void PlayGunFireSound()
    {
        if (!gunFireSound.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(gunFireSound, gameObject);
        }
    }

    public void PlayDamnMissedSound()
    {
        if (!damnMissedSound.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(damnMissedSound, gameObject);
        }
    }

    public void PlayGotchaHitSound()
    {
        if (!gotchaHitSound.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(gotchaHitSound, gameObject);
        }
    }

    #endregion

    void OnDestroy()
    {
        // Stop any ongoing sounds or coroutines to prevent errors
        StopPoemMonologue();
        StopIdleGrunts();

        // If using manual instance management for poem, ensure it's released:
        // if (_poemMonologueInstance.isValid())
        // {
        //     _poemMonologueInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); // Or ALLOWFADEOUT
        //     _poemMonologueInstance.release();
        // }
    }
}