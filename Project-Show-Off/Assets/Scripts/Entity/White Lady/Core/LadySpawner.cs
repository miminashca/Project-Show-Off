using System;
using UnityEngine;

public class LadySpawner : MonoBehaviour
{
    [Header("References & Prefabs")]
    [SerializeField] private GameObject whiteLadyPrefab;
    [SerializeField] private LadyAIConfig config;

    [Header("Spawn Timings")]
    [SerializeField] private float proximityCheckInterval = 2.0f;
    [SerializeField] private float dieToSpawnInterval = 30f;

    [Header("Spawn Conditions")]
    [SerializeField] private LayerMask occlusionLayers;

    private Transform playerTransform;
    private Camera playerCamera;
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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameLoaded -= Init;
        }
    }

    private void Update()
    {
        if (!canBeSpawned)
        {
            timer += Time.deltaTime;
            if (timer >= dieToSpawnInterval)
            {
                canBeSpawned = true;
            }
        }
    }

    private void Init()
    {
        // ... (this part of the code is correct and does not need to change) ...
        if (whiteLadyPrefab == null || config == null) { /* error */ return; }
        playerTransform = GameManager.Instance.PlayerTransform;
        if (playerTransform == null) { /* error */ return; }
        playerCamera = playerTransform.GetComponentInChildren<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera == null) { /* error */ return; }
        InvokeRepeating(nameof(CheckProximity), 0f, proximityCheckInterval);
    }

    private void CheckProximity()
    {
        // ... (this part of the code is correct and does not need to change) ...
        if (playerTransform == null) return;
        float distance = Vector3.Distance(playerTransform.position, transform.position);
        if (distance <= config.activationDistance && canBeSpawned)
        {
            if (!isActivated) isActivated = true;
            TrySpawn();
        }
        else
        {
            if (isActivated) isActivated = false;
        }
    }

    private void TrySpawn()
    {
        if (!isActivated || GameManager.Instance.isWhiteLadyActive) return;

        if (IsPlayerLookingAtSpawnPoint())
        {
            return;
        }
        
        // ... (the rest of the spawn logic is correct and does not need to change) ...
        Debug.Log($"Spawning White Lady at {transform.position}");
        GameObject ladyInstance = Instantiate(whiteLadyPrefab, transform.position, transform.rotation);
        GameManager.Instance.isWhiteLadyActive = true;
        canBeSpawned = false;
        timer = 0f;
        currentLady = ladyInstance.GetComponent<LadyStateMachine>();
        if (currentLady != null)
        {
            currentLady.Initialize(config, playerTransform);
            currentLady.OnLadyDie += StartSpawnWaitTimer;
        }
        else
        {
            Debug.LogError("Spawned White Lady Prefab is missing the LadyStateMachine script!", ladyInstance);
            Destroy(ladyInstance);
            GameManager.Instance.isWhiteLadyActive = false;
        }
    }
    
    /// <summary>
    /// --- NEW AND IMPROVED VERSION ---
    /// Checks if the spawn point is within the player's camera view and not blocked by geometry.
    /// This version is robust and works correctly for a single point (empty Transform).
    /// </summary>
    private bool IsPlayerLookingAtSpawnPoint()
    {
        // 1. Get the direction vector from the camera to the spawn point.
        Vector3 directionToSpawner = transform.position - playerCamera.transform.position;

        // 2. Angle Check: Calculate the angle between the camera's forward direction and the direction to the spawner.
        float angle = Vector3.Angle(playerCamera.transform.forward, directionToSpawner);

        // If the angle is greater than half of the camera's Field of View, it's outside the view cone.
        if (angle > playerCamera.fieldOfView / 2f)
        {
            return false; // Not in view, so player is NOT looking. Spawn is ALLOWED.
        }

        // 3. Occlusion Check: If it's within the view cone, check if anything is blocking the view.
        if (Physics.Raycast(playerCamera.transform.position, directionToSpawner.normalized, directionToSpawner.magnitude, occlusionLayers))
        {
            // A raycast hit something in the occlusion layer. The view is blocked.
            return false; // View is blocked, so player is NOT looking. Spawn is ALLOWED.
        }
        
        // If we pass both checks (it's in the FOV cone AND not occluded), the player IS looking at the point.
        // Therefore, we block the spawn.
        Debug.Log($"Spawn at {name} blocked - player has clear line of sight.");
        return true; 
    }

    private void OnDrawGizmos()
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
        // The canBeSpawned flag is already false. The timer is already reset in TrySpawn.
        // We just need to null out the reference.
        currentLady = null;
    }
}