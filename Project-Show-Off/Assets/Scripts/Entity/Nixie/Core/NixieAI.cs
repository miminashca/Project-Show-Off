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

    [Header("Vocalizations & SFX")]
    public List<AudioClip> LuringVocalizations;
    public AudioClip AttackSound;

    // --- Component & Runtime References ---
    // This is of type NixieStateMachine, so we can access its specific states.
    public NixieStateMachine StateMachine { get; private set; }
    public NixieNavigation Navigation { get; private set; } // Renamed for consistency from the state machine
    public AudioSource AudioSource { get; private set; }
    public Transform PlayerTransform { get; private set; }
    // Note: You will need a script on the player to track these stats.
    // public PlayerStatus PlayerStatus { get; private set; }

    // --- Runtime Data ---
    public float DistanceToPlayer { get; private set; }
    public bool IsPlayerInWater { get; set; } // This should be set by a water trigger zone

    public float CurrentDetectionRadius
    {
        get
        {
            // Simplified check. Replace with your actual PlayerStatus logic.
            // if (PlayerStatus != null && PlayerStatus.IsLanternOn)
            // {
            //     return DetectionRadiusLantern;
            // }
            return DetectionRadiusNormal;
        }
    }

    void Awake()
    {
        StateMachine = GetComponent<NixieStateMachine>();
        Navigation = GetComponent<NixieNavigation>();
        AudioSource = GetComponent<AudioSource>();

        // Find the player in the scene
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            PlayerTransform = playerObj.transform;
            // PlayerStatus = playerObj.GetComponent<PlayerStatus>();
        }
        else
        {
            Debug.LogError("NixieAI: Player not found! Make sure the player has the 'Player' tag.");
        }
    }

    void OnEnable()
    {
        // Subscribe to the player's "HEY!" shout event
        // Example: PlayerActionEventBus.OnPlayerShouted += HandlePlayerShout;
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        // Example: PlayerActionEventBus.OnPlayerShouted -= HandlePlayerShout;
    }

    void Update()
    {
        if (PlayerTransform == null) return;

        // Calculate distance to player once per frame for efficiency
        DistanceToPlayer = Vector3.Distance(transform.position, PlayerTransform.position);
    }

    /// <summary>
    /// Triggered by the player's "HEY!" shout event.
    /// </summary>
    private void HandlePlayerShout()
    {
        // Only get stunned if the shout is within the staring radius
        if (DistanceToPlayer <= StaringRadius)
        {
            Debug.Log("Nixie was stunned by a shout!");
            // --- THIS IS THE CORRECTED LINE ---
            // It now uses TransitToState and accesses the public StuntedState property from NixieStateMachine.
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
}