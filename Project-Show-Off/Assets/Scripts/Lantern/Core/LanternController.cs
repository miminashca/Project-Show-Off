using FMOD.Studio;
using FMODUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

public class LanternController : MonoBehaviour
{
    [Header("Lantern Setup")]
    public GameObject lanternPrefab;
    public Transform lanternHandAnchor;

    private GameObject currentLanternInstance;
    private PhysicsLanternSway currentPhysicsSwayScript;
    private Light[] lanternLights;

    [Header("State")]
    public bool isEquipped = false;
    public bool isRaised = false;
    private bool outOfFuel = false;
    public bool IsLightOn { get; private set; }
    public float TimeLanternRaised { get; private set; }

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

    [Header("Hemanneken Interaction")]
    public float hemannekenRepelRadius = 7f;
    public LayerMask hemannekenLayer;
    public float interactionCheckInterval = 0.25f;

    [Header("Nixie Interaction")]
    [Tooltip("The flicker speed when a Nixie is chasing.")]
    public float nixieFlickerSpeed = 15f;
    [Tooltip("The MINIMUM intensity multiplier when a Nixie is chasing.")]
    public float nixieFlickerMinIntensity = 0.4f;
    [Tooltip("The MAXIMUM intensity multiplier when a Nixie is chasing.")]
    public float nixieFlickerMaxIntensity = 1.6f;

    private Dictionary<LightFlicker, (float speed, float min, float max)> _originalFlickerValues = new Dictionary<LightFlicker, (float, float, float)>();
    private bool _isNixieFlickerActive = false;

    [Header("Raise Animation")]
    public Vector3 raisedLocalPositionOffset = new Vector3(0, 0.2f, 0.05f);

    [Header("FMOD Sounds")]
    [SerializeField]
    private EventReference lanternPullOutSoundEvent;
    [SerializeField]
    private EventReference lanternPutAwaySoundEvent;
    [SerializeField]
    private EventReference lanternGasBurnLoopEvent;

    private EventInstance gasBurnSoundInstance;

    [Header("VFX Settings")]
    [Tooltip("Name of the exposed Vector2 property in the VFX Graph for flame size X (min) and Y (max).")]
    public string flameSizeRangePropertyName = "Flame_SizeRange";
    public Vector2 defaultFlameSize = new Vector2(0.1f, 0.2f);
    public Vector2 raisedFlameSize = new Vector2(0.2f, 0.4f);

    private GameObject currentLanternVFXHolder;
    private VisualEffect lanternVFXGraph;
    private Coroutine interactionCoroutine;

    private PlayerInput playerInputActions;
    private PlayerStatus playerStatus;

    private HingeLimitStabilizer hinge;

    public event Action<float, float> OnFuelChanged;

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
        OnFuelChanged?.Invoke(currentFuel, maxFuel);

