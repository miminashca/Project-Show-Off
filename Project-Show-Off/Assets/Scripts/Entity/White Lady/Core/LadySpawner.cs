using System;
using UnityEngine;

public class LadySpawner : MonoBehaviour
{
    [SerializeField] private GameObject whiteLadyPrefab;
    [SerializeField] private LadyAIConfig config;
    [Tooltip("How often (in seconds) to check for player proximity.")]
    [SerializeField] private float proximityCheckInterval = 2.0f;

    private Transform playerTransform;
    private bool isActivated = false;

    private void Awake()
    {
        GameManager.Instance.OnGameLoaded += Init;
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnGameLoaded -= Init;
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

        if (distance <= config.activationDistance)
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

        // Pass essential references to the newly spawned AI
        var aiController = ladyInstance.GetComponent<LadyStateMachine>();
        if (aiController != null)
        {
            aiController.Initialize(config, playerTransform);
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
}