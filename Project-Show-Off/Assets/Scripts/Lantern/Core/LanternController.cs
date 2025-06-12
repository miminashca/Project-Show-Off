using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.VFX;
using FMODUnity;
using FMOD.Studio;

public class LanternController : MonoBehaviour
{
    [Header("Lantern Setup")]
    public GameObject lanternPrefab;
    public Transform lanternHandAnchor;

    private GameObject currentLanternInstance;
    private PhysicsLanternSway currentPhysicsSwayScript;
    private Light lanternLight;
    private LightFlicker lightFlicker;

    [Header("State")]
    public bool isEquipped = false;
    public bool isRaised = false;
    private bool outOfFuel = false;
    public bool IsLightOn { get; private set; }

    [Header("Light Settings")]
    public float defaultIntensity = 1.5f;
    public float raisedIntensity = 4.0f;
    public float defaultRange = 10f;
    public float raisedRange = 15f;
    public Color lightColor = Color.yellow;

    [Header("Fuel System")]
    public float maxFuel = 100f;
    public float currentFuel;
    public float passiveDrainRate = 0.1f;
    public float activeDrainRate = 1.0f;

    [Header("Interaction")]
    public float hemannekenRepelRadius = 7f;
    public LayerMask hemannekenLayer;
    public float interactionCheckInterval = 0.25f;

    [Header("Raise Animation")]
    public Vector3 raisedLocalPositionOffset = new Vector3(0, 0.2f, 0.05f);

    [Header("FMOD Sounds")]
    [SerializeField]
    private EventReference lanternPullOutSoundEvent;
    [SerializeField]
    private EventReference lanternPutAwaySoundEvent;
    [SerializeField]
    private EventReference lanternGasBurnLoopEvent; // Event for the looping gas burn sound

    private EventInstance gasBurnSoundInstance; // Instance for the looping sound

    [Header("VFX Settings")]
    [Tooltip("Name of the exposed Vector2 property in the VFX Graph for flame size X (min) and Y (max).")]
    public string flameSizeRangePropertyName = "Flame_SizeRange";
    public Vector2 defaultFlameSize = new Vector2(0.1f, 0.2f);
    public Vector2 raisedFlameSize = new Vector2(0.2f, 0.4f); // Make sure this is noticeably larger

    private GameObject currentLanternVFXHolder;
    private VisualEffect lanternVFXGraph;
    private Coroutine interactionCoroutine;

    private PlayerInput playerInputActions;
    private PlayerStatus playerStatus;

    private HingeLimitStabilizer hinge;

    private void Awake()
    {
        if (playerInputActions == null)
        {
            playerInputActions = new PlayerInput();
        }

        playerStatus = GetComponentInParent<PlayerStatus>();
        if (playerStatus == null) Debug.LogError("LanternController needs PlayerStatus component on player object hierarchy!");

        if (lanternHandAnchor == null)
        {
            Debug.LogError("LanternController: lanternHandAnchor is not assigned!", this);
        }

        hinge = GetComponentInChildren<HingeLimitStabilizer>(true);
    }

    void Start()
    {
        if (lanternPrefab == null || lanternHandAnchor == null)
        {
            Debug.LogError("LanternController: Missing Lantern PREFAB or LanternHandPosition reference!");
            enabled = false;
            return;
        }
        currentFuel = maxFuel;
    }

    void Update()
    {
        HandleInput();

        if (isEquipped && !outOfFuel)
        {
            DrainFuel(Time.deltaTime);
        }
        else if (outOfFuel && isEquipped)
        {
            if (lanternLight != null && lanternLight.enabled) SetLightState(false);
            // VFX is handled by OutOfFuel() or ToggleEquip()
        }
    }

    void OnEnable()
    {
        if (playerInputActions == null)
        {
            playerInputActions = new PlayerInput();
        }
        playerInputActions.Player.Enable();
    }

    void OnDisable()
    {
        if (playerInputActions != null)
        {
            playerInputActions.Player.Disable();
        }

        // NEW CHANGE
        StartGasBurnLoopSFX();
        // END CHANGE

        if (isEquipped)
        {
            ToggleEquip(); // This will handle unequipping logic including VFX
        }
    }

    // NEW CHANGE
    private void OnDestroy()
    {
        StartGasBurnLoopSFX(); // Stop sound if object is destroyed
        // If currentLanternInstance exists and is parented to this, it will also be destroyed.
        // VFX objects are children of currentLanternInstance.
    }
    // END CHANGE

