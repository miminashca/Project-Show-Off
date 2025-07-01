using System;
using UnityEngine;

public class LadySpawner : MonoBehaviour
{
    [SerializeField] private GameObject whiteLadyPrefab;
    [SerializeField] private LadyAIConfig config;
    [Tooltip("How often (in seconds) to check for player proximity.")]
    [SerializeField] private float proximityCheckInterval = 2.0f;
    [SerializeField] private float dieToSpawnInterval = 30f;

    private Transform playerTransform;
    private bool isActivated = false;

    private float timer;

    private LadyStateMachine currentLady;
    private bool canBeSpawned = true;

    private void Awake()
    {
        GameManager.Instance.OnGameLoaded += Init;
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnGameLoaded -= Init;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= dieToSpawnInterval) canBeSpawned = true;
    }

    private void Init()
    {
        if (whiteLadyPrefab == null || config == null)
        {
            Debug.LogError("Spawner is missing Prefab or Config reference!", this);
            enabled = false;
            return;
        }

        // Cache player transform from the GameManager
        playerTransform = GameManager.Instance.PlayerTransform;
        if(playerTransform == null)
        {
            Debug.LogError("Spawner could not find Player Transform via GameManager.", this);
            enabled = false;
            return;
        }

        // Req 2.3: Use InvokeRepeating for optimized periodic checks
        InvokeRepeating(nameof(CheckProximity), 0f, proximityCheckInterval);
    }

    private void CheckProximity()
    {
        float distance = Vector3.Distance(playerTransform.position, transform.position);

        if (distance <= config.activationDistance && canBeSpawned)
        {
            if (!isActivated)
            {
                isActivated = true;
                // Debug.Log($"Spawner {name} Activated.");
            }
            TrySpawn();
        }
        else
        {
            if (isActivated)
            {
                isActivated = false;
                // Debug.Log($"Spawner {name} Deactivated.");
            }
        }
    }

    private void TrySpawn()
    {
        // Req 2.4 & 2.2: Check if spawn point is active and no White Lady exists
        if (!isActivated || GameManager.Instance.isWhiteLadyActive)
        {
            return;
        }
        
        // You could add a random chance here if desired, e.g., if (Random.value > 0.5f) return;

        Debug.Log($"Spawning White Lady at {transform.position}");
        
        // Instantiate and set the global flag
        GameObject ladyInstance = Instantiate(whiteLadyPrefab, transform.position, transform.rotation);
        GameManager.Instance.isWhiteLadyActive = true;

        canBeSpawned = false;
        
        // Pass essential references to the newly spawned AI
        currentLady = ladyInstance.GetComponent<LadyStateMachine>();
        if (currentLady != null)
        {
            currentLady.Initialize(config, playerTransform);
            currentLady.OnLadyDie += StartSpawnWaitTimer;
        }
        else
        {
            Debug.LogError("Spawned White Lady Prefab is missing the WhiteLady_AIController script!", ladyInstance);
            Destroy(ladyInstance); // Clean up
            GameManager.Instance.isWhiteLadyActive = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (config != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, config.activationDistance);
        }
    }

    private void StartSpawnWaitTimer()
    {
        currentLady.OnLadyDie -= StartSpawnWaitTimer;
        canBeSpawned = false;
        currentLady = null;
        timer = 0f;
    }
}