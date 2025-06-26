// WhisperSource.cs - (More Robust Version)
using UnityEngine;
using FMODUnity;

public class WhisperSource : MonoBehaviour
{
    [Header("FMOD Event")]
    [Tooltip("The specific whisper sound for this clue item.")]
    [SerializeField]
    private EventReference whisperEvent;

    public EventReference WhisperEvent => whisperEvent;

    // --- NEW ---
    private bool isRegistered = false;

    private void OnEnable()
    {
        // We still try to register here for objects that are enabled/disabled at runtime.
        if (WhisperManager.Instance != null)
        {
            WhisperManager.Instance.RegisterSource(this);
            isRegistered = true;
        }
    }

    // --- NEW ---
    private void Start()
    {
        // If we failed to register in OnEnable (due to execution order),
        // try again now. Start() is guaranteed to run after all Awake() methods.
        if (!isRegistered && WhisperManager.Instance != null)
        {
            WhisperManager.Instance.RegisterSource(this);
            isRegistered = true;
        }
        // If the manager still doesn't exist, something is wrong.
        else if (WhisperManager.Instance == null)
        {
            Debug.LogWarning("WhisperSource could not find the WhisperManager in OnEnable or Start. Make sure a WhisperManager exists in the scene.", this);
        }
    }

    private void OnDisable()
    {
        if (WhisperManager.Instance != null)
        {
            WhisperManager.Instance.UnregisterSource(this);
            isRegistered = false; // Reset the flag
        }
    }
}