    void HandleInput()
    {
        if (playerInputActions == null) return;

        if (playerInputActions.Player.EquipLantern.WasPressedThisFrame())
        {
            ToggleEquip();
        }

        if (isEquipped && !outOfFuel)
        {
            if (playerInputActions.Player.RaiseLantern.WasPressedThisFrame())
            {
                Debug.Log("Pressed Raise Lantern!");
                StartRaising();
            }
            else if (playerInputActions.Player.RaiseLantern.WasReleasedThisFrame())
            {
                StopRaising();
            }
        }
        else if (isRaised && playerInputActions.Player.RaiseLantern.WasReleasedThisFrame()) // Handle release even if out of fuel, to lower it visually
        {
            StopRaising();
        }
    }

    void ToggleEquip()
    {
        isEquipped = !isEquipped;

        if (isEquipped)
        {
            if (currentLanternInstance == null)
            {
                currentLanternInstance = GetComponentInChildren<LanternParts>(true).gameObject;
                currentLanternInstance.transform.localRotation = Quaternion.identity;
                LanternParts parts = currentLanternInstance.GetComponent<LanternParts>();

                if (parts == null)
                {
                    Debug.LogError("LanternController: Lantern prefab is missing the LanternParts script!", currentLanternInstance);
                    isEquipped = false; // Revert equip status
                    if (currentLanternInstance != null) Destroy(currentLanternInstance);
                    currentLanternInstance = null;
                    return;
                }

                currentPhysicsSwayScript = parts.swayScript;

                // VFX Setup - Find VFX components within the newly instantiated lantern
                currentLanternVFXHolder = parts.lanternVFXHolder;
                if (currentLanternVFXHolder != null)
                {
                    lanternVFXGraph = currentLanternVFXHolder.GetComponentInChildren<VisualEffect>();
                    if (lanternVFXGraph == null)
                    {
                        Debug.LogError($"LanternController: Could not find VisualEffect component.");
                    }
                }
                else
                {
                    Debug.LogError($"LanternController: Could not find VFX Holder GameObject.");
                }


                if (currentPhysicsSwayScript != null)
                {
                    Camera mainCam = Camera.main;
                    if (mainCam == null) Debug.LogError("LanternController cannot find Player Camera for PhysicsLanternSway!");
                    if (parts.handleRigidbody == null) Debug.LogError("LanternParts on prefab has no Handle Rigidbody assigned.", parts);

                    currentPhysicsSwayScript.InitializeSway(
                        this.playerInputActions,
                        mainCam != null ? mainCam.transform : null,
                        lanternHandAnchor,
                        parts.handleRigidbody,
                        parts.swingingLanternBodyRB
                    );
                }
                else Debug.LogError("No PhysicsLanternSway script found on lantern prefab!");

                if (lanternLight == null) lanternLight = currentLanternInstance.GetComponentInChildren<Light>();
                if (lightFlicker == null && lanternLight != null) lightFlicker = lanternLight.GetComponent<LightFlicker>();

                if (lanternLight == null) Debug.LogError("LanternController: Could not find a Light component on the lantern prefab or its children!", currentLanternInstance);
            }


            if (hinge) hinge.ResetHinge();

            currentLanternInstance.SetActive(true); // Activate main lantern object
            isRaised = false; // Reset raised state on equip
            outOfFuel = (currentFuel <= 0);

            if (currentPhysicsSwayScript != null)
            {
                currentPhysicsSwayScript.SetTargetLocalOffsetImmediate(Vector3.zero); // Set initial sway position
                currentPhysicsSwayScript.targetLocalOffset = Vector3.zero;
            }


            if (!outOfFuel)
            {
                if (lanternLight != null) SetLightState(true, defaultIntensity, defaultRange);
                // Enable and configure VFX
                if (currentLanternVFXHolder != null)
                {
                    currentLanternVFXHolder.SetActive(true);
                }
                else
                {
                    Debug.Log("holder is null");
                }
                if (lanternVFXGraph != null)
                {
                    lanternVFXGraph.SetVector2(flameSizeRangePropertyName, defaultFlameSize);
                    lanternVFXGraph.Play(); // Explicitly play the VFX
                }
            }
            else // Equipping while out of fuel
            {
                if (lanternLight != null) SetLightState(false);
                // Keep VFX off if out of fuel
                if (lanternVFXGraph != null) lanternVFXGraph.Stop();
                if (currentLanternVFXHolder != null) currentLanternVFXHolder.SetActive(false);
            }
            Debug.Log("Lantern Equipped");

            // NEW CHANGE (FMOD)
            if (lanternPullOutSoundEvent.Guid != System.Guid.Empty)
            {
                RuntimeManager.PlayOneShot(lanternPullOutSoundEvent, transform.position);
            }
            if (!outOfFuel)
            {
                StartGasBurnLoop();
            }
            // END CHANGE

            if (playerStatus != null) playerStatus.IsLanternRaised = isRaised;
        }
        else // Unequipping
        {
            if (isRaised)
            {
                StopRaising(); // This will also reset VFX size if not out of fuel
            }
            else if (playerStatus != null)
            {
                playerStatus.IsLanternRaised = false; // Ensure status is updated if not raised but unequipped
            }


            if (lanternLight != null) SetLightState(false);

            // Disable VFX before deactivating the lantern instance
            if (lanternVFXGraph != null)
            {
                lanternVFXGraph.Stop();
            }
            if (currentLanternVFXHolder != null)
            {
                currentLanternVFXHolder.SetActive(false);
            }

            if (currentLanternInstance != null)
            {
                currentLanternInstance.SetActive(false); // Deactivate the lantern object
            }
            // Optionally Destroy(currentLanternInstance) if you don't want to pool/reuse it.
            // For now, SetActive(false) is fine. currentLanternInstance will be nulled on next equip if needed.

            Debug.Log("Lantern Unequipped");

            // NEW CHANGE (FMOD)
            if (lanternPutAwaySoundEvent.Guid != System.Guid.Empty)
            {
                RuntimeManager.PlayOneShot(lanternPutAwaySoundEvent, transform.position);
            }
            StartGasBurnLoopSFX();
            // END CHANGE
        }

        UpdatePlayerStatus();
    }

