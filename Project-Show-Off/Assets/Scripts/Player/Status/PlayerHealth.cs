using System;
using UnityEngine;

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

    // --- Component References ---
    private PlayerMovement playerMovement;
    private PlayerStateController playerStateController;
    private PlayerInput controls;

    // --- Events for UI/GameManager ---
    public static event Action<int, int> OnWoundLevelChanged; // Sends Current, Max
    public static event Action<float, float> OnChokeTimerChanged;
    public static event Action OnPlayerDied;

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
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Start()
    {
        // Announce initial state
        OnWoundLevelChanged?.Invoke(currentWoundLevel, maxWoundLevel);
    }

    private void Update()
    {
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
        Debug.Log($"Player was shot! New wound level: {currentWoundLevel}/{maxWoundLevel}");

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

    /// <summary>
    /// Called by a Hemanneken when it enters its AttachedState.
    /// </summary>
    public void IncrementAttachedHemannekens()
    {
        if (numberOfHemannekensAttached == 0)
        {
            // This is the first one, start the timer from its max value.
            chokeTimer = timeToChoke;
        }
        numberOfHemannekensAttached++;
        Debug.Log($"Hemanneken attached. Total: {numberOfHemannekensAttached}. Choke timer started/sped up.");
    }

    /// <summary>
    /// Called by a Hemanneken when it exits its AttachedState (by dying or detaching).
    /// </summary>
    public void DecrementAttachedHemannekens()
    {
        numberOfHemannekensAttached--;
        if (numberOfHemannekensAttached < 0) numberOfHemannekensAttached = 0;

        if (numberOfHemannekensAttached == 0)
        {
            Debug.Log("Last Hemanneken detached. Choke timer stopped.");
            // Stop the timer and notify UI it's gone.
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

        // --- Player Death Logic ---
        if (playerMovement) playerMovement.enabled = false;
        if (playerStateController) playerStateController.enabled = false;
        if (controls != null) controls.Disable();
        // You might also want to disable the camera controller script here.
        this.enabled = false;
    }
}