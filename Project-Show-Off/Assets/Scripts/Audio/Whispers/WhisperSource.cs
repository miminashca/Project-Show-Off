// WhisperSource.cs
//
// Attach this script to each individual audio emitter GameObject (the child of a clue).
// Its only purpose is to register itself with the WhisperManager when it's created
// and unregister itself when it's destroyed.

using UnityEngine;
using FMODUnity;

public class WhisperSource : MonoBehaviour
{
    [Header("FMOD Event")]
    [Tooltip("The specific whisper sound for this clue item.")]
    [SerializeField]
    private EventReference whisperEvent;

    // A public property so the Manager can get the event from this source.
    public EventReference WhisperEvent => whisperEvent;

    private void OnEnable()
    {
        // When this object is enabled, find the manager and register this source.
        if (WhisperManager.Instance != null)
        {
            WhisperManager.Instance.RegisterSource(this);
        }
        else
        {
            Debug.LogWarning("WhisperManager.Instance not found. A WhisperSource was enabled but could not register itself.", this);
        }
    }

    private void OnDisable()
    {
        // When this object is disabled or destroyed, unregister it from the manager.
        // We must check if the Instance still exists, as it might be destroyed first when closing the game.
        if (WhisperManager.Instance != null)
        {
            WhisperManager.Instance.UnregisterSource(this);
        }
    }
}