    void StartRaising()
    {
        if (!isEquipped || isRaised || outOfFuel) return;

        isRaised = true;
        if (playerStatus != null) playerStatus.IsLanternRaised = true;
        SetLightState(true, raisedIntensity, raisedRange);
        UpdatePlayerStatus();

        // Adjust VFX flame size
        if (lanternVFXGraph != null && !outOfFuel) // Ensure VFX graph exists and we have fuel
        {
            lanternVFXGraph.SetVector2(flameSizeRangePropertyName, raisedFlameSize);
        }

        Debug.Log("Lantern Raised");

        if (interactionCoroutine != null) StopCoroutine(interactionCoroutine);
        interactionCoroutine = StartCoroutine(MonsterInteractionCheck());

        if (currentPhysicsSwayScript != null)
        {
            currentPhysicsSwayScript.targetLocalOffset = raisedLocalPositionOffset;
        }
    }

    void StopRaising()
    {
        // Only proceed if it was actually raised or if equipped and out of fuel (to reset visual state)
        if (!isRaised && !(isEquipped && outOfFuel)) return;

        bool wasActuallyRaised = isRaised; // Store before changing
        isRaised = false;
        if (playerStatus != null) playerStatus.IsLanternRaised = false;

        if (lanternLight != null)
        {
            if (!outOfFuel) SetLightState(true, defaultIntensity, defaultRange);
            else SetLightState(false); // Ensure light is off if out of fuel
        }

        UpdatePlayerStatus();

        // Adjust VFX flame size back to default
        if (lanternVFXGraph != null && !outOfFuel) // Ensure VFX graph exists and we have fuel
        {
            lanternVFXGraph.SetVector2(flameSizeRangePropertyName, defaultFlameSize);
        }
        // If outOfFuel, VFX would have been stopped by OutOfFuel()

        if (wasActuallyRaised) Debug.Log("Lantern Lowered");


        if (interactionCoroutine != null)
        {
            StopCoroutine(interactionCoroutine);
            interactionCoroutine = null;
        }

        if (currentPhysicsSwayScript != null)
        {
            currentPhysicsSwayScript.targetLocalOffset = Vector3.zero;
        }
    }

    void DrainFuel(float deltaTime)
    {
        if (currentFuel <= 0) return;
        float drain = isRaised ? (passiveDrainRate + activeDrainRate) : passiveDrainRate;
        currentFuel -= drain * deltaTime;
        currentFuel = Mathf.Clamp(currentFuel, 0f, maxFuel);

        if (currentFuel <= 0) OutOfFuel();
    }

    void OutOfFuel()
    {
        Debug.Log("Lantern Out of Fuel!");
        outOfFuel = true;
        if (playerStatus != null) playerStatus.IsLanternRaised = false; // Update status immediately
        if (lanternLight != null) SetLightState(false);
        UpdatePlayerStatus();

        // Disable VFX
        if (lanternVFXGraph != null)
        {
            lanternVFXGraph.Stop();
        }
        if (currentLanternVFXHolder != null)
        {
            currentLanternVFXHolder.SetActive(false);
        }

        // NEW CHANGE
        StartGasBurnLoopSFX();
        // END CHANGE

        if (isRaised) // If it was raised when fuel ran out
        {
            StopRaising(); // This will reset sway, and confirm isRaised = false
        }
    }

