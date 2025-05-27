using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic; // For NodeGraph if it's a List

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
// Ensure ThimbleHunterStateMachine is also on this GameObject
[RequireComponent(typeof(HunterStateMachine))]
public class HunterAI : MonoBehaviour
{
    [Header("Core Attributes")]
    public float MaxSuperpositionDistance = 50f;
    public float VisionConeAngle = 90f;
    public float VisionConeRange = 30f;
    public float AuditoryDetectionRange = 20f;
    public float ShootingRange = 15f;
    public float MeleeRange = 1.5f;
    public int GunDamage = 100;

    [Header("Movement Speeds")]
    public float MovementSpeedRoaming = 2f;
    public float MovementSpeedInvestigating = 3f;
    public float MovementSpeedChasing = 4.5f;

    [Header("Timers")]
    public float AimTime = 2.0f;
    public float ReloadTime = 3.0f;
    public float InvestigationDuration = 8.0f;

    [Header("References")]
    public Transform PlayerTransform;
    public Transform GunMuzzleTransform;
    public Transform EyeLevelTransform;
    // public HunterNodeGraph NodeGraph; // Assign if you create a HunterNodeGraph component/ScriptableObject

    // --- Component References (public properties for states to access) ---
    public NavMeshAgent NavAgent { get; private set; }
    public HunterNavigation Navigation { get; private set; }
    public Animator HunterAnimator { get; private set; }
    public AudioSource HunterAudioSource { get; private set; }

    // --- Runtime AI Data (public properties for states to access) ---
    public Vector3 LastKnownPlayerPosition { get; set; }
    public bool IsPlayerVisible { get; private set; }
    public bool CanHearPlayerAlert { get; private set; }
    public float CurrentInvestigationTimer { get; set; }
    public float CurrentAimTimer { get; set; }
    public float CurrentReloadTimer { get; set; }
    public Transform CurrentTargetNode { get; set; }

    void Awake()
    {
        NavAgent = GetComponent<NavMeshAgent>();
        HunterAnimator = GetComponent<Animator>();
        HunterAudioSource = GetComponent<AudioSource>();

        if (PlayerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) PlayerTransform = playerObj.transform;
            else Debug.LogError("ThimbleHunterAI: PlayerTransform not assigned and Player not found by tag!", this);
        }

        Navigation = GetComponent<HunterNavigation>();
        if (Navigation == null)
        {
            Debug.LogError("ThimbleHunterAI requires a HunterNavigation component on the same GameObject!", this);
            enabled = false; // Or add it automatically: Navigation = gameObject.AddComponent<HunterNavigation>();
        }

