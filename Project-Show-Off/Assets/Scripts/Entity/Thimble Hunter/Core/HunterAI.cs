using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

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

    [Header("Detection System")]
    public float BaseDetectionRate = 0.5f; // Units: progress/second (0 to 1)
    public float DetectionDecayRate = 0.3f; // Units: progress/second
    [Range(0f, 1f)]
    public float DetectionProgress { get; private set; } = 0f;
    public bool IsPlayerFullySpotted { get; private set; } = false;
    [Tooltip("Detection progress must drop below this for IsPlayerFullySpotted to become false.")]
    public float FullySpottedLossThreshold = 0.8f; // e.g., if progress drops below 0.8, no longer "fully spotted"

    [Header("Detection Modifiers")]
    public float CrouchVisibilityMultiplier = 0.6f;
    public float MovementVisibilityMultiplier = 1.5f; // Player moving is easier to spot
    public float StationaryVisibilityMultiplier = 1.0f; // Baseline for not moving
    public float TallGrassConcealmentMultiplier = 0.4f;
    public float LanternRaisedVisibilityMultiplier = 2.0f;
    public float ShallowWaterConcealmentMultiplier = 0.7f;

    [Header("Investigating")]
    public float InvestigationLookSweepDuration = 2.0f;
    public float InvestigationLookPauseDuration = 1.0f;
    public int InvestigationMaxLookSweeps = 2;
    public float InvestigationScanAlertnessMultiplier = 1.5f;

    [Header("Suppressing Fire")]
    public int SuppressingMaxShots = 3;
    public float SuppressingTimeBetweenShots = 1.0f;
    public float SuppressingStateDuration = 5.0f;
    public float SuppressingSpreadRadius = 1.0f;

    [Header("Advanced Aiming System")]
    public float AimCatchUpSpeed = 2.0f; // How quickly the gun tries to catch up to the target point.
    public float MaxAimSwayAngle = 1.5f; // Max random sway in degrees from the "perfect" aim.
    public float AimSwaySpeed = 1.0f;      // How quickly the sway oscillates.
    public float WeaponSpreadAngle = 2.5f;
    public float TimeToMaxConfidence = 1.5f; // Time needed on target for max confidence.
    public float ShotConfidenceThreshold = 0.75f; // (0 to 1) Min confidence to take a shot (unless patience runs out)
    public float MinAngleForShotConfidence = 5.0f; // How close gunDir must be to targetDir to gain confidence (degrees)
    public float BodyTurnSpeedInAim = 10f; // How fast the Hunter's body orients while aiming
    private Vector3 actualFiringDirection;

    [Header("Timers")]
    public float AimTime = 2.0f;
    public float ReloadTime = 3.0f;
    public float InvestigationDuration = 8.0f;
    public float SuperpositionAttemptCooldown = 10.0f;

    [Header("Gameplay Rules")]
    public float WaterSurfaceYLevel = 0.5f;

    [Header("References")]
    public Transform PlayerTransform;
    public Transform GunMuzzleTransform;
    public Transform EyeLevelTransform;

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
    public bool IsActivelyScanning { get; set; } = false;

    public bool CanHearPlayerAlert { get; private set; }
    public float CurrentInvestigationTimer { get; set; }
    public float AimAttemptCooldownTimer { get; private set; }
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
        if (CurrentSuperpositionCooldownTimer > 0) CurrentSuperpositionCooldownTimer -= Time.deltaTime;
        if (AimAttemptCooldownTimer > 0) AimAttemptCooldownTimer -= Time.deltaTime;

        if (PlayerTransform == null || TargetPlayerStatus == null)
        {
            // If player doesn't exist, decay detection
            if (DetectionProgress > 0)
            {
                DetectionProgress -= DetectionDecayRate * Time.deltaTime;
                DetectionProgress = Mathf.Clamp01(DetectionProgress);
                UpdateFullySpottedStatus();
            }
            return;
        }
        ProcessSensorsAndDetectionLogic();
    }

    void ProcessSensorsAndDetectionLogic()
    {
        // Exit early if we have no target
        if (PlayerTransform == null || TargetPlayerStatus == null)
        {
            if (DetectionProgress > 0)
            {
                DetectionProgress = Mathf.Clamp01(DetectionProgress - DetectionDecayRate * Time.deltaTime);
                UpdateFullySpottedStatus();
            }
            return;
        }

        int visiblePoints = 0;
        Vector3 directionToPlayerCenter = (PlayerTransform.position - EyeLevelTransform.position).normalized;
        float distanceToPlayer = Vector3.Distance(EyeLevelTransform.position, PlayerTransform.position);

        // --- Broad Phase Check: Is the player even generally in the cone and range? ---
        // This is a cheap check to see if we should bother with expensive raycasts.
        if (distanceToPlayer <= VisionConeRange &&
            Vector3.Angle(EyeLevelTransform.forward, directionToPlayerCenter) <= VisionConeAngle / 2f)
        {
            // --- Detailed Volumetric LoS Check ---
            Transform[] playerVisibilityPoints = TargetPlayerStatus.GetVisibilityPoints();
            int hunterLayer = LayerMask.NameToLayer("Hunter");
            LayerMask ignoreHunterMask = ~(1 << hunterLayer);

            foreach (var point in playerVisibilityPoints)
            {
                // 1. Is the specific point submerged? If so, the hunter can't see it.
                if (TargetPlayerStatus.IsSubmerged(point.position))
                {
                    continue; // Skip to the next point
                }

                // 2. Is there a clear line of sight to this non-submerged point?
                Vector3 directionToPoint = point.position - EyeLevelTransform.position;
                float distanceToPoint = directionToPoint.magnitude; // Use distance to the specific point

                RaycastHit hit;
                // Raycast only up to the distance of the point itself.
                if (Physics.Raycast(EyeLevelTransform.position, directionToPoint.normalized, out hit, distanceToPoint, ignoreHunterMask, QueryTriggerInteraction.Ignore))
                {
                    // We hit something. If it's NOT the player, the point is blocked.
                    if (!hit.transform.IsChildOf(PlayerTransform) && hit.transform != PlayerTransform)
                    {
                        // Blocked by an obstacle, this point is not visible.
                        continue;
                    }
                }

                // If we reach here, either the raycast hit the player, or it hit nothing on its way to the point,
                // which means the path is clear.
                visiblePoints++;
            }
        }

        // --- Update Detection Progress based on how many points were visible ---
        if (visiblePoints > 0)
        {
            LastKnownPlayerPosition = PlayerTransform.position;

            // The "visibility score" (0 to 1) based on how much of the player is visible
            float visibilityScore = (float)visiblePoints / (float)TargetPlayerStatus.GetVisibilityPoints().Length;
            float currentRate = BaseDetectionRate * visibilityScore;

            // --- Apply all concealment/visibility multipliers ---
            currentRate *= TargetPlayerStatus.IsCrouching ? CrouchVisibilityMultiplier : 1.0f;
            currentRate *= TargetPlayerStatus.IsMoving ? MovementVisibilityMultiplier : StationaryVisibilityMultiplier;
            if (TargetPlayerStatus.IsInTallGrass) currentRate *= TallGrassConcealmentMultiplier;
            if (TargetPlayerStatus.IsLanternRaised) currentRate *= LanternRaisedVisibilityMultiplier;

            if (TargetPlayerStatus.CurrentWaterZone != null)
            {
                currentRate *= ShallowWaterConcealmentMultiplier;
            }

            if (IsActivelyScanning)
            {
                currentRate *= InvestigationScanAlertnessMultiplier;
            }

            DetectionProgress += currentRate * Time.deltaTime;
        }
        else
        {
            // No points are visible, decay detection
            DetectionProgress -= DetectionDecayRate * Time.deltaTime;
        }

        DetectionProgress = Mathf.Clamp01(DetectionProgress);
        UpdateFullySpottedStatus();
    }

    private void UpdateFullySpottedStatus()
    {
        if (DetectionProgress >= 1.0f)
        {
            if (!IsPlayerFullySpotted) // Became fully spotted THIS frame
            {
                IsPlayerFullySpotted = true;
                Debug.Log($"{gameObject.name} Player FULLY SPOTTED! Transitioning to Chasing.");
                // Consider playing the SpottedPlayerSound here or upon entering ChasingState.
                // HunterEventBus.HunterSpottedPlayer(PlayerTransform.gameObject); // This event implies "now fully spotted"
            }
        }
        else if (DetectionProgress < FullySpottedLossThreshold) // Check if progress drops below a certain point
        {
            if (IsPlayerFullySpotted) // Lost full spotting THIS frame
            {
                IsPlayerFullySpotted = false;
                Debug.Log($"{gameObject.name} Player no longer fully spotted.");
            }
        }
    }

    /// <summary>
    /// Checks if the player has a direct line of sight to the Hunter.
    /// This is used to prevent superposition while being observed.
    /// </summary>
    /// <returns>True if the Hunter is visible to the player's camera, false otherwise.</returns>
    public bool IsVisibleToPlayer()
    {
        // No player or camera means we can't be seen.
        if (PlayerTransform == null) return false;
        Camera playerCamera = Camera.main;
        if (playerCamera == null) return false;

        Vector3 hunterCenter = transform.position + Vector3.up * 1.0f; // A point in the center of the Hunter
        Vector3 directionFromPlayer = (hunterCenter - playerCamera.transform.position).normalized;
        float distanceToHunter = Vector3.Distance(playerCamera.transform.position, hunterCenter);

        // A mask that includes obstacles but ignores the Hunter itself (so the ray can reach it).
        LayerMask obstacleMask = ~(1 << gameObject.layer);

        RaycastHit hit;
        // If a raycast from the player's camera hits an obstacle before it hits the hunter...
        if (Physics.Raycast(playerCamera.transform.position, directionFromPlayer, out hit, distanceToHunter, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            // We hit something. If it's NOT us, then we are hidden.
            if (hit.transform.root != transform.root)
            {
                // Path is blocked, Hunter is NOT visible.
                return false;
            }
        }

        // If the raycast either hit nothing (clear path) or hit the Hunter, then the Hunter IS visible.
        return true;
    }

    public void TriggerAimAttemptCooldown(float duration)
    {
        AimAttemptCooldownTimer = duration;
    }

    public Vector3 GetPlayerAimPoint()
    {
        if (PlayerTransform == null || TargetPlayerStatus == null || TargetPlayerStatus.TorsoVisibilityPoint == null)
        {
            // Fallback to the player's base position if references are missing.
            return PlayerTransform != null ? PlayerTransform.position : Vector3.zero;
        }

        // The new, simplified logic. We just ask for the torso's current position.
        // PlayerStatus and PlayerMovement handle whether it's the standing or crouching position.
        return TargetPlayerStatus.TorsoVisibilityPoint.position;
    }

    public void SetActualFiringDirection(Vector3 direction)
    {
        actualFiringDirection = direction.normalized;
    }

    // Auditory detection might give a direct boost to DetectionProgress
    private void HandlePlayerShoutEvent(Vector3 shoutPosition)
    {
        if (this == null || !enabled || !gameObject.activeInHierarchy) return;

        if (Vector3.Distance(transform.position, shoutPosition) <= AuditoryDetectionRange)
        {
            CanHearPlayerAlert = true; // Still useful for investigating state
            LastKnownPlayerPosition = shoutPosition;
            PlaySound(HeardNoiseSound);

            // Add a boost to detection based on noise
            float noiseDetectionBoost = 0.3f; // Example
            DetectionProgress = Mathf.Clamp01(DetectionProgress + noiseDetectionBoost);
            UpdateFullySpottedStatus(); // Re-evaluate if this shout made them fully spotted
            Debug.Log($"{gameObject.name} heard player shout. LKP updated. Detection boosted to {DetectionProgress}. CanHearPlayerAlert = true");
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
            Instantiate(MuzzleFlashPrefab, GunMuzzleTransform.position, Quaternion.LookRotation(actualFiringDirection), GunMuzzleTransform);
        }

        if (PlayerTransform == null || GunMuzzleTransform == null) return;

        // --- Apply Weapon Spread ---
        Quaternion spreadRotation = Quaternion.Euler(
            Random.Range(-WeaponSpreadAngle / 2f, WeaponSpreadAngle / 2f),
            Random.Range(-WeaponSpreadAngle / 2f, WeaponSpreadAngle / 2f),
            0f
        );
        Vector3 finalShotDirection = spreadRotation * actualFiringDirection; // actualFiringDirection should be world space
                                                                             // If actualFiringDirection was relative to hunter's transform, it'd be:
                                                                             // finalShotDirection = GunMuzzleTransform.rotation * spreadRotation * (Quaternion.LookRotation(actualFiringDirection) * Vector3.forward);


        // --- Submergence Check (for the PLAYER'S general position, not the exact aim point) ---
        // We are shooting in a general direction. The main concern for water is if the *player* is mostly submerged.
        // Let's use the player's *base* position or a primary visibility point for a quick submergence check.
        Vector3 playerCheckPosForSubmergence = GetPlayerAimPoint();
        if (TargetPlayerStatus != null && TargetPlayerStatus.IsSubmerged(playerCheckPosForSubmergence))
        {
            Debug.Log($"{gameObject.name} SHOT FIRED towards generally submerged player area. Impacting water near player.");
            // Logic to spawn water impact near player's surface position
            if (BulletImpactWaterPrefab != null)
            {
                Plane waterPlane = new Plane(Vector3.up, new Vector3(0, WaterSurfaceYLevel, 0));
                Ray waterImpactRay = new Ray(GunMuzzleTransform.position, finalShotDirection); // Use the spread direction
                if (waterPlane.Raycast(waterImpactRay, out float enterDist))
                {
                    if (enterDist <= ShootingRange * 1.2f)
                    {
                        Instantiate(BulletImpactWaterPrefab, waterImpactRay.GetPoint(enterDist), Quaternion.LookRotation(waterPlane.normal));
                    }
                }
            }
            return;
        }

        // --- Raycast with the final spread direction ---
        float shotDistance = ShootingRange * 1.2f;
        RaycastHit hit;
        int hunterLayer = LayerMask.NameToLayer("Hunter");
        LayerMask shootableMask = ~(1 << hunterLayer);

        Debug.DrawRay(GunMuzzleTransform.position, finalShotDirection * shotDistance, Color.red, 2.0f); // Visualize actual shot

        // Raycast
        if (Physics.Raycast(GunMuzzleTransform.position, finalShotDirection, out hit, shotDistance, shootableMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(PlayerTransform) || hit.collider.transform == PlayerTransform)
            {
                // Check if the *actual hit point on the player* is submerged
                if (TargetPlayerStatus != null && TargetPlayerStatus.IsSubmerged(hit.point))
                {
                    Debug.Log($"{gameObject.name} SHOT HIT Player's submerged part at {hit.point}. Impacting water.");
                    if (BulletImpactWaterPrefab != null) Instantiate(BulletImpactWaterPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                }
                else
                {
                    Debug.Log($"{gameObject.name} HIT Player: {hit.collider.name} at {hit.point}");
                    PlayerHealth playerHealth = hit.collider.GetComponentInParent<PlayerHealth>(); // GetComponentInParent is safer if colliders are on children
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
        if (PlayerTransform == null)
        {
            Debug.LogWarning($"{gameObject.name}: IsPathClear - PlayerTransform is null.");
            return false;
        }

        if (TargetPlayerStatus != null && TargetPlayerStatus.IsSubmerged(aimPoint))
        {
            Debug.LogWarning($"{gameObject.name}: IsPathClear - Aim point ({aimPoint}) is SUBMERGED. Path NOT clear.");
            return false;
        }

        if (GunMuzzleTransform == null)
        {
            Debug.LogWarning($"{gameObject.name}: IsPathClear - GunMuzzleTransform is null.");
            return false;
        }

        Vector3 directionToAimPoint = (aimPoint - GunMuzzleTransform.position).normalized;
        float distanceToAimPoint = Vector3.Distance(GunMuzzleTransform.position, aimPoint);

        if (distanceToAimPoint < 0.1f) return true; // Already on top of the target

        int hunterLayer = LayerMask.NameToLayer("Hunter");
        LayerMask shootableMask = ~(1 << hunterLayer);
        RaycastHit hit;

        Debug.DrawRay(GunMuzzleTransform.position, directionToAimPoint * distanceToAimPoint, Color.cyan, 1.0f);

        if (Physics.Raycast(GunMuzzleTransform.position, directionToAimPoint, out hit, distanceToAimPoint, shootableMask, QueryTriggerInteraction.Ignore))
        {
            // We hit something. If it's NOT the player, the path is blocked.
            if (!hit.transform.IsChildOf(PlayerTransform) && hit.transform != PlayerTransform)
            {
                Debug.LogWarning($"{gameObject.name}: IsPathClear - Path to player for shot BLOCKED by OBSTACLE: {hit.collider.name} at {hit.point}");
                return false;
            }
        }

        // If we get here, either the ray hit the player, or it hit nothing (meaning the path is clear to the point)
        // Both are valid conditions for a clear shot path.
        return true;
    }

    void OnDrawGizmosSelected()
    {
        if (EyeLevelTransform == null) return;

        // --- Vision Cone & Sensor Ranges ---
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(EyeLevelTransform.position, VisionConeRange);
        Vector3 fovLine1 = Quaternion.AngleAxis(VisionConeAngle / 2, EyeLevelTransform.up) * EyeLevelTransform.forward * VisionConeRange;
        Vector3 fovLine2 = Quaternion.AngleAxis(-VisionConeAngle / 2, EyeLevelTransform.up) * EyeLevelTransform.forward * VisionConeRange;
        Gizmos.DrawLine(EyeLevelTransform.position, EyeLevelTransform.position + fovLine1);
        Gizmos.DrawLine(EyeLevelTransform.position, EyeLevelTransform.position + fovLine2);

        // --- Volumetric LoS Debugging ---
        if (PlayerTransform != null && TargetPlayerStatus != null)
        {
            Transform[] points = TargetPlayerStatus.GetVisibilityPoints();
            if (points == null || points.Length == 0) return;

            int hunterLayer = LayerMask.NameToLayer("Hunter");
            LayerMask ignoreHunterMask = ~(1 << hunterLayer);

            foreach (var point in points)
            {
                if (point == null) continue;

                // Always draw a small sphere at the point's location
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(point.position, 0.1f);

                // Only draw lines if in play mode, as TargetPlayerStatus might not be fully ready in editor
                if (!Application.isPlaying) continue;

                // Check 1: Is the point submerged?
                if (TargetPlayerStatus.IsSubmerged(point.position))
                {
                    Gizmos.color = Color.blue; // BLUE for SUBMERGED
                    Gizmos.DrawLine(EyeLevelTransform.position, point.position);
                    continue; // Don't do further checks
                }

                // Check 2: Is there a clear line of sight?
                Vector3 directionToPoint = point.position - EyeLevelTransform.position;
                float distanceToPoint = directionToPoint.magnitude;

                // Perform the same raycast as the detection logic
                RaycastHit hit;
                if (Physics.Raycast(EyeLevelTransform.position, directionToPoint.normalized, out hit, distanceToPoint, ignoreHunterMask, QueryTriggerInteraction.Ignore))
                {
                    // We hit something. If it's NOT the player, it's an obstacle.
                    if (!hit.transform.IsChildOf(PlayerTransform) && hit.transform != PlayerTransform)
                    {
                        Gizmos.color = Color.red; // RED for BLOCKED
                        Gizmos.DrawLine(EyeLevelTransform.position, hit.point); // Draw line to the hit point
                        Gizmos.DrawSphere(hit.point, 0.15f); // Mark where it was blocked
                    }
                    else
                    {
                        Gizmos.color = Color.green; // GREEN for VISIBLE (hit the player)
                        Gizmos.DrawLine(EyeLevelTransform.position, point.position);
                    }
                }
                else
                {
                    // Raycast hit nothing, meaning clear path to the point
                    Gizmos.color = Color.green; // GREEN for VISIBLE
                    Gizmos.DrawLine(EyeLevelTransform.position, point.position);
                }
            }
        }


        // --- Detection Progress Bar ---
        if (Application.isPlaying)
        {
            float barWidth = 1f;
            float barHeight = 0.1f;
            Vector3 barPosition = transform.position + Vector3.up * 2.5f;

            Gizmos.color = Color.grey;
            Gizmos.DrawCube(barPosition, new Vector3(barWidth, barHeight, 0.01f));

            Gizmos.color = Color.Lerp(Color.green, Color.red, DetectionProgress);
            float progressWidth = barWidth * DetectionProgress;
            Vector3 progressPosition = barPosition - Vector3.right * (barWidth / 2f) + Vector3.right * (progressWidth / 2f);
            Gizmos.DrawCube(progressPosition, new Vector3(progressWidth, barHeight, 0.01f));
        }

        // --- Other Gizmos (can remain mostly the same) ---
        Gizmos.color = Color.cyan; // Changed from blue to avoid confusion with submerged LoS lines
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
            if (TargetPlayerStatus != null && TargetPlayerStatus.IsSubmerged(currentAimGizmoPoint))
            {
                Gizmos.color = Color.blue; // Dark blue if aim point submerged
                Gizmos.DrawSphere(currentAimGizmoPoint, 0.25f);
                // Use the WaterZone's surface level if available for a more accurate gizmo
                float surfaceY = TargetPlayerStatus.CurrentWaterZone != null ? TargetPlayerStatus.CurrentWaterZone.SurfaceYLevel : WaterSurfaceYLevel;
                Gizmos.DrawLine(currentAimGizmoPoint, new Vector3(currentAimGizmoPoint.x, surfaceY, currentAimGizmoPoint.z)); // Line to surface
            }
            else
            {
                Gizmos.color = Color.yellow; // Yellow if aim point above water
                Gizmos.DrawSphere(currentAimGizmoPoint, 0.25f);
            }
        }
    }
}