using System.Collections;
using UnityEngine;
using FMODUnity; // Ensure this is present for FMOD functionality

public class RandomSoundEmitter : MonoBehaviour
{
    [Header("FMOD Event Settings")]
    public EventReference fmodEvent; // Drag your FMOD event here
    public float minDelay = 5f;
    public float maxDelay = 10f;

    [Header("Player Proximity Settings")]
    public float quietZoneDistance = 40f; // Sounds won't play if the player is closer than this distance

    // Reference to the player's Transform.
    // We'll try to find it automatically, but you can also assign it manually in the Inspector.
    private Transform playerTransform;
    private Coroutine playRoutine;

    void Awake()
    {
        // It's good practice to find the player in Awake to ensure it's ready before Start.
        // Option 1: Find by Tag (most common for player characters)
        // Ensure your player GameObject has the tag "Player" in Unity.
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            // Debug.Log($"RandomSoundEmitter ({gameObject.name}): Found player transform: {playerTransform.name}");
        }
        else
        {
            // Option 2: If the player is the main camera, you could use this:
            // if (Camera.main != null)
            // {
            //     playerTransform = Camera.main.transform;
            //     Debug.LogWarning($"RandomSoundEmitter ({gameObject.name}): Player with tag 'Player' not found. Using Main Camera as player reference.");
            // }
            // else
            // {
            Debug.LogError($"RandomSoundEmitter ({gameObject.name}): Player GameObject with tag 'Player' not found! " +
                           "Please ensure your player has the 'Player' tag or assign 'playerTransform' manually in the inspector. " +
                           "Sounds will play without distance checks until a player is found.");
            // }
        }
    }

    void Start()
    {
        playRoutine = StartCoroutine(PlaySoundAtRandomIntervals());
    }

    IEnumerator PlaySoundAtRandomIntervals()
    {
        while (true)
        {
            float delay = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(delay);

            // --- New Logic: Check player distance ---
            if (playerTransform != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

                if (distanceToPlayer < quietZoneDistance)
                {
                    // Player is too close, skip playing the sound this time
                    // Debug.Log($"RandomSoundEmitter ({gameObject.name}): Player is too close ({distanceToPlayer:F2}m). Skipping sound playback.");
                    continue; // This skips the rest of the current loop iteration and starts the next one (waiting for new delay)
                }
            }
            else
            {
                // If playerTransform is null (e.g., player wasn't found in Awake),
                // we'll proceed to play the sound without a distance check.
                // A warning would have been logged in Awake for this.
            }

            // If we reach this point, either the player is far enough away,
            // or the player reference wasn't found (and we decided to play anyway).
            RuntimeManager.PlayOneShot(fmodEvent, transform.position);
            // Debug.Log($"RandomSoundEmitter ({gameObject.name}): Played sound. Distance to player: {(playerTransform != null ? Vector3.Distance(transform.position, playerTransform.position).ToString("F2") + "m" : "N/A")}");
        }
    }

    // Optional: Stop playback if needed (e.g., if the emitter object is disabled or destroyed)
    public void StopEmitting()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null; // Set to null to indicate it's stopped
            // Debug.Log($"RandomSoundEmitter ({gameObject.name}): Stopped emitting sounds.");
        }
    }
}