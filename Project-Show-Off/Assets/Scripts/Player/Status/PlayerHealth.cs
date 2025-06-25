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

    // --- Component References ---
    private PlayerMovement playerMovement;
    private PlayerStateController playerStateController;
    private PlayerInput controls;

    // --- Events for UI/GameManager ---
    public static event Action<int, int> OnWoundLevelChanged; // Sends Current, Max
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

    private void ActivateAdrenalineRush()
    {
        Debug.Log("ADRENALINE RUSH ACTIVATED!");
        adrenalineTimer = adrenalineDuration;

        // Let the other components know the rush has started
        playerStateController.SetAdrenalineBoost(adrenalineSpeedBoost, true);
        playerMovement.HasInfiniteStamina = true;
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

    private void Die()
    {
        Debug.Log("Player has died! Wound level reached maximum.");
        OnPlayerDied?.Invoke();

        // --- Player Death Logic ---
        // This is where you would trigger a level restart or game over screen.
        // For now, we can disable player control as a placeholder.
        if (playerMovement) playerMovement.enabled = false;
        if (playerStateController) playerStateController.enabled = false;
        if (controls != null) controls.Disable();
        // You might also want to disable the camera controller script here.
        this.enabled = false;
    }
    
    public void SetWoundLevel(int level)
    {
        currentWoundLevel = Mathf.Clamp(level, 0, maxWoundLevel);

        // This is crucial to update UI and other game logic that listens for this event.
        OnWoundLevelChanged?.Invoke(currentWoundLevel, maxWoundLevel);
    
        // Reset timers to a neutral state
        regenerationTimer = timeToRegenerateOneLevel;
        adrenalineTimer = 0f;
    }
}