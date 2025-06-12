using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class WindController : MonoBehaviour
{
    public static WindController Instance { get; private set; }

    [Header("FMOD Settings")]
    [Tooltip("The 2D ambiance event that contains the 'Windy' parameter.")]
    public EventReference ambianceEvent;
    [Tooltip("The exact name of the FMOD parameter controlling wind volume.")]
    public string windyParameterName = "Windy"; // Default name, change if yours is different

    [Header("Transition Settings")]
    [Tooltip("How many seconds it takes for the wind to fully fade in or out.")]
    public float transitionDuration = 3.0f;

    private EventInstance ambianceInstance;
    private float currentWindyValue = 0f;
    private float targetWindyValue = 0f;
    private int activeZoneCount = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Optional: if this controller should persist across scenes
        }
        else
        {
            Debug.LogWarning("Multiple WindController instances detected. Destroying this one.", this);
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (ambianceEvent.IsNull)
        {
            Debug.LogError("WindController: Ambiance Event is not assigned!", this);
            enabled = false; // Disable this script if no event is set
            return;
        }

        ambianceInstance = RuntimeManager.CreateInstance(ambianceEvent);
        ambianceInstance.start();
        // Initialize the parameter to its starting state (0 for no wind)
        ambianceInstance.setParameterByName(windyParameterName, currentWindyValue);
        // Debug.Log("WindController initialized and ambiance event started.");
    }

    void Update()
    {
        if (ambianceInstance.isValid())
        {
            if (!Mathf.Approximately(currentWindyValue, targetWindyValue))
            {
                // Calculate how much to change this frame
                float step = (1.0f / Mathf.Max(0.01f, transitionDuration)) * Time.deltaTime;
                currentWindyValue = Mathf.MoveTowards(currentWindyValue, targetWindyValue, step);

                ambianceInstance.setParameterByName(windyParameterName, currentWindyValue);
                // Debug.Log($"Windy parameter updated to: {currentWindyValue}");
            }
        }
    }

    public void PlayerEnteredWindZone()
    {
        activeZoneCount++;
        if (activeZoneCount == 1) // If this is the first zone entered
        {
            targetWindyValue = 1.0f; // Start fading in
            // Debug.Log("Target Windy set to 1.0");
        }
    }

    public void PlayerExitedWindZone()
    {
        activeZoneCount--;
        if (activeZoneCount <= 0) // If exited the last (or only) zone
        {
            activeZoneCount = 0; // Ensure it doesn't go negative
            targetWindyValue = 0.0f; // Start fading out
            // Debug.Log("Target Windy set to 0.0");
        }
    }

    void OnDestroy()
    {
        if (ambianceInstance.isValid())
        {
            ambianceInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); // Or ALLOWFADEOUT if you have a global fade out on the event itself
            ambianceInstance.release();
            // Debug.Log("Ambiance event stopped and released.");
        }
        if (Instance == this)
        {
            Instance = null;
        }
    }
}