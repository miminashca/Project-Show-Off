using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic; // For NodeGraph if it's a List

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
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

    [Header("Gameplay Rules")]
    public float WaterSurfaceYLevel = 0.5f;
    // public HunterNodeGraph NodeGraph; // Assign if you create a HunterNodeGraph component/ScriptableObject

    [Header("VFX/SFX (Assign in Inspector)")]
    public GameObject MuzzleFlashPrefab;
    public GameObject BulletImpactPlayerPrefab;
    public GameObject BulletImpactObstaclePrefab;
    public GameObject BulletImpactWaterPrefab;
    public AudioClip GunshotSound;
    public AudioClip ReloadSound;
    public AudioClip SpottedPlayerSound;
    public AudioClip HeardNoiseSound;
    public AudioClip StartAimingSound;

    // --- Component References (public properties for states to access) ---
    public NavMeshAgent NavAgent { get; private set; }
    public HunterNavigation Navigation { get; private set; }
    public Animator HunterAnimator { get; private set; }
    public AudioSource HunterAudioSource { get; private set; }
    public PlayerStatus TargetPlayerStatus { get; private set; }

    // --- Runtime AI Data (public properties for states to access) ---
    public Vector3 LastKnownPlayerPosition { get; set; }
    public bool IsPlayerVisible { get; private set; }
    public bool CanHearPlayerAlert { get; private set; }
    public float CurrentInvestigationTimer { get; set; }
    public float CurrentAimTimer { get; set; }
    public float CurrentReloadTimer { get; set; }
    public Transform CurrentTargetNode { get; set; }
    public Vector3 CurrentConfirmedAimTarget { get; set; }

    void Awake()
    {
        NavAgent = GetComponent<NavMeshAgent>();
        HunterAnimator = GetComponent<Animator>();
        HunterAudioSource = GetComponent<AudioSource>();

        if (PlayerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                PlayerTransform = playerObj.transform;
                TargetPlayerStatus = playerObj.GetComponent<PlayerStatus>(); // Get PlayerStatus component
            }
            else Debug.LogError("HunterAI: PlayerTransform not assigned and Player not found by tag!", this);
        }
        else
        {
            TargetPlayerStatus = PlayerTransform.GetComponent<PlayerStatus>();
        }

        Navigation = GetComponent<HunterNavigation>();
        if (Navigation == null)
        {
            Debug.LogError("HunterAI requires a HunterNavigation component on the same GameObject!", this);
            enabled = false;
        }

        if (EyeLevelTransform == null) EyeLevelTransform = transform;
        if (GunMuzzleTransform == null) GunMuzzleTransform = transform;
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
        PlayerActionEventBus.OnPlayerShouted -= HandlePlayerShoutEvent;
    }

    void Update()
    {
        if (PlayerTransform == null)
        {
            IsPlayerVisible = false;
            return;
        }
        ProcessSensors();
    }

    void ProcessSensors()
    {
        IsPlayerVisible = false;
        if (PlayerTransform == null || EyeLevelTransform == null) return;

        Vector3 playerTargetPoint = PlayerTransform.position + Vector3.up * 1.0f;
        Vector3 directionToPlayer = playerTargetPoint - EyeLevelTransform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer <= VisionConeRange)
        {
            if (Vector3.Angle(EyeLevelTransform.forward, directionToPlayer.normalized) <= VisionConeAngle / 2f)
            {
                // Optional: Tall Grass / Crouch check
                // if (TargetPlayerStatus != null && TargetPlayerStatus.IsCrouching && TargetPlayerStatus.IsInTallGrassZone) {
                //    IsPlayerVisible = false; // Player is hidden
                //    return;
                // }

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
        if (this == null || !enabled || !gameObject.activeInHierarchy) return;

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

    public void PlaySound(AudioClip clip)
    {
        if (clip != null && HunterAudioSource != null)
        {
            HunterAudioSource.PlayOneShot(clip);
        }
    }


    public void FireGun()
    {
        Debug.Log($"{gameObject.name}: BANG!");
        HunterAnimator.SetTrigger("Shoot");
        HunterEventBus.HunterFiredShot();
        PlaySound(GunshotSound);

        if (MuzzleFlashPrefab != null && GunMuzzleTransform != null)
        {
            Instantiate(MuzzleFlashPrefab, GunMuzzleTransform.position, GunMuzzleTransform.rotation, GunMuzzleTransform); // Parent to muzzle for lifetime
        }

        if (PlayerTransform == null || GunMuzzleTransform == null) return;

        Vector3 aimTargetToUse = CurrentConfirmedAimTarget; // Use the target confirmed by AimingState
        if (aimTargetToUse == Vector3.zero) // Fallback if not set (shouldn't happen in normal flow)
        {
            Debug.LogWarning($"{gameObject.name}: CurrentConfirmedAimTarget was zero, using PlayerTransform direct for shot.");
            aimTargetToUse = PlayerTransform.position + Vector3.up * 1.0f;
        }

        Vector3 directionToTarget = (aimTargetToUse - GunMuzzleTransform.position).normalized;
        float shotDistance = ShootingRange * 1.2f; // Give a little extra range for the raycast

        RaycastHit hit;
        int hunterLayer = LayerMask.NameToLayer("Hunter");
        LayerMask shootableMask = ~(1 << hunterLayer); // Don't hit self

        // Check for water impact directly, even if IsPathToPlayerClearForShot passed
        // This handles if player moved into water at the last micro-second.
        if (TargetPlayerStatus != null && TargetPlayerStatus.IsSubmerged(aimTargetToUse, WaterSurfaceYLevel))
        {
            Debug.Log($"{gameObject.name} SHOT hit water (player submerged).");
            if (BulletImpactWaterPrefab != null)
            {
                // Attempt to find where the bullet would hit the water surface
                // This is a simplified raycast towards the aim target, capped by water surface
                Ray waterRay = new Ray(GunMuzzleTransform.position, directionToTarget);
                Plane waterPlane = new Plane(Vector3.up, new Vector3(0, WaterSurfaceYLevel, 0));
                if (waterPlane.Raycast(waterRay, out float enterDist))
                {
                    if (enterDist <= shotDistance)
                    {
                        Instantiate(BulletImpactWaterPrefab, waterRay.GetPoint(enterDist), Quaternion.LookRotation(Vector3.down));
                    }
                }
            }
            return; // Shot is ineffective
        }


        if (Physics.Raycast(GunMuzzleTransform.position, directionToTarget, out hit, shotDistance, shootableMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.CompareTag("Player"))
            {
                Debug.Log($"{gameObject.name} HIT Player: {hit.collider.name}");
                PlayerHealth playerHealth = hit.collider.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(GunDamage);
                }
                if (BulletImpactPlayerPrefab != null)
                {
                    Instantiate(BulletImpactPlayerPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                }
            }
            else
            {
                Debug.Log($"{gameObject.name} HIT Obstacle: {hit.collider.name} at {hit.point}");
                if (BulletImpactObstaclePrefab != null)
                {
                    Instantiate(BulletImpactObstaclePrefab, hit.point, Quaternion.LookRotation(hit.normal));
                }
            }
        }
        else
        {
            Debug.Log($"{gameObject.name} SHOT missed (hit nothing within range).");
        }
    }

    public Transform GetConfiguredRoamNode()
    {
        if (Navigation != null)
        {
            return Navigation.GetNextRoamNode();
        }
        Debug.LogWarning("GetConfiguredRoamNode: HunterNavigation component not found.", this);
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
        Gizmos.color = new Color(1f, 0f, 1f, 0.5f); // Melee Range Gizmo
        Gizmos.DrawWireSphere(transform.position, MeleeRange);

        if (LastKnownPlayerPosition != Vector3.zero)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(LastKnownPlayerPosition, 0.5f);
        }

        if (CurrentConfirmedAimTarget != Vector3.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(CurrentConfirmedAimTarget, 0.3f);
            if (GunMuzzleTransform != null) Gizmos.DrawLine(GunMuzzleTransform.position, CurrentConfirmedAimTarget);
        }
    }
}