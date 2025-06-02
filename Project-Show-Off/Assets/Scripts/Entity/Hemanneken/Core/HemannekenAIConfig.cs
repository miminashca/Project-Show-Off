using UnityEngine;

[CreateAssetMenu(fileName = "HemannekenAIConfig", menuName = "EnemyAI/HemannekenAIConfig")]
public class HemannekenAIConfig : ScriptableObject
{
    [Header("Detection Distances")]
    [Range(0f, 10f)] public float chaseDistanceRabbit = 3f;
    [Range(0f, 15f)] public float chaseDistanceTrue = 15f;
    [Range(0f, 20f)] public float endChaseDistance = 20f;
    [Range(0f, 10f)] public float stunDistance = 7f;
    [Range(0f, 100f)] public float investigateDistance = 50f;
    [Range(0f, 100f)] public float attachDistance = 1f;

    [Header("Timers & Durations")]
    [Range(0, 30)] public int investigationTimerDuration = 10;
    public float stunEffectDuration = 5f;
    public float lanternStunHoldDuration = 2f;
    public float transformationDuration = 1f;
    public float deathEffectDuration = 5f;
    public float waterDeathThreshold = 2f;

    [Header("Custom Roaming Movement")]
    public float defaultSpeed = 3.5f;
    public float rotationSpeed = 720f; // Degrees per second
    public float stoppingDistance = 0.2f; // Increased slightly for smoother stops

    [Header("Ground")]
    public LayerMask groundLayerMask = 1; // Layer(s) considered as ground (e.g., "Default", "Terrain")
    [Range(0f, 5f)] public float groundOffset = 0.1f;     // How far above the detected ground the agent should be
    [Range(0f, 20f)] public float groundRaycastMaxDistance = 20f; // Max distance the ray travels down to find ground
    [Range(0f, 10f)] public float groundRaycastStartHeightOffset = 1f; // How far above a reference Y to start the raycast
    public bool defaultRoamOnGround = false; // Default roaming behavior (grounded or aerial)

    [Header("Wave Path Parameters (for Roaming)")]
    [Tooltip("Max perpendicular distance the wave deviates from the direct path.")]
    [Range(0f, 5f)] public float waveAmplitude = 1.0f;
    [Tooltip("Number of full sine wave cycles over the path distance between main waypoints.")]
    [Range(0.1f, 5f)] public float waveFrequency = 1.0f;
    [Tooltip("Number of points used to define the wave curve between main waypoints. Higher is smoother.")]
    [Range(2, 100)] public int wavePathResolution = 8; // Replaces _intermediatePointsPerRoamSegment for clarity
    
    [Header("Runtime Hop Path Tweaks")]
    public float hopDistance = 2.0f;        // How far to travel in one hop
    public float hopSpeed = 10.0f;          // Speed during the hop itself (can be different from general speed)
    public float hopWaitDuration = 1.0f;    // How long to wait after a hop
}