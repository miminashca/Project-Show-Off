using UnityEngine;

[CreateAssetMenu(fileName = "LadyAIConfig", menuName = "AI/White Lady Config", order = 1)]
public class LadyAIConfig : ScriptableObject
{
    [Header("Spawner Settings")]
    public float activationDistance = 50.0f;

    [Header("State Transition Timers")]
    public float timeToTriggerSeen = 1.0f;
    public float timeToDissipate = 1.0f;
    public float despawnDelay = 1.5f;

    [Header("Gaze Penalties")]
    public float timeToDamage = 5.0f;
    public float timeToDeath = 10.0f;

    [Header("Player Feedback Settings")]
    public float minFovValue = 40.0f;
    public float fovTransitionDuration = 10.0f; // Time in seconds to go from max to min
    public float cameraPullStrength = 0.1f;
    public int gazeDamageAmount = 1;

    [Header("Audio (FMOD Event Paths)")]
    public string creepingAudioEvent = "event:/SFX/WhiteLady/Creeping";
    public string playerBreathAudioEvent = "event:/SFX/Player/FearBreath";
    public string gazeDamageAudioEvent = "event:/SFX/Player/GazeDamage";
}