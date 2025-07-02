using UnityEngine;

[CreateAssetMenu(fileName = "LadyAIConfig", menuName = "AI/White Lady Config", order = 1)]
public class LadyAIConfig : ScriptableObject
{
    [Header("Spawner Settings")]
    public float activationDistance = 50.0f;

    [Header("State Transition Timers")]
    public float timeToTriggerSeen = 1.0f;
    public float timeToDissipate = 10f;
    public float timeToReturnCreeping = 1.0f;
    public float despawnDelay = 1.5f;

    [Header("Gaze Penalties")]
    public float timeToDamage = 5.0f;
    public float timeToDeath = 10.0f;

    [Header("Gaze Feedback Settings")]
    [Tooltip("The duration (in seconds) over which the zoom-in and camera pull effects intensify.")]
    public float fovTransitionDuration = 10.0f;
    [Tooltip("The final apparent size of the White Lady as a percentage of her initial size. 0.6 means she will appear 60% of her original size.")]
    [Range(0.1f, 1.0f)]
    public float dollyZoomTargetScale = 0.6f;
    [Tooltip("How quickly the FOV snaps to the dynamically calculated target. Higher is faster.")]
    public float dynamicFovSmoothing = 5.0f;
    
    [Header("Gaze Effects")]
    [Tooltip("How much control the gaze has over the camera at the start of the effect. A lerp factor from 0 to 1.")]
    [Range(0.0f, 1.0f)]
    public float minCameraPullLerp = 0.0f; // Start with no pull
    [Tooltip("How much control the gaze has at maximum intensity. 0.1 is a strong pull, 1.0 is full control.")]
    [Range(0.0f, 1.0f)]
    public float maxCameraPullLerp = 0.05f; // End with a 5% pull per frame.
    public int gazeDamageAmount = 1;
    [Tooltip("How quickly the White Lady turns to face the player when seen. Higher is faster.")]
    public float turnSpeed = 5.0f;

    [Header("Audio (FMOD Event Paths)")]
    public string creepingAudioEvent = "event:/SFX/WhiteLady/Creeping";
    public string playerBreathAudioEvent = "event:/SFX/Player/FearBreath";
    public string gazeDamageAudioEvent = "event:/SFX/Player/GazeDamage";
}