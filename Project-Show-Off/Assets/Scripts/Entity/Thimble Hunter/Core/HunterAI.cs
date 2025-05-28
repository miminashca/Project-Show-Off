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
    public float SuperpositionAttemptCooldown = 10.0f;

    [Header("Player Targeting Offsets")]
    public Vector3 PlayerVisibilityPointOffsetStanding = new Vector3(0, 1.6f, 0); // Approx head height when standing
    public Vector3 PlayerVisibilityPointOffsetCrouching = new Vector3(0, 0.9f, 0); // Approx head height when crouching
    public Vector3 PlayerAimPointOffsetStanding = new Vector3(0, 1.0f, 0);     // Approx torso center when standing
    public Vector3 PlayerAimPointOffsetCrouching = new Vector3(0, 0.7f, 0);    // Approx torso center when crouching

    [Header("References")]
    public Transform PlayerTransform;
    public Transform GunMuzzleTransform;
    public Transform EyeLevelTransform;

    [Header("Gameplay Rules")]
    public float WaterSurfaceYLevel = 0.5f;

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
    public float CurrentSuperpositionCooldownTimer { get; set; }
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

        CurrentSuperpositionCooldownTimer = 0f;
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
        if (CurrentSuperpositionCooldownTimer > 0)
        {
            CurrentSuperpositionCooldownTimer -= Time.deltaTime;
        }

        if (PlayerTransform == null)
        {
            IsPlayerVisible = false;
            return;
        }
        ProcessSensors();
    }

    public Vector3 GetPlayerVisibilityCheckPoint()
    {
        if (PlayerTransform == null) return Vector3.zero; // Or some invalid position
        if (TargetPlayerStatus != null && TargetPlayerStatus.IsCrouching)
        {
            return PlayerTransform.position + PlayerVisibilityPointOffsetCrouching;
        }
        return PlayerTransform.position + PlayerVisibilityPointOffsetStanding;
    }

    public Vector3 GetPlayerAimPoint()
    {
        if (PlayerTransform == null) return Vector3.zero;
        if (TargetPlayerStatus != null && TargetPlayerStatus.IsCrouching)
        {
            return PlayerTransform.position + PlayerAimPointOffsetCrouching;
        }
        return PlayerTransform.position + PlayerAimPointOffsetStanding;
    }

    void ProcessSensors()
    {
        IsPlayerVisible = false;
        if (PlayerTransform == null || EyeLevelTransform == null) return;

        Vector3 playerTargetPoint = GetPlayerVisibilityCheckPoint();
        if (playerTargetPoint == Vector3.zero) return;

        // Optional: If visibility point itself is underwater, player is not visible from there.
        if (TargetPlayerStatus != null && TargetPlayerStatus.IsSubmerged(playerTargetPoint, WaterSurfaceYLevel))
        {
            // Debug.Log("Player visibility point is submerged.");
            IsPlayerVisible = false;
            return;
        }

        Vector3 directionToPlayer = playerTargetPoint - EyeLevelTransform.position;
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
                        LastKnownPlayerPosition = PlayerTransform.position; // LKP is player's base position
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
            PlaySound(HeardNoiseSound);
        }
        // Removed the "too far" debug log for brevity during play
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
            Instantiate(MuzzleFlashPrefab, GunMuzzleTransform.position, GunMuzzleTransform.rotation, GunMuzzleTransform);
        }

        if (PlayerTransform == null || GunMuzzleTransform == null) return;

        Vector3 aimTargetToUse = CurrentConfirmedAimTarget;
        if (aimTargetToUse == Vector3.zero)
        {
            Debug.LogWarning($"{gameObject.name}: CurrentConfirmedAimTarget was zero, using dynamic PlayerAimPoint for shot.");
            aimTargetToUse = GetPlayerAimPoint(); // Get current best aim point
            if (aimTargetToUse == Vector3.zero) return; // Player likely gone
        }

        // Final check: Is this confirmed/derived aim target submerged?
        if (TargetPlayerStatus != null && TargetPlayerStatus.IsSubmerged(aimTargetToUse, WaterSurfaceYLevel))
        {
            Debug.Log($"{gameObject.name} SHOT aimed at submerged point. Impacting water.");
            if (BulletImpactWaterPrefab != null)
            {
                Vector3 directionToSubmergedTarget = (aimTargetToUse - GunMuzzleTransform.position).normalized;
                Ray waterRay = new Ray(GunMuzzleTransform.position, directionToSubmergedTarget);
                Plane waterPlane = new Plane(Vector3.up, new Vector3(0, WaterSurfaceYLevel, 0));
                if (waterPlane.Raycast(waterRay, out float enterDist))
                {
                    if (enterDist <= ShootingRange * 1.2f) // Use shotDistance concept
                    {
                        Instantiate(BulletImpactWaterPrefab, waterRay.GetPoint(enterDist), Quaternion.LookRotation(Vector3.down));
                    }
                }
            }
            return; // Shot is ineffective
        }

        // Proceed with raycast if aim target is not submerged
        Vector3 directionToTarget = (aimTargetToUse - GunMuzzleTransform.position).normalized;
        float shotDistance = ShootingRange * 1.2f;

        RaycastHit hit;
        int hunterLayer = LayerMask.NameToLayer("Hunter");
        LayerMask shootableMask = ~(1 << hunterLayer);

        if (Physics.Raycast(GunMuzzleTransform.position, directionToTarget, out hit, shotDistance, shootableMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.CompareTag("Player"))
            {
                // Check if the *actual hit point on the player* is submerged (e.g., shot low, hit legs in water)
                if (TargetPlayerStatus != null && TargetPlayerStatus.IsSubmerged(hit.point, WaterSurfaceYLevel))
                {
                    Debug.Log($"{gameObject.name} SHOT HIT Player's submerged part at {hit.point}. Impacting water.");
                    if (BulletImpactWaterPrefab != null) Instantiate(BulletImpactWaterPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                }
                else
                {
                    Debug.Log($"{gameObject.name} HIT Player: {hit.collider.name} at {hit.point}");
                    PlayerHealth playerHealth = hit.collider.GetComponent<PlayerHealth>();
                    if (playerHealth != null) playerHealth.TakeDamage(GunDamage);
                    if (BulletImpactPlayerPrefab != null) Instantiate(BulletImpactPlayerPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                }
            }
            else
            {
                Debug.Log($"{gameObject.name} HIT Obstacle: {hit.collider.name} at {hit.point}");
                if (BulletImpactObstaclePrefab != null) Instantiate(BulletImpactObstaclePrefab, hit.point, Quaternion.LookRotation(hit.normal));
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
        if (PlayerTransform == null) return false;

        // 1. Check if the intended aimPoint itself is submerged
        if (TargetPlayerStatus != null && TargetPlayerStatus.IsSubmerged(aimPoint, WaterSurfaceYLevel))
        {
            Debug.Log($"{gameObject.name}: Aim point for shot ({aimPoint}) is submerged. Path NOT clear.");
            return false;
        }

        if (GunMuzzleTransform == null) return false;

        Vector3 directionToAimPoint = (aimPoint - GunMuzzleTransform.position).normalized;
        float distanceToAimPoint = Vector3.Distance(GunMuzzleTransform.position, aimPoint);

        // Prevent issues with zero distance/direction
        if (distanceToAimPoint < 0.01f) return true; // Effectively at the target, assume clear

        int hunterLayer = LayerMask.NameToLayer("Hunter");
        LayerMask ignoreHunterMask = ~(1 << hunterLayer);
        RaycastHit hit;

        // 2. Raycast *just short* of the aimPoint to see if anything obstructs the path
        // We use 0.99f * distance to ensure we don't hit the player target itself with this check
        if (Physics.Raycast(GunMuzzleTransform.position, directionToAimPoint, out hit, distanceToAimPoint * 0.99f, ignoreHunterMask, QueryTriggerInteraction.Ignore))
        {
            // If this raycast hits something, it's an obstacle before reaching the player.
            Debug.Log($"{gameObject.name}: Path to player for shot blocked by obstacle: {hit.collider.name}");
            return false;
        }

        // 3. If no obstacle was hit, the path to (just before) the aimPoint is clear.
        // The assumption is that aimPoint is accurately on the player.
        // A final direct check to player can be redundant if aimPoint is trusted, but ensures the very end is clear.
        // For simplicity now, if the short raycast is clear, we consider the path clear to the aimpoint.
        return true;
    }

    void OnDrawGizmosSelected()
    {
        // --- Vision Cone & Sensor Ranges ---
        if (EyeLevelTransform != null)
        {
            Gizmos.color = IsPlayerVisible ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(EyeLevelTransform.position, VisionConeRange);
            Vector3 fovLine1 = Quaternion.AngleAxis(VisionConeAngle / 2, EyeLevelTransform.up) * EyeLevelTransform.forward * VisionConeRange;
            Vector3 fovLine2 = Quaternion.AngleAxis(-VisionConeAngle / 2, EyeLevelTransform.up) * EyeLevelTransform.forward * VisionConeRange;
            Gizmos.DrawLine(EyeLevelTransform.position, EyeLevelTransform.position + fovLine1);
            Gizmos.DrawLine(EyeLevelTransform.position, EyeLevelTransform.position + fovLine2);

            if (PlayerTransform != null)
            {
                Vector3 currentVisibilityPoint = GetPlayerVisibilityCheckPoint();
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(currentVisibilityPoint, 0.15f);
                if (IsPlayerVisible)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(EyeLevelTransform.position, currentVisibilityPoint);
                }
                else if (Vector3.Distance(EyeLevelTransform.position, currentVisibilityPoint) <= VisionConeRange)
                {
                    Gizmos.color = Color.red; // In range but not visible (LoS blocked or angle)
                    Gizmos.DrawLine(EyeLevelTransform.position, currentVisibilityPoint);
                }
            }
        }
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, AuditoryDetectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, ShootingRange);
        Gizmos.color = new Color(1f, 0f, 1f, 0.5f); // Melee
        Gizmos.DrawWireSphere(transform.position, MeleeRange);

        // --- Superposition Trigger Distance ---
        Gizmos.color = new Color(0.8f, 0.5f, 0.2f, 0.7f); // Orange for superposition trigger
        Gizmos.DrawWireSphere(transform.position, MaxSuperpositionDistance);
        if (PlayerTransform != null && Vector3.Distance(transform.position, PlayerTransform.position) > MaxSuperpositionDistance)
        {
            Gizmos.color = Color.red; // Indicate player is outside this range
            Gizmos.DrawLine(transform.position, PlayerTransform.position);
        }


        // --- Last Known Player Position ---
        if (LastKnownPlayerPosition != Vector3.zero)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(LastKnownPlayerPosition, 0.5f);
        }

        // --- Current Confirmed Aim Target (from Aiming State) ---
        if (CurrentConfirmedAimTarget != Vector3.zero)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(CurrentConfirmedAimTarget, 0.2f);
            if (GunMuzzleTransform != null) Gizmos.DrawLine(GunMuzzleTransform.position, CurrentConfirmedAimTarget);
        }

        // --- Water Surface Visualization & Player Submergence ---
        if (PlayerTransform != null)
        {
            Vector3 playerBase = PlayerTransform.position;
            float lineLength = 5f; // Length of the water level indicator lines

            // Draw a simple representation of water level around player
            Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.4f); // Light blue, semi-transparent
            Vector3 waterLineStart, waterLineEnd;

            // Forward/Backward lines at water level
            waterLineStart = new Vector3(playerBase.x, WaterSurfaceYLevel, playerBase.z - lineLength / 2);
            waterLineEnd = new Vector3(playerBase.x, WaterSurfaceYLevel, playerBase.z + lineLength / 2);
            Gizmos.DrawLine(waterLineStart, waterLineEnd);

            // Left/Right lines at water level
            waterLineStart = new Vector3(playerBase.x - lineLength / 2, WaterSurfaceYLevel, playerBase.z);
            waterLineEnd = new Vector3(playerBase.x + lineLength / 2, WaterSurfaceYLevel, playerBase.z);
            Gizmos.DrawLine(waterLineStart, waterLineEnd);

            // Indicate if player's current AIM POINT is submerged
            Vector3 currentAimGizmoPoint = GetPlayerAimPoint();
            if (TargetPlayerStatus != null && TargetPlayerStatus.IsSubmerged(currentAimGizmoPoint, WaterSurfaceYLevel))
            {
                Gizmos.color = Color.blue; // Dark blue if aim point submerged
                Gizmos.DrawSphere(currentAimGizmoPoint, 0.25f);
                Gizmos.DrawLine(currentAimGizmoPoint, new Vector3(currentAimGizmoPoint.x, WaterSurfaceYLevel, currentAimGizmoPoint.z)); // Line to surface
            }
            else
            {
                Gizmos.color = Color.yellow; // Yellow if aim point above water
                Gizmos.DrawSphere(currentAimGizmoPoint, 0.25f);
            }
        }
    }
}