        ClueEventManager.Instance.OnFuelPickedUp += RefillFuel;
    }

    void Update()
    {
        HandleInput();

        // If the lantern is currently raised, equipped, and has fuel, increment the timer.
        if (isRaised && isEquipped && !outOfFuel)
        {
            TimeLanternRaised += Time.deltaTime;
        }
        else
        {
            // Otherwise, reset the timer to zero.
            TimeLanternRaised = 0f;
        }

        if (isEquipped && !outOfFuel)
        {
            DrainFuel(Time.deltaTime);
        }
        else if (outOfFuel && isEquipped)
        {
            if (lanternLights != null && lanternLights.Length > 0 && IsLightOn) SetLightState(false);
        }
    }

    void OnEnable()
    {
        if (playerInputActions == null)
        {
            playerInputActions = new PlayerInput();
        }
        playerInputActions.Player.Enable();

        NixieEventBus.OnNixieChaseStart += HandleNixieChaseStart;
        NixieEventBus.OnNixieChaseEnd += HandleNixieChaseEnd;
    }

    void OnDisable()
    {
        // Check for null in case the instance is destroyed before this object
        if (ClueEventManager.Instance != null)
        {
            ClueEventManager.Instance.OnFuelPickedUp -= RefillFuel;
        }

        if (playerInputActions != null)
        {
            playerInputActions.Player.Disable();
        }

        StopGasBurnLoopSFX();

        if (isEquipped)
        {
            ToggleEquip();
        }

        NixieEventBus.OnNixieChaseStart -= HandleNixieChaseStart;
        NixieEventBus.OnNixieChaseEnd -= HandleNixieChaseEnd;
    }

    private void OnDestroy()
    {
        StopGasBurnLoopSFX();
    }

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
        else if (isRaised && playerInputActions.Player.RaiseLantern.WasReleasedThisFrame())
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
                    isEquipped = false;
                    if (currentLanternInstance != null) Destroy(currentLanternInstance);
                    currentLanternInstance = null;
                    return;
                }
                currentPhysicsSwayScript = parts.swayScript;

                currentLanternVFXHolder = parts.lanternVFXHolder;
                if (currentLanternVFXHolder != null)
                {
                    lanternVFXGraph = currentLanternVFXHolder.GetComponentInChildren<VisualEffect>();
                }

                if (currentPhysicsSwayScript != null)
                {
                    currentPhysicsSwayScript.InitializeSway(this.playerInputActions, lanternHandAnchor, parts.swingingLanternBodyRB);
                }
                else Debug.LogError("No PhysicsLanternSway script found on lantern prefab!");

                if (lanternLights == null || lanternLights.Length == 0)
                {
                    lanternLights = parts.lanternLights;
                }
                if (lanternLights == null || lanternLights.Length == 0)
                {
                    Debug.LogError("LanternController: No Lights have been assigned in the LanternParts component on the prefab!", currentLanternInstance);
                }
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
                if (lanternLights != null && lanternLights.Length > 0) SetLightState(true, defaultIntensity, defaultRange);
                if (currentLanternVFXHolder != null) currentLanternVFXHolder.SetActive(true);
                if (lanternVFXGraph != null)
                {
                    lanternVFXGraph.SetVector2(flameSizeRangePropertyName, defaultFlameSize);
                    lanternVFXGraph.Play();
                }
            }
            else // Equipping while out of fuel
            {
                if (lanternLights != null && lanternLights.Length > 0) SetLightState(false);
                if (lanternVFXGraph != null) lanternVFXGraph.Stop();
                if (currentLanternVFXHolder != null) currentLanternVFXHolder.SetActive(false);
            }

            if (lanternPullOutSoundEvent.Guid != System.Guid.Empty)
            {
                RuntimeManager.PlayOneShot(lanternPullOutSoundEvent, transform.position);
            }
            if (!outOfFuel)
            {
                StartGasBurnLoop();
            }

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

            if (lanternLights != null && lanternLights.Length > 0) SetLightState(false);

            if (lanternVFXGraph != null) lanternVFXGraph.Stop();
            if (currentLanternVFXHolder != null) currentLanternVFXHolder.SetActive(false);
            if (currentLanternInstance != null) currentLanternInstance.SetActive(false);

            Debug.Log("Lantern Unequipped");

            if (lanternPutAwaySoundEvent.Guid != System.Guid.Empty)
            {
                RuntimeManager.PlayOneShot(lanternPutAwaySoundEvent, transform.position);
            }
            StopGasBurnLoopSFX();
        }

        UpdatePlayerStatus();
    }

    void StartRaising()
    {
        if (!isEquipped || isRaised || outOfFuel) return;

        isRaised = true;
        if (playerStatus != null) playerStatus.IsLanternRaised = true;

        SetLightState(true, raisedIntensity, raisedRange);

        if (lanternVFXGraph != null && !outOfFuel)
        {
            lanternVFXGraph.SetVector2(flameSizeRangePropertyName, raisedFlameSize);
        }

        if (interactionCoroutine != null) StopCoroutine(interactionCoroutine);
        interactionCoroutine = StartCoroutine(HemannekenInteractionCheck());

        if (currentPhysicsSwayScript != null)
        {
            currentPhysicsSwayScript.targetLocalOffset = raisedLocalPositionOffset;
        }
        UpdatePlayerStatus();
    }

    void StopRaising()
    {
        // Only proceed if it was actually raised or if equipped and out of fuel (to reset visual state)
        if (!isRaised && !(isEquipped && outOfFuel)) return;

        isRaised = false;

        if (lanternLights != null && lanternLights.Length > 0)
        {
            if (!outOfFuel) SetLightState(true, defaultIntensity, defaultRange);
            else SetLightState(false);
        }

        if (lanternVFXGraph != null && !outOfFuel)
        {
            lanternVFXGraph.SetVector2(flameSizeRangePropertyName, defaultFlameSize);
        }

        if (interactionCoroutine != null)
        {
            StopCoroutine(interactionCoroutine);
            interactionCoroutine = null;
        }

        if (currentPhysicsSwayScript != null)
        {
            currentPhysicsSwayScript.targetLocalOffset = Vector3.zero;
        }
        UpdatePlayerStatus();
    }

    void DrainFuel(float deltaTime)
    {
        if (currentFuel <= 0) return;
        float drain = isRaised ? (passiveDrainRate + activeDrainRate) : passiveDrainRate;
        currentFuel -= drain * deltaTime;
        currentFuel = Mathf.Clamp(currentFuel, 0f, maxFuel);

        OnFuelChanged?.Invoke(currentFuel, maxFuel);

        if (currentFuel <= 0) OutOfFuel();
    }

    private void StartGasBurnLoop()
    {
        if (isEquipped && !outOfFuel && !lanternGasBurnLoopEvent.IsNull && !gasBurnSoundInstance.isValid())
        {
            gasBurnSoundInstance = RuntimeManager.CreateInstance(lanternGasBurnLoopEvent);
            if (currentLanternInstance != null)
            {
                RuntimeManager.AttachInstanceToGameObject(gasBurnSoundInstance, currentLanternInstance);
                gasBurnSoundInstance.start();
            }
        }
    }

    private void StopGasBurnLoopSFX()
    {
        if (gasBurnSoundInstance.isValid())
        {
            gasBurnSoundInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            gasBurnSoundInstance.release();
        }
    }

    void OutOfFuel()
    {
        outOfFuel = true;

        if (playerStatus != null) playerStatus.IsLanternRaised = false; // Update status immediately

        if (lanternLights != null && lanternLights.Length > 0) SetLightState(false);

        if (lanternVFXGraph != null) lanternVFXGraph.Stop();
        if (currentLanternVFXHolder != null) currentLanternVFXHolder.SetActive(false);

        StopGasBurnLoopSFX();

        if (isRaised)
        {
            StopRaising();
        }
        UpdatePlayerStatus();
    }

    public void RefillFuel()
    {
        currentFuel = maxFuel;
        outOfFuel = false;

        OnFuelChanged?.Invoke(currentFuel, maxFuel);

        if (isEquipped)
        {
            SetLightState(true, isRaised ? raisedIntensity : defaultIntensity, isRaised ? raisedRange : defaultRange);

            if (currentLanternVFXHolder != null && lanternVFXGraph != null)
            {
                currentLanternVFXHolder.SetActive(true);
                lanternVFXGraph.Play();
                lanternVFXGraph.SetVector2(flameSizeRangePropertyName, isRaised ? raisedFlameSize : defaultFlameSize);
            }

            if (playerStatus != null) playerStatus.IsLanternRaised = isRaised;

            if (currentPhysicsSwayScript != null && !isRaised)
            {
                currentPhysicsSwayScript.targetLocalOffset = Vector3.zero;
            }

            StartGasBurnLoop();
        }
        UpdatePlayerStatus();
    }

    void SetLightState(bool enabled, float intensity = 0, float range = 0)
    {
        if (lanternLights == null || lanternLights.Length == 0) return;

        IsLightOn = enabled; // Update IsLightOn status

        foreach (Light light in lanternLights)
        {
            if (light == null) continue; // Skip if a light in the array is null

            light.enabled = enabled;
            LightFlicker flicker = light.GetComponent<LightFlicker>();

            if (enabled)
            {
                if (flicker != null && flicker.enabled)
                {
                    // If flicker is active (e.g., from Nixie), let it control the light.
                    // Just ensure its base values are updated.
                    flicker.SetBaseValues(intensity, range);
                }
                else
                {
                    // If no flicker, set values directly.
                    light.intensity = intensity;
                    if (light.type == LightType.Point || light.type == LightType.Spot)
                    {
                        light.range = range;
                    }
                }
            }
            else
            {
                // When turning off, always disable flicker and zero out intensity.
                if (flicker != null) flicker.enabled = false;
                light.intensity = 0;
            }
        }
    }

    private void UpdatePlayerStatus()
    {
        if (playerStatus != null)
        {
            playerStatus.IsLanternOn = IsLightOn;
            playerStatus.IsLanternRaised = isRaised;
        }
    }

    IEnumerator HemannekenInteractionCheck()
    {
        while (isRaised && !outOfFuel)
        {
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

    /// <summary>
    /// Called by NixieEventBus when a chase begins.
    /// </summary>
    private void HandleNixieChaseStart()
    {
        if (_isNixieFlickerActive || lanternLights == null) return;
        _isNixieFlickerActive = true;
        _originalFlickerValues.Clear(); // Clear old values

        foreach (var light in lanternLights)
        {
            LightFlicker flicker = light.GetComponent<LightFlicker>();
            if (flicker != null)
            {
                flicker.enabled = true; // Ensure flicker is on
                // Store original values
                _originalFlickerValues[flicker] = (flicker.flickerSpeed, flicker.minIntensityMultiplier, flicker.maxIntensityMultiplier);

                // Apply panicked flicker values
                flicker.flickerSpeed = nixieFlickerSpeed;
                flicker.minIntensityMultiplier = nixieFlickerMinIntensity;
                flicker.maxIntensityMultiplier = nixieFlickerMaxIntensity;
            }
        }
    }

    /// <summary>
    /// Called by NixieEventBus when a chase ends.
    /// </summary>
    private void HandleNixieChaseEnd()
    {
        if (!_isNixieFlickerActive) return;
        _isNixieFlickerActive = false;

        foreach (var flickerKvp in _originalFlickerValues)
        {
            LightFlicker flicker = flickerKvp.Key;
            var originalValues = flickerKvp.Value;

            if (flicker != null)
            {
                // Restore original values
                flicker.flickerSpeed = originalValues.speed;
                flicker.minIntensityMultiplier = originalValues.min;
                flicker.maxIntensityMultiplier = originalValues.max;
            }
        }
        _originalFlickerValues.Clear();
    }
    
    public void ApplyLoadedFuel(float fuelAmount)
    {
        currentFuel = Mathf.Clamp(fuelAmount, 0f, maxFuel);
    
        // Update the state based on the new fuel level
        outOfFuel = (currentFuel <= 0);
    
        // Notify the UI or other listeners about the change
        OnFuelChanged?.Invoke(currentFuel, maxFuel);

        // If the lantern is supposed to be on, update its visuals and sounds
        if (isEquipped)
        {
            if (outOfFuel)
            {
                // If we loaded and are now out of fuel, turn everything off
                if (lanternLights != null && lanternLights.Length > 0) SetLightState(false);
                if (lanternVFXGraph != null) lanternVFXGraph.Stop();
                if (currentLanternVFXHolder != null) currentLanternVFXHolder.SetActive(false);
                StopGasBurnLoopSFX();
            }
            else
            {
                // If we have fuel, ensure light, VFX, and sound are correct for the current state (raised/lowered)
                SetLightState(true, isRaised ? raisedIntensity : defaultIntensity, isRaised ? raisedRange : defaultRange);
            
                if (currentLanternVFXHolder != null) currentLanternVFXHolder.SetActive(true);
                if (lanternVFXGraph != null)
                {
                    lanternVFXGraph.Play();
                    lanternVFXGraph.SetVector2(flameSizeRangePropertyName, isRaised ? raisedFlameSize : defaultFlameSize);
                }
                StartGasBurnLoop();
            }
        }
    }

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