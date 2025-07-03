using UnityEngine;

/// <summary>
/// Spawns the White Lady at a random time, in a random position around the player,
/// but only when the chosen spawn point is out of the player's line of sight.
/// This script should exist on a single manager object in the scene.
/// </summary>
public class DynamicLadySpawner : MonoBehaviour
{
    [Header("References & Prefabs")]
    [SerializeField] private GameObject whiteLadyPrefab;
    [SerializeField] private LadyAIConfig config;

    [Header("Spawn Timings")]
    [Tooltip("The minimum time (in seconds) to wait before attempting a new spawn.")]
    [SerializeField] private float minSpawnTime = 15.0f;
    [Tooltip("The maximum time (in seconds) to wait before attempting a new spawn.")]
    [SerializeField] private float maxSpawnTime = 30.0f;
    [Tooltip("The cooldown duration after the lady de-spawns before this spawner becomes active again.")]
    [SerializeField] private float dieToSpawnInterval = 30f;

    [Header("Spawn Conditions")]
    [Tooltip("The minimum distance from the player the lady can spawn.")]
    [SerializeField] private float minSpawnRadius = 10.0f;
    [Tooltip("The maximum distance from the player the lady can spawn.")]
    [SerializeField] private float maxSpawnRadius = 25.0f;
    [Tooltip("The layer mask for objects that can block the player's view of the spawn point.")]
    [SerializeField] private LayerMask occlusionLayers;
    [Tooltip("How many random positions to check per spawn attempt. Higher is more likely to succeed but less performant.")]
    [SerializeField] private int spawnAttempts = 10;

    // --- Private State ---
    private Transform playerTransform;
    private Camera playerCamera;
    private LadyStateMachine currentLady;

    private float spawnCooldownTimer; // Timer for the global cooldown after a lady dies
    private float nextSpawnAttemptTimer; // Timer for the random interval between spawn attempts
    private bool isCooldownActive = false;

    private void Start()
    {
        GameManager.Instance.OnGameLoaded += Init;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameLoaded -= Init;
        }
    }

    private void Init()
    {
        if (whiteLadyPrefab == null || config == null) { Debug.LogError("Dynamic Spawner is missing Prefab or Config reference!", this); enabled = false; return; }
        playerTransform = GameManager.Instance.PlayerTransform;
        if (playerTransform == null) { Debug.LogError("Dynamic Spawner could not find Player Transform via GameManager.", this); enabled = false; return; }
        
        playerCamera = playerTransform.GetComponentInChildren<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera == null) { Debug.LogError("Dynamic Spawner could not find a Camera!", this); enabled = false; return; }

        // Start the first spawn timer immediately
        ResetSpawnAttemptTimer();
    }

    private void Update()
    {
        // Handle the global cooldown after a lady has de-spawned.
        if (isCooldownActive)
        {
            spawnCooldownTimer += Time.deltaTime;
            if (spawnCooldownTimer >= dieToSpawnInterval)
            {
                isCooldownActive = false;
                ResetSpawnAttemptTimer(); // Start the spawn cycle again
            }
            return; // Do nothing else while on cooldown
        }

        // If a lady is already active, do nothing.
        if (GameManager.Instance.isWhiteLadyActive)
        {
            return;
        }

        // Countdown to the next spawn attempt.
        nextSpawnAttemptTimer -= Time.deltaTime;
        if (nextSpawnAttemptTimer <= 0)
        {
            TrySpawnAroundPlayer();
            ResetSpawnAttemptTimer(); // Always reset the timer after an attempt, successful or not
        }
    }

    private void TrySpawnAroundPlayer()
    {
        for (int i = 0; i < spawnAttempts; i++)
        {
            // 1. Find a random point in a "donut" shape around the player.
            Vector2 randomPoint2D = Random.insideUnitCircle.normalized * Random.Range(minSpawnRadius, maxSpawnRadius);
            Vector3 spawnPoint = playerTransform.position + new Vector3(randomPoint2D.x, 0, randomPoint2D.y);
            // Optional: You could add a raycast downwards here to place it perfectly on the ground.

            // 2. Check if this random point is visible to the player.
            if (!IsPositionInPlayerSight(spawnPoint))
            {
                // Found a valid point! Spawn her and exit the loop.
                SpawnLady(spawnPoint);
                return;
            }
        }
        // If the loop finishes, no valid spawn point was found in this attempt.
        // Debug.Log("Dynamic spawner failed to find a valid hidden spawn point this attempt.");
    }

    private void SpawnLady(Vector3 spawnPosition)
    {
        // Calculate rotation so the lady faces the player when she spawns
        Vector3 directionToPlayer = playerTransform.position - spawnPosition;
        directionToPlayer.y = 0; // Flatten rotation to the horizontal plane
        Quaternion spawnRotation = Quaternion.LookRotation(directionToPlayer);
        
        Debug.Log($"Dynamic Spawner is spawning White Lady at {spawnPosition}");

        GameObject ladyInstance = Instantiate(whiteLadyPrefab, spawnPosition, spawnRotation);
        GameManager.Instance.isWhiteLadyActive = true;
        
        currentLady = ladyInstance.GetComponent<LadyStateMachine>();
        if (currentLady != null)
        {
            currentLady.Initialize(config, playerTransform);
            currentLady.OnLadyDie += StartSpawnCooldown;
        }
        else
        {
            Debug.LogError("Spawned White Lady Prefab is missing the LadyStateMachine script!", ladyInstance);
            Destroy(ladyInstance);
            GameManager.Instance.isWhiteLadyActive = false;
        }
    }

    private bool IsPositionInPlayerSight(Vector3 position)
    {
        Vector3 directionToPoint = position - playerCamera.transform.position;
        float angle = Vector3.Angle(playerCamera.transform.forward, directionToPoint);

        if (angle > playerCamera.fieldOfView / 2f)
        {
            return false; // Not in the view cone.
        }

        if (Physics.Raycast(playerCamera.transform.position, directionToPoint.normalized, directionToPoint.magnitude, occlusionLayers))
        {
            return false; // View is blocked by an obstacle.
        }

        return true; // Has a clear line of sight.
    }

    private void StartSpawnCooldown()
    {
        currentLady.OnLadyDie -= StartSpawnCooldown;
        currentLady = null;
        
        isCooldownActive = true;
        spawnCooldownTimer = 0f;
    }

    private void ResetSpawnAttemptTimer()
    {
        nextSpawnAttemptTimer = Random.Range(minSpawnTime, maxSpawnTime);
    }
    
    private void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            // Draw the inner (minimum) radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerTransform.position, minSpawnRadius);
            // Draw the outer (maximum) radius
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerTransform.position, maxSpawnRadius);
        }
    }
}