    public void RefillFuel()
    {
        Debug.Log("Refilling Lantern Fuel");
        currentFuel = maxFuel;
        outOfFuel = false; // Set outOfFuel to false before further checks

        if (isEquipped)
        {
            SetLightState(true, isRaised ? raisedIntensity : defaultIntensity, isRaised ? raisedRange : defaultRange);
            UpdatePlayerStatus();

            // Re-enable and configure VFX
            if (currentLanternVFXHolder != null && lanternVFXGraph != null) // Ensure references are still valid
            {
                currentLanternVFXHolder.SetActive(true);
                lanternVFXGraph.Play(); // Start/Resume VFX
                if (isRaised)
                {
                    lanternVFXGraph.SetVector2(flameSizeRangePropertyName, raisedFlameSize);
                }
                else
                {
                    lanternVFXGraph.SetVector2(flameSizeRangePropertyName, defaultFlameSize);
                }
            }
            // It's possible currentLanternVFXHolder is null if ToggleEquip had an issue,
            // but if isEquipped is true, it should have been set up.

            if (playerStatus != null) playerStatus.IsLanternRaised = isRaised;

            if (currentPhysicsSwayScript != null && !isRaised)
            {
                currentPhysicsSwayScript.targetLocalOffset = Vector3.zero;
            }
            // NEW CHANGE
            StartGasBurnLoop(); // Start the gas burn sound again now that there's fuel
            // END CHANGE
        }
    }

    void SetLightState(bool enabled, float intensity = 0, float range = 0)
    {
        if (lanternLight == null) return;
        lanternLight.enabled = enabled;
        if (enabled)
        {
            if (lightFlicker != null)
            {
                lightFlicker.enabled = true;
                lightFlicker.SetBaseValues(intensity, range);
            }
            else
            {
                lanternLight.intensity = intensity;
                lanternLight.range = range;
            }
        }
        else
        {
            if (lightFlicker != null) lightFlicker.enabled = false;
            lanternLight.intensity = 0; // Ensure intensity is zero when disabled
        }
    }

    /// <summary>
    /// Updates the central PlayerStatus script with the lantern's current state.
    /// </summary>
    private void UpdatePlayerStatus()
    {
        if (playerStatus != null)
        {
            playerStatus.IsLanternOn = IsLightOn;
            playerStatus.IsLanternRaised = isRaised;
        }
    }

    IEnumerator MonsterInteractionCheck()
    {
        while (isRaised && !outOfFuel)
        {
            // Repel Hemanneken logic remains
            Collider[] hemannekenCols = Physics.OverlapSphere(transform.position, hemannekenRepelRadius, hemannekenLayer);
            foreach (Collider col in hemannekenCols)
            {
                HemannekenAI hemanneken = col.GetComponent<HemannekenAI>();
                if (hemanneken != null) hemanneken.Repel(transform.position);
            }

            yield return new WaitForSeconds(interactionCheckInterval);
        }
        interactionCoroutine = null;
    }

    // NEW CHANGE (FMOD Sound Methods)
    private void StartGasBurnLoop()
    {
        // Check if already valid to prevent multiple instances (though CreateInstance handles this)
        if (isEquipped && !outOfFuel && !lanternGasBurnLoopEvent.IsNull && !gasBurnSoundInstance.isValid())
        {
            gasBurnSoundInstance = RuntimeManager.CreateInstance(lanternGasBurnLoopEvent);
            if (currentLanternInstance != null) // Ensure lantern instance exists for attachment
            {
                RuntimeManager.AttachInstanceToGameObject(gasBurnSoundInstance, currentLanternInstance.transform); // Attach to transform
                gasBurnSoundInstance.start();
            }
            else
            {
                Debug.LogError("FMOD: Tried to start gas burn loop, but currentLanternInstance is null while isEquipped is true and not out of fuel.");
                // Fallback: play at controller position if lantern instance is somehow null
                // gasBurnSoundInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
                // gasBurnSoundInstance.start();
            }
        }
    }

    private void StartGasBurnLoopSFX() // Renamed for clarity, this STOPS the loop
    {
        if (gasBurnSoundInstance.isValid())
        {
            gasBurnSoundInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            gasBurnSoundInstance.release();
            // gasBurnSoundInstance = default; // Optional: reset the struct if you want to be very clean
        }
    }
    // END CHANGE

    void OnDrawGizmosSelected()
    {
        Vector3 interactionCenter = lanternHandAnchor != null ? lanternHandAnchor.position : transform.position;

        if (isRaised && isEquipped && !outOfFuel)
        {
            Gizmos.color = Color.red; // Hemanneken repel radius
            Gizmos.DrawWireSphere(interactionCenter, hemannekenRepelRadius);
        }
        else if (isEquipped)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(interactionCenter, hemannekenRepelRadius);
        }
    }
}