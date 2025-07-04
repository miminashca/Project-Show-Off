using System;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class PlayerHealth : MonoBehaviour
{
    [Header("Wound Status")]
    [Tooltip("The number of shots the player can take before dying. The 3rd shot is fatal.")]
    [SerializeField] private int maxWoundLevel = 3;
    [SerializeField] private int currentWoundLevel = 0;

    [Header("Regeneration")]
    [Tooltip("Time in seconds to wait before one wound level is healed.")]
    [SerializeField] private float timeToRegenerateOneLevel = 30f;
    private float regenerationTimer = 0f;

    [Header("Adrenaline Rush Post-Hit")]
    [Tooltip("How long the adrenaline boost lasts after being shot.")]
    [SerializeField] private float adrenalineDuration = 10f;
    [Tooltip("How much speed is added during adrenaline rush (e.g., 0.5 for a 50% increase).")]
    [SerializeField] private float adrenalineSpeedBoost = 0.5f;
    private float adrenalineTimer = 0f;

    [Header("Choking by Hemanneken")]
    [Tooltip("The base time in seconds the player can survive while being choked by one Hemanneken.")]
    [SerializeField] private float timeToChoke = 10f;
    private int numberOfHemannekensAttached = 0;
    private float chokeTimer;

    // NEW FMOD CHANGE
    [Header("FMOD Injured Sounds")]
    [Tooltip("One-shot 'ARGH' sound played when the player is shot.")]
    [SerializeField] private EventReference playerHurtArghSound;
    [Tooltip("Looping injured breathing sound for when player is NOT sprinting. This event should contain the snapshot to mute other breathing sounds.")]
    [SerializeField] private EventReference injuredBreathingLoopEvent;
    // NEW FMOD CHANGE
    [Tooltip("Looping choking sound that plays while a Hemanneken is attached.")]
    [SerializeField] private EventReference chokingLoopEvent;
    // END FMOD CHANGE

    private EventInstance injuredBreathingInstance;
    // NEW FMOD CHANGE
    private EventInstance chokingLoopInstance;
    // END FMOD CHANGE

    //death screen implementation
    //[Header("Death Screen")]
    //[Tooltip("The UI panel that shows when the player dies.")]
    //[SerializeField] private GameObject deathScreenPanel;
    //[SerializeField] private CameraMovement cameraMovement;

    //end death screen implementation

    // --- Component References ---
    private PlayerMovement playerMovement;
    private PlayerStateController playerStateController;
    //private PlayerInput controls;

    // --- Events for UI/GameManager ---
    public static event Action<int, int> OnWoundLevelChanged; // Sends Current, Max
    public static event Action<float, float> OnChokeTimerChanged;
    public static event Action OnPlayerDied;
    public static event Action OnPlayerTookDamage;

    // NEW FMOD CHANGE
    public AmbientMusicTensionController musicTensionController;
    // END FMOD CHANGE

    public int CurrentWoundLevel => currentWoundLevel;
    public int MaxWoundLevel => maxWoundLevel;
    // <<< CHANGED: Added a flag to prevent Die() from being called multiple times. >>>
    private bool isDead = false;
    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerStateController = GetComponent<PlayerStateController>();
        //controls = new PlayerInput();
        //deathScreenPanel.SetActive(false);

        if (musicTensionController == null)
        {
            musicTensionController = FindAnyObjectByType<AmbientMusicTensionController>();
        }

        if (playerMovement == null || playerStateController == null)
        {
            Debug.LogError("PlayerHealth requires PlayerMovement and PlayerStateController on the same GameObject!", this);
            enabled = false;
        }

        // NEW FMOD CHANGE
        // Create the FMOD instance for the looping injured breathing sound.
        if (!injuredBreathingLoopEvent.IsNull)
        {
            injuredBreathingInstance = RuntimeManager.CreateInstance(injuredBreathingLoopEvent);
        }
        // END FMOD CHANGE
        // NEW FMOD CHANGE
        // Create the FMOD instance for the looping choking sound.
        if (!chokingLoopEvent.IsNull)
        {
            chokingLoopInstance = RuntimeManager.CreateInstance(chokingLoopEvent);
        }
        // END FMOD CHANGE
    }

    //private void OnEnable() => controls.Enable();
    //private void OnDisable() => controls.Disable();

    private void Start()
    {
        OnWoundLevelChanged?.Invoke(currentWoundLevel, maxWoundLevel);
        UpdateInjuredBreathingState();
    }

    private void OnDestroy()
    {
        // NEW FMOD CHANGE
        if (injuredBreathingInstance.isValid())
        {
            injuredBreathingInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            injuredBreathingInstance.release();
        }
        // END FMOD CHANGE
        // NEW FMOD CHANGE
        if (chokingLoopInstance.isValid())
        {
            chokingLoopInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            chokingLoopInstance.release();
        }
        // END FMOD CHANGE
    }

    private void Update()
    {
        // NEW FMOD CHANGE
        // Manually update the 3D attributes of the looping sound instance every frame
        // to ensure it follows the player's position.
        if (injuredBreathingInstance.isValid())
        {
            injuredBreathingInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
        }

        // Check the breathing state every frame to react to starting/stopping sprinting.
        UpdateInjuredBreathingState();
        // END FMOD CHANGE
        // NEW FMOD CHANGE
        // Update the 3D attributes for the choking sound as well.
        if (chokingLoopInstance.isValid())
        {
            chokingLoopInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
        }
        // END FMOD CHANGE

        //coment this if you wanna revert to the old death screen implementation
        if (isDead) return;

        HandleRegeneration();
        HandleAdrenalineRush();
        HandleChoking();
    }

    public void RegisterShot()
    {
        //if (currentWoundLevel >= maxWoundLevel) return;
        // <<< CHANGED: Check the isDead flag >>>
        if (currentWoundLevel >= maxWoundLevel || isDead) return;

        currentWoundLevel++;
        OnWoundLevelChanged?.Invoke(currentWoundLevel, maxWoundLevel);
        OnPlayerTookDamage?.Invoke();
        Debug.Log($"Player was shot! New wound level: {currentWoundLevel}/{maxWoundLevel}");

        // NEW FMOD CHANGE
        if (!playerHurtArghSound.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(playerHurtArghSound, gameObject);
        }

        if (musicTensionController != null)
        {
            musicTensionController.TriggerShotTension();
        }

        // UpdateInjuredBreathingState is called in Update, so it will handle the change.
        // END FMOD CHANGE

        if (currentWoundLevel >= maxWoundLevel)
        {
            Die();
        }
        else
        {
            regenerationTimer = timeToRegenerateOneLevel;
            ActivateAdrenalineRush();
        }
    }

    private void HandleRegeneration()
    {
        if (currentWoundLevel > 0 && adrenalineTimer <= 0)
        {
            regenerationTimer -= Time.deltaTime;
            if (regenerationTimer <= 0)
            {
                currentWoundLevel--;
                OnWoundLevelChanged?.Invoke(currentWoundLevel, maxWoundLevel);
                Debug.Log($"Player regenerated one level. New wound level: {currentWoundLevel}");

                // UpdateInjuredBreathingState is called in Update, so it will handle the change.

                if (currentWoundLevel > 0)
                {
                    regenerationTimer = timeToRegenerateOneLevel;
                }
            }
        }
    }
    //method 1 (Old)
    //public void Die()
    //{
    //    Debug.Log("Player has died! Wound level reached maximum.");
    //    OnPlayerDied?.Invoke();


    //    Time.timeScale = 0f; // Pause the game
    //    Cursor.lockState = CursorLockMode.None; // Unlock the cursor
    //    Cursor.visible = true; // Make the cursor visible

    //    // This call will correctly stop the sound as the component is about to be disabled.
    //    UpdateInjuredBreathingState();

    //    if (playerMovement) playerMovement.enabled = false;
    //    if (cameraMovement) cameraMovement.enabled = false;
    //    if (playerStateController) playerStateController.enabled = false;
    //    if (controls != null) controls.Disable();
    //    this.enabled = false;

    //    if (deathScreenPanel) deathScreenPanel.SetActive(true);

    //    // Finally, disable this script.
    //    this.enabled = false;
    //}

    //method 2 (New)
    // <<< CHANGED: This is the most important change. The Die() method is now extremely simple. >>>
    public void Die()
    {
        // Prevent this method from running more than once
        if (isDead) return;
        isDead = true;

        Debug.Log("Player has died! Notifying GameManager.");
        OnPlayerDied?.Invoke();

        // This call will correctly stop the sound as the component is about to be disabled by the GameManager.
        UpdateInjuredBreathingState();

        // Tell the GameManager to handle everything else (pausing time, showing UI, disabling controls).
        GameManager.Instance.PlayerDied();

    }
    public void SetWoundLevel(int level)
    {
        currentWoundLevel = Mathf.Clamp(level, 0, maxWoundLevel);
        OnWoundLevelChanged?.Invoke(currentWoundLevel, maxWoundLevel);

        regenerationTimer = timeToRegenerateOneLevel;
        adrenalineTimer = 0f;

        // UpdateInjuredBreathingState is called in Update, so it will handle the change.
    }

    // NEW FMOD CHANGE
    /// <summary>
    /// Checks the player's state and starts or stops the base injured breathing loop.
    /// This sound should only play when the player is wounded AND not sprinting.
    /// </summary>
    private void UpdateInjuredBreathingState()
    {
        if (!injuredBreathingInstance.isValid()) return;

        injuredBreathingInstance.getPlaybackState(out PLAYBACK_STATE currentState);

        // Define the conditions under which this sound should be playing.
        bool shouldBePlaying = currentWoundLevel > 0 &&
                               (playerMovement != null && !playerMovement.isSprinting) &&
                               this.enabled;

        if (shouldBePlaying)
        {
            // If the sound should be playing, but it's stopped, start it.
            if (currentState == PLAYBACK_STATE.STOPPED)
            {
                injuredBreathingInstance.start();
            }
        }
        else
        {
            // If the sound should NOT be playing, but it is, stop it with a fade.
            if (currentState != PLAYBACK_STATE.STOPPED)
            {
                injuredBreathingInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            }
        }
    }
    // END FMOD CHANGE

    // --- Other methods like HandleChoking, HandleAdrenalineRush, etc. remain unchanged ---

    private void HandleChoking()
    {
        // <<< CHANGED: Check the isDead flag >>>
        if (isDead) return;

        // NEW FMOD CHANGE
        if (!chokingLoopInstance.isValid()) return; // Early exit if the sound isn't set up
        chokingLoopInstance.getPlaybackState(out PLAYBACK_STATE state);
        // END FMOD CHANGE

        if (numberOfHemannekensAttached > 0)
        {
            chokeTimer -= Time.deltaTime * numberOfHemannekensAttached;
            OnChokeTimerChanged?.Invoke(chokeTimer, timeToChoke);
            if (chokeTimer <= 0)
            {
                Debug.Log("Player has been choked to death by Hemanneken!");
                Die();
            }
            // NEW FMOD CHANGE
            // If we are being choked and the sound is not yet playing, start it.
            if (state == PLAYBACK_STATE.STOPPED)
            {
                chokingLoopInstance.start();
            }
            // END FMOD CHANGE
        }
        // NEW FMOD CHANGE
        else
        {
            // If we are NOT being choked, but the sound is still playing, stop it with a fade out.
            if (state != PLAYBACK_STATE.STOPPED)
            {
                chokingLoopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            }
        }
        // END FMOD CHANGE
    }

    private void HandleAdrenalineRush()
    {
        if (adrenalineTimer > 0)
        {
            adrenalineTimer -= Time.deltaTime;
            if (adrenalineTimer <= 0)
            {
                Debug.Log("Adrenaline rush worn off.");
                playerStateController.SetAdrenalineBoost(adrenalineSpeedBoost, false);
                playerMovement.HasInfiniteStamina = false;
                if (currentWoundLevel > 0)
                {
                    regenerationTimer = timeToRegenerateOneLevel;
                }
            }
        }
    }

    private void ActivateAdrenalineRush()
    {
        Debug.Log("ADRENALINE RUSH ACTIVATED!");
        adrenalineTimer = adrenalineDuration;
        playerStateController.SetAdrenalineBoost(adrenalineSpeedBoost, true);
        playerMovement.HasInfiniteStamina = true;
    }

    public void IncrementAttachedHemannekens()
    {
        if (numberOfHemannekensAttached == 0)
        {
            chokeTimer = timeToChoke;
        }
        numberOfHemannekensAttached++;
        Debug.Log($"Hemanneken attached. Total: {numberOfHemannekensAttached}. Choke timer started/sped up.");
    }

    public void DecrementAttachedHemannekens()
    {
        numberOfHemannekensAttached--;
        if (numberOfHemannekensAttached < 0) numberOfHemannekensAttached = 0;
        if (numberOfHemannekensAttached == 0)
        {
            Debug.Log("Last Hemanneken detached. Choke timer stopped.");
            OnChokeTimerChanged?.Invoke(timeToChoke, timeToChoke);
        }
        else
        {
            Debug.Log($"Hemanneken detached. Remaining: {numberOfHemannekensAttached}.");
        }
    }
}