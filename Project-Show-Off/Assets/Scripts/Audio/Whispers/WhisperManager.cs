// WhisperManager.cs
//
// Place this script on a single, persistent manager object in your scene (e.g., AudioManager).
// This manager will control the entire whisper system.

using UnityEngine;
using FMODUnity;
using System.Collections.Generic;

public class WhisperManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    // This allows any script to access the manager easily via WhisperManager.Instance
    public static WhisperManager Instance { get; private set; }

    [Header("Playback Settings")]
    [SerializeField]
    [Tooltip("How often (in seconds) the whisper sound will be triggered from the closest source.")]
    private float soundPlayInterval = 120f;

    [Header("System Settings")]
    [SerializeField]
    [Tooltip("How often (in seconds) to check for the closest emitter. Lower is more responsive but less performant.")]
    private float distanceCheckInterval = 0.1f;
    [SerializeField]
    [Tooltip("The Tag assigned to your player character.")]
    private string playerTag = "Player";

    // --- Private Variables ---
    private List<WhisperSource> activeSources = new List<WhisperSource>();
    private WhisperSource currentClosestSource;
    private Transform playerTransform;

    private float distanceCheckTimer;
    private float soundPlayTimer;

    private void Awake()
    {
        // Implement the Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Find the player
        GameObject playerObject = GameObject.FindWithTag(playerTag);
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogError($"<b>[WhisperManager]</b> could not find a GameObject with the tag '{playerTag}'. The system will not function.", this);
            this.enabled = false; // Disable the manager if there's no player
            return;
        }

        // Initialize timers
        soundPlayTimer = soundPlayInterval; // Start with the full interval
    }

    private void Update()
    {
        // Don't run if there are no sources to check
        if (activeSources.Count == 0)
        {
            return;
        }

        // Timer for checking distances (frequent)
        distanceCheckTimer -= Time.deltaTime;
        if (distanceCheckTimer <= 0f)
        {
            distanceCheckTimer = distanceCheckInterval;
            UpdateClosestSource();
        }

        // Timer for playing the sound (infrequent)
        soundPlayTimer -= Time.deltaTime;
        if (soundPlayTimer <= 0f)
        {
            soundPlayTimer = soundPlayInterval;
            PlayWhisperFromClosestSource();
        }
    }

    private void PlayWhisperFromClosestSource()
    {
        // If we have a valid closest source with a valid FMOD event...
        if (currentClosestSource != null && !currentClosestSource.WhisperEvent.IsNull)
        {
            // ...play the one-shot sound attached to that source's GameObject.
            RuntimeManager.PlayOneShotAttached(currentClosestSource.WhisperEvent, currentClosestSource.gameObject);
            Debug.Log($"Playing whisper from {currentClosestSource.name}");
        }
    }

    private void UpdateClosestSource()
    {
        if (playerTransform == null || activeSources.Count == 0)
        {
            currentClosestSource = null;
            return;
        }

        WhisperSource closest = null;
        float minDistanceSq = float.MaxValue;

        foreach (var source in activeSources)
        {
            if (source == null) continue; // Safety check for destroyed objects

            float distanceSq = (source.transform.position - playerTransform.position).sqrMagnitude;
            if (distanceSq < minDistanceSq)
            {
                minDistanceSq = distanceSq;
                closest = source;
            }
        }

        currentClosestSource = closest;
    }

    // --- Public methods for sources to register/unregister ---
    public void RegisterSource(WhisperSource source)
    {
        if (!activeSources.Contains(source))
        {
            activeSources.Add(source);
        }
    }

    public void UnregisterSource(WhisperSource source)
    {
        if (activeSources.Contains(source))
        {
            activeSources.Remove(source);
        }
    }
}