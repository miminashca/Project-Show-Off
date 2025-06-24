using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(NixieStateMachine), typeof(NixieNavigation), typeof(AudioSource))]
public class NixieAI : MonoBehaviour
{
    [Header("Sensory Parameters")]
    [Tooltip("The radius at which the Nixie will stop and stare at the player.")]
    public float StaringRadius = 20f;
    [Tooltip("The radius at which the Nixie will detect and chase the player in water.")]
    public float DetectionRadiusNormal = 7f;
    [Tooltip("The detection radius when the player's lantern is on.")]
    public float DetectionRadiusLantern = 15f;
    [Tooltip("The unconditional detection radius. If the player is this close in the zone, the lantern state doesn't matter.")]
    public float PointBlankRadius = 3f;
    [Tooltip("The range at which the Nixie can attack the player.")]
    public float AttackRange = 1f;

    [Header("Behavior Timers")]
    [Tooltip("How long the Nixie remains stunned after attacking or being shouted at.")]
    public float StunDuration = 3f;
    [Tooltip("How long the player can be in the Nixie's zone with the lantern OFF before being automatically detected.")]
    public float MaxTensionDuration = 20f;

    [Header("Environment")]
    [Tooltip("The specific WaterZone this Nixie lives in. It will only react to the player entering this zone.")]
    public NixieZone MyNixieZone;

    [Header("Vocalizations & SFX")]
    public List<AudioClip> LuringVocalizations;
    public AudioClip AttackSound;

    // --- Component & Runtime References ---
    public NixieStateMachine StateMachine { get; private set; }
    public NixieNavigation Navigation { get; private set; }
    public AudioSource AudioSource { get; private set; }
    public Transform PlayerTransform { get; private set; }
    public PlayerStatus PlayerStatus { get; private set; }
    public float DistanceToPlayer { get; private set; }
    public Vector3 PlayerLastKnownPosition { get; set; }

    private float tensionTimer;

    public float CurrentDetectionRadius
    {
        get
        {
            if (PlayerStatus != null && PlayerStatus.IsLanternOn)
            {
                return DetectionRadiusLantern;
            }
            return DetectionRadiusNormal;
        }
    }

    public bool IsPlayerInMyZone
    {
        get
        {
            return PlayerStatus != null && MyNixieZone != null && PlayerStatus.CurrentNixieZone == MyNixieZone;
        }
    }

    void Awake()
    {
        StateMachine = GetComponent<NixieStateMachine>();
        Navigation = GetComponent<NixieNavigation>();
        AudioSource = GetComponent<AudioSource>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            PlayerTransform = playerObj.transform;
            PlayerStatus = playerObj.GetComponent<PlayerStatus>();
        }
        else
        {
            Debug.LogError("NixieAI: Player not found! Make sure the player has the 'Player' tag.");
        }
    }

    void OnEnable()
    {
        PlayerActionEventBus.OnPlayerShouted += HandlePlayerShout;
    }

    void OnDisable()
    {
        PlayerActionEventBus.OnPlayerShouted -= HandlePlayerShout;
    }

    void Update()
    {
        if (PlayerTransform == null) return;

        DistanceToPlayer = Vector3.Distance(transform.position, PlayerTransform.position);

        if (IsPlayerInMyZone && !PlayerStatus.IsLanternOn &&
            StateMachine.CurrentState != StateMachine.ChasingState &&
            StateMachine.CurrentState != StateMachine.StaringState)
        {
            tensionTimer += Time.deltaTime;
            if (tensionTimer >= MaxTensionDuration)
            {
                Debug.Log("Tension timer expired! Nixie has found the player.");
                // Force a transition to Chasing state, bypassing normal checks
                StateMachine.TransitToState(StateMachine.ChasingState);
                tensionTimer = 0f; // Reset the timer
            }
        }
        else
        {
            // Reset the timer if the condition is not met (player leaves, turns on lantern, etc.)
            tensionTimer = 0f;
        }
    }

    private void HandlePlayerShout(Vector3 shoutPosition)
    {
        // The event sends the shout position, so we must accept the parameter,
        // even if our current logic only checks the distance.
        if (DistanceToPlayer <= StaringRadius)
        {
            Debug.Log("Nixie was stunned by a shout!");
            StateMachine.TransitToState(StateMachine.StuntedState);
        }
    }

    public void PlayLuringSound()
    {
        if (LuringVocalizations == null || LuringVocalizations.Count == 0) return;
        AudioClip clip = LuringVocalizations[Random.Range(0, LuringVocalizations.Count)];
        AudioSource.PlayOneShot(clip);
    }

    public void PlayAttackSound()
    {
        if (AttackSound == null) return;
        AudioSource.PlayOneShot(AttackSound);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;

        // --- Draw Radiuses ---

        // Staring Radius (Blue)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(pos, StaringRadius);
        DrawGizmoLabel(pos + Vector3.up * StaringRadius, "Staring Radius", Color.blue);

        // Attack Range (Red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, AttackRange);
        DrawGizmoLabel(pos + Vector3.up * AttackRange, "Attack Range", Color.red);

        // Point-Blank Radius (White)
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(pos, PointBlankRadius);
        DrawGizmoLabel(pos - Vector3.up * PointBlankRadius, "Point-Blank", Color.white);

        // Detection Radius - shows the currently active one
        // We use DrawWireSphere here as well for consistency
        bool lanternOn = (Application.isPlaying && PlayerStatus != null && PlayerStatus.IsLanternOn);
        if (lanternOn)
        {
            // Lantern Detection Radius (Orange)
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
            Gizmos.DrawWireSphere(pos, DetectionRadiusLantern);
            DrawGizmoLabel(pos + Vector3.forward * DetectionRadiusLantern, "Detection (Lantern)", Gizmos.color);
        }
        else
        {
            // Normal Detection Radius (Yellow)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pos, DetectionRadiusNormal);
            DrawGizmoLabel(pos + Vector3.forward * DetectionRadiusNormal, "Detection (Normal)", Gizmos.color);
        }

        // --- Draw Lines ---
        if (PlayerTransform != null)
        {
            // Line to player for clarity
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pos, PlayerTransform.position);

            // Line to Last Known Position if lurking
            if (Application.isPlaying && StateMachine.CurrentState == StateMachine.LurkingState)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(pos, PlayerLastKnownPosition);
                Gizmos.DrawSphere(PlayerLastKnownPosition, 0.5f);
                DrawGizmoLabel(PlayerLastKnownPosition, "LKP", Color.magenta);
            }
        }
    }

    // Helper method to draw text labels in the scene view
    private void DrawGizmoLabel(Vector3 position, string text, Color color)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.Label(position, text);
#endif
    }
}