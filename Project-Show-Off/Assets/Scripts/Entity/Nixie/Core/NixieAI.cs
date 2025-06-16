using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(NixieStateMachine), typeof(NixieNavigation), typeof(AudioSource))]
public class NixieAI : MonoBehaviour
{
    [Header("Sensory Parameters")]
    [Tooltip("The radius at which the Nixie will stop and stare at the player.")]
    public float StaringRadius = 40f;
    [Tooltip("The radius at which the Nixie will detect and chase the player in water.")]
    public float DetectionRadiusNormal = 15f;
    [Tooltip("The detection radius when the player's lantern is on.")]
    public float DetectionRadiusLantern = 30f;
    [Tooltip("The range at which the Nixie can attack the player.")]
    public float AttackRange = 1f;

    [Header("Behavior Timers")]
    [Tooltip("How long the Nixie remains stunned after attacking or being shouted at.")]
    public float StunDuration = 3f;

    [Header("Environment")]
    [Tooltip("The specific WaterZone this Nixie lives in. It will only react to the player entering this zone.")]
    public WaterZone MyWaterZone;

    [Header("Vocalizations & SFX")]
    public List<AudioClip> LuringVocalizations;
    public AudioClip AttackSound;

    // --- Component & Runtime References ---
    public NixieStateMachine StateMachine { get; private set; }
    public NixieNavigation Navigation { get; private set; }
    public AudioSource AudioSource { get; private set; }
    public Transform PlayerTransform { get; private set; }
    public PlayerStatus PlayerStatus { get; private set; }

    // --- Runtime Data ---
    public float DistanceToPlayer { get; private set; }

    // Note: The old IsPlayerInWater property is no longer needed with the zone-specific logic
    // public bool IsPlayerInWater { get; set; } 

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

    public bool IsPlayerInMyWater
    {
        get
        {
            return PlayerStatus != null && MyWaterZone != null && PlayerStatus.CurrentWaterZone == MyWaterZone;
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
    }

    /// <summary>
    /// Triggered by the player's "HEY!" shout event.
    /// </summary>
    // --- THIS IS THE CORRECTED LINE ---
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
        // Gizmos are only drawn for the selected object, which is good for performance.
        // Staring Radius (Blue)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, StaringRadius);

        // Attack Range (Red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRange);

        // Draw the current detection radius
        // We use DrawWireSphere here as well for consistency
        if (PlayerStatus != null && PlayerStatus.IsLanternOn)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
            Gizmos.DrawWireSphere(transform.position, DetectionRadiusLantern);
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, DetectionRadiusNormal);
        }

        // Line to player for clarity
        if (PlayerTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, PlayerTransform.position);
        }
    }
}