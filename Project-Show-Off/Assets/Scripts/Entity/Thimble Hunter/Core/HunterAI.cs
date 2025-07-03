using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[System.Serializable]
public struct GizmoSettings
{
    [Header("Sensor Ranges")]
    public bool ShowVisionCone;
    public bool ShowAuditoryRange;
    public bool ShowShootingRange;
    public bool ShowSuperpositionRange;

    [Header("Dynamic State & Debugging")]
    public bool ShowVolumetricLoSLines;
    public bool ShowDetectionProgressBar;
    public bool ShowLastKnownPlayerPosition;
    public bool ShowCurrentAimTarget;
    public bool ShowWaterLevelAndSubmergence;
}

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(HunterStateMachine))]
public class HunterAI : MonoBehaviour
{
    [Header("Core Attributes")]
    public float MaxSuperpositionDistance = 50f;
    public float VisionConeAngle = 140f;
    public float VisionConeRange = 30f;
    public float AuditoryDetectionRange = 20f;
    public float ShootingRange = 15f;
    public int GunDamage = 100;

    [Header("Movement Speeds")]
    public float MovementSpeedRoaming = 2f;
    public float MovementSpeedInvestigating = 3f;
    public float MovementSpeedChasing = 4.5f;

    [Header("Detection System")]
    public float BaseDetectionRate = 0.5f; // Units: progress/second (0 to 1)
    public float DetectionDecayRate = 0.15f; // Units: progress/second
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
    public float TimeBetweenShots = 2.5f;
    public float ReloadTime = 3.0f;
    public float InvestigationDuration = 8.0f;
    public float SuperpositionAttemptCooldown = 10.0f;

    [Header("Gameplay Rules")]
    public float WaterSurfaceYLevel = 0.5f;

    [Header("References")]
    public Transform PlayerTransform;
    public Transform GunMuzzleTransform;
    public Transform EyeLevelTransform;

    [Header("Gizmo Display Settings")]
    public GizmoSettings GizmoToggles;

    [Header("VFX (Assign in Inspector)")]
    public GameObject MuzzleFlashPrefab;
    public GameObject BulletImpactPlayerPrefab;
    public GameObject BulletImpactObstaclePrefab;
    public GameObject BulletImpactWaterPrefab;

    // --- Component References (public properties for states to access) ---
    public NavMeshAgent NavAgent { get; private set; }
    public HunterNavigation Navigation { get; private set; }
    public Animator HunterAnimator { get; private set; }
    public PlayerStatus TargetPlayerStatus { get; private set; }

    // --- Runtime AI Data (public properties for states to access) ---
    private float shotCooldownTimer;
    public bool IsShotOnCooldown => shotCooldownTimer > 0;
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
    public HunterSoundController SoundController { get; private set; }