        if (EyeLevelTransform == null) EyeLevelTransform = transform; // Default to self if not set
        if (GunMuzzleTransform == null) GunMuzzleTransform = transform; // Default to self if not set
    }

    void OnEnable()
    {
        PlayerActionEventBus.OnPlayerShouted += HandlePlayerShoutEvent;
    }

    void OnDisable()
    {
        PlayerActionEventBus.OnPlayerShouted -= HandlePlayerShoutEvent;
    }

    void OnDestroy()
    {
        HunterEventBus.OnPlayerShouted -= HandlePlayerShoutEvent;
    }

    // This Update is for continuous sensor processing, not state logic.
    // The StateMachine's Update will call the current state's Handle().
    void Update()
    {
        if (PlayerTransform == null) return;
        ProcessSensors();
    }

    void ProcessSensors()
    {
        IsPlayerVisible = false;
        if (PlayerTransform == null || EyeLevelTransform == null) return;

        Vector3 directionToPlayer = (PlayerTransform.position + Vector3.up * 1.0f) - EyeLevelTransform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer <= VisionConeRange)
        {
            if (Vector3.Angle(EyeLevelTransform.forward, directionToPlayer.normalized) <= VisionConeAngle / 2f)
            {
                RaycastHit hit;
                int hunterLayer = LayerMask.NameToLayer("Hunter");
                LayerMask ignoreHunterMask = ~(1 << hunterLayer);
                if (Physics.Raycast(EyeLevelTransform.position, directionToPlayer.normalized, out hit, VisionConeRange, ignoreHunterMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.CompareTag("Player"))
                    {
                        IsPlayerVisible = true;
                        LastKnownPlayerPosition = PlayerTransform.position;
                    }
                }
            }
        }
    }

    private void HandlePlayerShoutEvent(Vector3 shoutPosition)
    {
        if (this == null || !enabled || !gameObject.activeInHierarchy) return; // Safety check

        if (Vector3.Distance(transform.position, shoutPosition) <= AuditoryDetectionRange)
        {
            CanHearPlayerAlert = true;
            LastKnownPlayerPosition = shoutPosition;
            Debug.Log($"{gameObject.name} heard player shout at {shoutPosition} (Dist: {Vector3.Distance(transform.position, shoutPosition)}). LKP updated. CanHearPlayerAlert = true");
        }
        else
        {
            Debug.Log($"{gameObject.name} heard player shout at {shoutPosition} but was too far (Dist: {Vector3.Distance(transform.position, shoutPosition)}, Range: {AuditoryDetectionRange}).");
        }
    }

    public void AcknowledgePlayerAlert()
    {
        CanHearPlayerAlert = false;
        Debug.Log($"{gameObject.name} acknowledged player alert. CanHearPlayerAlert = false");
    }


    public void FireGun()
    {
        Debug.Log($"{gameObject.name}: BANG!");
        HunterAnimator.SetTrigger("Shoot");
        HunterEventBus.HunterFiredShot();
        // Implement actual raycast, damage, VFX, SFX here
    }

    public Transform GetRandomNodeFromGraph()
    {
        if (Navigation != null)
        {
            return Navigation.GetNextRoamNode();
        }
        Debug.LogWarning("GetRandomNodeFromGraph: HunterNavigation component not found.", this);
        return null;
    }

    public Transform GetSuperpositionNode()
    {
        if (Navigation != null)
        {
            return Navigation.GetSuperpositionNode();
        }
        Debug.LogWarning("GetSuperpositionNode: HunterNavigation component not found.", this);
        return null;
    }

    public bool IsPathToPlayerClearForShot(Vector3 aimPoint)
    {
        if (GunMuzzleTransform == null) return false;
        Vector3 directionToAimPoint = (aimPoint - GunMuzzleTransform.position).normalized;
        float distanceToAimPoint = Vector3.Distance(GunMuzzleTransform.position, aimPoint);
        int hunterLayer = LayerMask.NameToLayer("Hunter");
        LayerMask ignoreHunterMask = ~(1 << hunterLayer);
        RaycastHit hit;
        if (Physics.Raycast(GunMuzzleTransform.position, directionToAimPoint, out hit, distanceToAimPoint * 1.05f, ignoreHunterMask, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.CompareTag("Player");
        }
        return true;
    }

    void OnDrawGizmosSelected()
    {
        if (EyeLevelTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(EyeLevelTransform.position, VisionConeRange);
            Vector3 fovLine1 = Quaternion.AngleAxis(VisionConeAngle / 2, EyeLevelTransform.up) * EyeLevelTransform.forward * VisionConeRange;
            Vector3 fovLine2 = Quaternion.AngleAxis(-VisionConeAngle / 2, EyeLevelTransform.up) * EyeLevelTransform.forward * VisionConeRange;
            Gizmos.DrawLine(EyeLevelTransform.position, EyeLevelTransform.position + fovLine1);
            Gizmos.DrawLine(EyeLevelTransform.position, EyeLevelTransform.position + fovLine2);
            if (IsPlayerVisible && PlayerTransform != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(EyeLevelTransform.position, PlayerTransform.position + Vector3.up * 1.0f);
            }
        }
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, AuditoryDetectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, ShootingRange);
        if (LastKnownPlayerPosition != Vector3.zero)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(LastKnownPlayerPosition, 0.5f);
        }
    }
}