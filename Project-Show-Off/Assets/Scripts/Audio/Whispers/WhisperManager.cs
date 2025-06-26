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
    public static WhisperManager Instance { get; private set; }

    // --- CHANGED ---: Replaced the single interval with a min/max range.
    [Header("Playback Settings")]
    [SerializeField]
    [Tooltip("The minimum time (in seconds) to wait before playing a whisper.")]
    private float minSoundPlayInterval = 90f;

    [SerializeField]
    [Tooltip("The maximum time (in seconds) to wait before playing a whisper.")]
    private float maxSoundPlayInterval = 150f;

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

        // --- CHANGED ---: Initialize the first timer with a random value from the start.
        ResetSoundTimer();
    }

    private void Update()
    {
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
            // --- CHANGED ---: Play the sound and then reset the timer to a new random value.
            PlayWhisperFromClosestSource();
            ResetSoundTimer();
        }
    }

    // --- NEW ---: A helper method to get a new random interval.
    /// <summary>
    /// Sets the sound timer to a new random value between the min and max intervals.
    /// </summary>
    private void ResetSoundTimer()
    {
        soundPlayTimer = Random.Range(minSoundPlayInterval, maxSoundPlayInterval);
    }

    private void PlayWhisperFromClosestSource()
    {
        if (currentClosestSource != null && !currentClosestSource.WhisperEvent.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(currentClosestSource.WhisperEvent, currentClosestSource.gameObject);
            Debug.Log($"Playing whisper from {currentClosestSource.name}. Next whisper in approx {soundPlayTimer:F1} seconds.");
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
            if (source == null) continue;

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

    // --- NEW ---: Editor-only validation to prevent invalid values.
    /// <summary>
    /// This function is called in the editor when the script is loaded or a value is changed in the Inspector.
    /// </summary>
    private void OnValidate()
    {
        // Ensure min value is never negative.
        if (minSoundPlayInterval < 0)
        {
            minSoundPlayInterval = 0;
        }
        // Ensure max value is never less than the min value.
        if (maxSoundPlayInterval < minSoundPlayInterval)
        {
            maxSoundPlayInterval = minSoundPlayInterval;
        }
    }
}