    void Awake()
    {
        NavAgent = GetComponent<NavMeshAgent>();
        HunterAnimator = GetComponent<Animator>();

        SoundController = GetComponent<HunterSoundController>();
        if (SoundController == null)
        {
            Debug.LogError("HunterAI is missing a HunterSoundController component!", this.gameObject);
        }

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
        // Update timers and other non-transform logic
        if (shotCooldownTimer > 0) shotCooldownTimer -= Time.deltaTime;
        if (CurrentSuperpositionCooldownTimer > 0) CurrentSuperpositionCooldownTimer -= Time.deltaTime;
        if (AimAttemptCooldownTimer > 0) AimAttemptCooldownTimer -= Time.deltaTime;

        if (PlayerTransform == null || TargetPlayerStatus == null)
        {
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

    private void HandlePlayerShoutEvent(Vector3 shoutPosition)
    {
        if (this == null || !enabled || !gameObject.activeInHierarchy) return;

        if (Vector3.Distance(transform.position, shoutPosition) <= AuditoryDetectionRange)
        {
            CanHearPlayerAlert = true; // Still useful for investigating state
            LastKnownPlayerPosition = shoutPosition;

            // Add a boost to detection based on noise
            float noiseDetectionBoost = 0.3f;
            DetectionProgress = Mathf.Clamp01(DetectionProgress + noiseDetectionBoost);
            UpdateFullySpottedStatus();

            Debug.Log($"{gameObject.name} heard player shout. LKP updated. Detection boosted to {DetectionProgress}. CanHearPlayerAlert = true");
        }
    }

    public void AcknowledgePlayerAlert()
    {
        CanHearPlayerAlert = false;
        Debug.Log($"{gameObject.name} acknowledged player alert. CanHearPlayerAlert = false");
    }

    public void FireGun()
    {
        Debug.Log($"{gameObject.name}: Animation trigger 'Shoot' has been set.");

        HunterAnimator.SetTrigger("Shoot");
        HunterEventBus.HunterFiredShot();
    }

    public void HandleShotEventFromAnimation()
    {
        shotCooldownTimer = TimeBetweenShots;

        Debug.Log($"{gameObject.name}: BANG! (Cooldown started: {TimeBetweenShots}s)");

        // 1. Play the Sound
        if (SoundController != null)
        {
            SoundController.PlayGunFireSound();
        }

        // 2. Spawn Muzzle Flash VFX
        if (GunMuzzleTransform != null)
        {
            // 1. Instantiate Muzzle Flash Prefab
            if (MuzzleFlashPrefab != null)
            {
                // Instantiate the prefab at the muzzle's position and rotation, parented to the muzzle
                Instantiate(MuzzleFlashPrefab, GunMuzzleTransform.position, GunMuzzleTransform.rotation, GunMuzzleTransform);
            }

            // 2. Start the Light Flash Coroutine
            StartCoroutine(MuzzleFlashLightRoutine());
        }

        // 3. Perform the Raycast and Damage Logic (Moved from the old FireGun method)
        if (PlayerTransform == null || GunMuzzleTransform == null) return;

        // --- Apply Weapon Spread ---
        Quaternion spreadRotation = Quaternion.Euler(
            Random.Range(-WeaponSpreadAngle / 2f, WeaponSpreadAngle / 2f),
            Random.Range(-WeaponSpreadAngle / 2f, WeaponSpreadAngle / 2f),
            0f
        );
        Vector3 finalShotDirection = spreadRotation * actualFiringDirection;

        // --- Submergence Check (for the PLAYER'S general position, not the exact aim point) ---
        Vector3 playerCheckPosForSubmergence = GetPlayerAimPoint();
        if (TargetPlayerStatus != null && TargetPlayerStatus.IsSubmerged(playerCheckPosForSubmergence))
        {
            Debug.Log($"{gameObject.name} SHOT FIRED towards generally submerged player area. Impacting water near player.");

            if (BulletImpactWaterPrefab != null)
            {
                Plane waterPlane = new Plane(Vector3.up, new Vector3(0, WaterSurfaceYLevel, 0));
                Ray waterImpactRay = new Ray(GunMuzzleTransform.position, finalShotDirection);

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
        int hunterLayer = LayerMask.NameToLayer("Hunter");
        LayerMask shootableMask = ~(1 << hunterLayer);

        Debug.DrawRay(GunMuzzleTransform.position, finalShotDirection * shotDistance, Color.red, 2.0f);

        // Raycast
        if (Physics.Raycast(GunMuzzleTransform.position, finalShotDirection, out RaycastHit hit, shotDistance, shootableMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(PlayerTransform) || hit.collider.transform == PlayerTransform)
            {
                Debug.Log($"{gameObject.name} HIT Player: {hit.collider.name} at {hit.point}");

                // Check if the *actual hit point on the player* is submerged
                if (TargetPlayerStatus != null && TargetPlayerStatus.IsSubmerged(hit.point))
                {
                    Debug.Log($"{gameObject.name} SHOT HIT Player's submerged part at {hit.point}. Impacting water.");

                    if (BulletImpactWaterPrefab != null) Instantiate(BulletImpactWaterPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                }
                else
                {
                    Debug.Log($"{gameObject.name} HIT Player: {hit.collider.name} at {hit.point}");

                    PlayerHealth playerHealth = hit.collider.GetComponentInParent<PlayerHealth>();

                    if (playerHealth != null)
                    {
                        playerHealth.RegisterShot();
                    }
                    if (BulletImpactPlayerPrefab != null) Instantiate(BulletImpactPlayerPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                }
            }
        }
        else
        {
            Debug.Log($"{gameObject.name} SHOT missed (hit nothing within range).");
        }
    }

    /// <summary>
    /// Creates a bright light at the muzzle for a split second and then destroys it.
    /// </summary>
    private System.Collections.IEnumerator MuzzleFlashLightRoutine()
    {
        // Create a new empty GameObject to hold our light
        GameObject lightGO = new GameObject("MuzzleFlashLight");
        lightGO.transform.position = GunMuzzleTransform.position;

        // Add a Light component to the new GameObject
        Light lightComp = lightGO.AddComponent<Light>();

        // Configure the light to be a bright, short-range flash
        lightComp.color = Color.yellow;
        lightComp.intensity = 8f;   // Very bright
        lightComp.range = 25f;      // Affects a good area
        lightComp.shadows = LightShadows.None; // Performance: no shadows needed for a quick flash
        lightComp.bounceIntensity = 0;

        // Wait for a fraction of a second
        yield return new WaitForSeconds(0.06f);

        // Destroy the temporary light GameObject
        Destroy(lightGO);
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

    void OnDrawGizmos()
    {
        if (GizmoToggles.ShowVisionCone && EyeLevelTransform != null)
        {
            Gizmos.color = Color.yellow;
            // Use Handles for a filled cone for better visibility
#if UNITY_EDITOR
            UnityEditor.Handles.color = new Color(1, 1, 0, 0.1f);
            UnityEditor.Handles.DrawSolidArc(EyeLevelTransform.position, EyeLevelTransform.up, Quaternion.AngleAxis(-VisionConeAngle / 2, EyeLevelTransform.up) * EyeLevelTransform.forward, VisionConeAngle, VisionConeRange);
#endif
            Gizmos.DrawWireSphere(EyeLevelTransform.position, VisionConeRange);
        }

        if (GizmoToggles.ShowVolumetricLoSLines)
        {
            if (EyeLevelTransform != null && PlayerTransform != null && TargetPlayerStatus != null)
            {
                Transform[] points = TargetPlayerStatus.GetVisibilityPoints();
                if (points == null || points.Length == 0) return;

                int hunterLayer = LayerMask.NameToLayer("Hunter");
                LayerMask ignoreHunterMask = ~(1 << hunterLayer);

                foreach (var point in points)
                {
                    if (point == null) continue;
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawSphere(point.position, 0.1f);

                    if (!Application.isPlaying) continue;
                    if (TargetPlayerStatus.IsSubmerged(point.position))
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawLine(EyeLevelTransform.position, point.position);
                        continue;
                    }
                    Vector3 directionToPoint = point.position - EyeLevelTransform.position;
                    float distanceToPoint = directionToPoint.magnitude;
                    RaycastHit hit;
                    if (Physics.Raycast(EyeLevelTransform.position, directionToPoint.normalized, out hit, distanceToPoint, ignoreHunterMask, QueryTriggerInteraction.Ignore))
                    {
                        if (!hit.transform.IsChildOf(PlayerTransform) && hit.transform != PlayerTransform)
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawLine(EyeLevelTransform.position, hit.point);
                            Gizmos.DrawSphere(hit.point, 0.15f);
                        }
                        else
                        {
                            Gizmos.color = Color.green;
                            Gizmos.DrawLine(EyeLevelTransform.position, point.position);
                        }
                    }
                    else
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(EyeLevelTransform.position, point.position);
                    }
                }
            }
        }

        if (GizmoToggles.ShowDetectionProgressBar && Application.isPlaying)
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

        if (GizmoToggles.ShowAuditoryRange)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, AuditoryDetectionRange);
        }
        if (GizmoToggles.ShowShootingRange)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, ShootingRange);
        }
        if (GizmoToggles.ShowSuperpositionRange)
        {
            Gizmos.color = new Color(0.8f, 0.5f, 0.2f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, MaxSuperpositionDistance);
        }
        if (GizmoToggles.ShowLastKnownPlayerPosition && LastKnownPlayerPosition != Vector3.zero)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(LastKnownPlayerPosition, 0.5f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(LastKnownPlayerPosition + Vector3.up, "Last Known Position");
#endif
        }

        if (GizmoToggles.ShowWaterLevelAndSubmergence && PlayerTransform != null)
        {
            Vector3 playerBase = PlayerTransform.position;
            float lineLength = 5f;
            Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.4f);
            Vector3 waterLineStart = new Vector3(playerBase.x, WaterSurfaceYLevel, playerBase.z - lineLength / 2);
            Vector3 waterLineEnd = new Vector3(playerBase.x, WaterSurfaceYLevel, playerBase.z + lineLength / 2);
            Gizmos.DrawLine(waterLineStart, waterLineEnd);
            waterLineStart = new Vector3(playerBase.x - lineLength / 2, WaterSurfaceYLevel, playerBase.z);
            waterLineEnd = new Vector3(playerBase.x + lineLength / 2, WaterSurfaceYLevel, playerBase.z);
            Gizmos.DrawLine(waterLineStart, waterLineEnd);

            Vector3 currentAimGizmoPoint = GetPlayerAimPoint();
            if (Application.isPlaying && TargetPlayerStatus != null && TargetPlayerStatus.IsSubmerged(currentAimGizmoPoint))
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(currentAimGizmoPoint, 0.25f);
                float surfaceY = TargetPlayerStatus.CurrentWaterZone != null ? TargetPlayerStatus.CurrentWaterZone.SurfaceYLevel : WaterSurfaceYLevel;
                Gizmos.DrawLine(currentAimGizmoPoint, new Vector3(currentAimGizmoPoint.x, surfaceY, currentAimGizmoPoint.z));
            }
            else
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(currentAimGizmoPoint, 0.25f);
            }
        }

        if (Application.isPlaying && HunterAnimator != null && HunterAnimator.GetBool("IsAiming"))
        {
            // 1. Show the Current Confirmed Aim Target
            if (GizmoToggles.ShowCurrentAimTarget && CurrentConfirmedAimTarget != Vector3.zero)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(CurrentConfirmedAimTarget, 0.2f);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(CurrentConfirmedAimTarget, "Aim Target");
#endif

                if (GunMuzzleTransform != null)
                {
                    // 2. Draw the ACTUAL Firing Direction (red line)
                    // This includes sway and is the most important for debugging.
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(GunMuzzleTransform.position, actualFiringDirection * VisionConeRange);
                }
            }
        }
    }
}