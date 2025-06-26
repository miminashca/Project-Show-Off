using System;
using UnityEngine;
// NEW FMOD CHANGE
using FMODUnity;
using FMOD.Studio;
// END FMOD CHANGE

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
    [Tooltip("Looping injured breathing sound. This event should contain the snapshot to mute other breathing sounds.")]
    [SerializeField] private EventReference injuredBreathingLoopEvent;

    private EventInstance injuredBreathingInstance;
    // END FMOD CHANGE

    // --- Component References ---
    private PlayerMovement playerMovement;
    private PlayerStateController playerStateController;
    private PlayerInput controls;

    // --- Events for UI/GameManager ---
    public static event Action<int, int> OnWoundLevelChanged; // Sends Current, Max
    public static event Action<float, float> OnChokeTimerChanged;
    public static event Action OnPlayerDied;
    public static event Action OnPlayerTookDamage;


    public int CurrentWoundLevel => currentWoundLevel;
    public int MaxWoundLevel => maxWoundLevel;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerStateController = GetComponent<PlayerStateController>();
        controls = new PlayerInput();

        if (playerMovement == null || playerStateController == null)
        {
            Debug.LogError("PlayerHealth requires PlayerMovement and PlayerStateController on the same GameObject!", this);
            enabled = false;
        }

        // NEW FMOD CHANGE
        // Create and attach the FMOD instance for the looping injured breathing sound.
        if (!injuredBreathingLoopEvent.IsNull)
        {
            injuredBreathingInstance = RuntimeManager.CreateInstance(injuredBreathingLoopEvent);
        }
        // END FMOD CHANGE
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Start()
    {
        // Announce initial state
        OnWoundLevelChanged?.Invoke(currentWoundLevel, maxWoundLevel);

        // NEW FMOD CHANGE
        // Ensure the breathing sound is in the correct state at the start of the game.
        UpdateInjuredBreathingState();
        // END FMOD CHANGE
    }

    // NEW FMOD CHANGE
    private void OnDestroy()
    {
        // Always release FMOD instances when the object is destroyed to prevent memory leaks.
        if (injuredBreathingInstance.isValid())
        {
            injuredBreathingInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            injuredBreathingInstance.release();
        }
    }
    // END FMOD CHANGE

    private void Update()
    {
        // NEW FMOD CHANGE
        // Manually update the 3D attributes of the looping sound instance every frame.
        // This is the most robust way to ensure the sound follows the player's position.
        if (injuredBreathingInstance.isValid())
        {
            injuredBreathingInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
        }
        // END FMOD CHANGE


        HandleRegeneration();
        HandleAdrenalineRush();
        HandleChoking();
    }

    /// <summary>
    /// Called by enemies (like the Hunter) when they successfully hit the player.
    /// </summary>
    public void RegisterShot()
    {
        if (currentWoundLevel >= maxWoundLevel) return; // Already dying

        currentWoundLevel++;
        OnWoundLevelChanged?.Invoke(currentWoundLevel, maxWoundLevel);
        OnPlayerTookDamage?.Invoke();
        Debug.Log($"Player was shot! New wound level: {currentWoundLevel}/{maxWoundLevel}");

        // NEW FMOD CHANGE
        // 1. Play the instant "ARGHH!" sound.
        if (!playerHurtArghSound.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(playerHurtArghSound, gameObject);
        }

        // 2. Update the state of the looping injured breathing sound.
        UpdateInjuredBreathingState();
        // END FMOD CHANGE

        if (currentWoundLevel >= maxWoundLevel)
        {
            Die();
        }
        else
        {
            // Reset the regeneration timer each time the player is hit
            regenerationTimer = timeToRegenerateOneLevel;
            ActivateAdrenalineRush();
        }
    }

    private void HandleChoking()
    {
        // Only run the timer if at least one Hemanneken is attached.
        if (numberOfHemannekensAttached > 0)
        {
            // The timer drains faster for each Hemanneken attached.
            // e.g., 2 attached drains the timer twice as fast.
            chokeTimer -= Time.deltaTime * numberOfHemannekensAttached;

            // Notify any UI elements about the timer's progress.
            OnChokeTimerChanged?.Invoke(chokeTimer, timeToChoke);

            if (chokeTimer <= 0)
            {
                Debug.Log("Player has been choked to death by Hemanneken!");
                Die();
            }
        }
    }

    private void HandleRegeneration()
    {
        // Only regenerate if the player is wounded and not currently in an adrenaline rush
        if (currentWoundLevel > 0 && adrenalineTimer <= 0)
        {
            regenerationTimer -= Time.deltaTime;
            if (regenerationTimer <= 0)
            {
                currentWoundLevel--;
                OnWoundLevelChanged?.Invoke(currentWoundLevel, maxWoundLevel);
                Debug.Log($"Player regenerated one level. New wound level: {currentWoundLevel}");

                // NEW FMOD CHANGE
                // Update the looping sound state now that a wound level has changed.
                // This will stop the sound if the player is fully healed.
                UpdateInjuredBreathingState();
                // END FMOD CHANGE

                // If still wounded, reset the timer to heal the next level.
                if (currentWoundLevel > 0)
                {
                    regenerationTimer = timeToRegenerateOneLevel;
                }
            }
        }
    }

    private void HandleAdrenalineRush()
    {
        if (adrenalineTimer > 0)
        {
            adrenalineTimer -= Time.deltaTime;

            if (adrenalineTimer <= 0)
            {
                // Adrenaline rush has worn off
                Debug.Log("Adrenaline rush worn off.");
                playerStateController.SetAdrenalineBoost(adrenalineSpeedBoost, false);
                playerMovement.HasInfiniteStamina = false;

                // Start the regeneration timer now that the rush is over
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

        // Let the other components know the rush has started
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

    public void Die()
    {
        Debug.Log("Player has died! Wound level reached maximum.");
        OnPlayerDied?.Invoke();

        // NEW FMOD CHANGE
        // Stop the injured breathing sound immediately on death.
        UpdateInjuredBreathingState(); // Calling this will stop the sound as the component disables.
        // END FMOD CHANGE

        // --- Player Death Logic ---
        if (playerMovement) playerMovement.enabled = false;
        if (playerStateController) playerStateController.enabled = false;
        if (controls != null) controls.Disable();
        this.enabled = false;
    }

    public void SetWoundLevel(int level)
    {
        currentWoundLevel = Mathf.Clamp(level, 0, maxWoundLevel);
        OnWoundLevelChanged?.Invoke(currentWoundLevel, maxWoundLevel);

        regenerationTimer = timeToRegenerateOneLevel;
        adrenalineTimer = 0f;

        // NEW FMOD CHANGE
        // Update the breathing sound to match the newly set health state.
        UpdateInjuredBreathingState();
        // END FMOD CHANGE
    }

    // NEW FMOD CHANGE
    /// <summary>
    /// Checks the player's current health and starts or stops the injured breathing loop accordingly.
    /// </summary>
    private void UpdateInjuredBreathingState()
    {
        if (!injuredBreathingInstance.isValid()) return;

        // Check the current playback state of the instance.
        injuredBreathingInstance.getPlaybackState(out PLAYBACK_STATE currentState);

        // If the player is wounded (and the component is active)...
        if (currentWoundLevel > 0 && this.enabled)
        {
            // ...and the sound is not already playing, start it.
            if (currentState == PLAYBACK_STATE.STOPPED)
            {
                injuredBreathingInstance.start();
            }
        }
        // If the player is not wounded (or the component is being disabled)...
        else
        {
            // ...and the sound is currently playing, stop it with a fade out.
            if (currentState != PLAYBACK_STATE.STOPPED)
            {
                injuredBreathingInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            }
        }
    }
    // END FMOD CHANGE
}