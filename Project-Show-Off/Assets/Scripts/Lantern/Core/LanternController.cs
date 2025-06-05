using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
// NEW CHANGE
using FMODUnity;
using FMOD.Studio; // Added for EventInstance
// END CHANGE

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
    public float nixieAttractRadius = 20f;
    public LayerMask hemannekenLayer;
    public LayerMask nixieLayer;
    public float interactionCheckInterval = 0.25f;

    [Header("Raise Animation")]
    public Vector3 raisedLocalPositionOffset = new Vector3(0, 0.2f, 0.05f);

    private Coroutine interactionCoroutine;

    private PlayerInput playerInputActions;
    private PlayerStatus playerStatus;

    // NEW CHANGE
    [Header("FMOD Sounds")]
    [SerializeField]
    private EventReference lanternPullOutSoundEvent;
    [SerializeField]
    private EventReference lanternPutAwaySoundEvent;
    [SerializeField]
    private EventReference lanternGasBurnLoopEvent; // Event for the looping gas burn sound

    private EventInstance gasBurnSoundInstance; // Instance for the looping sound
    // END CHANGE

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
            ToggleEquip();
        }
    }

    // NEW CHANGE
    private void OnDestroy()
    {
        StartGasBurnLoopSFX();
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
                Debug.Log("Pressed!");
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
                currentLanternInstance = Instantiate(lanternPrefab, lanternHandAnchor);
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
                    currentPhysicsSwayScript.SetTargetLocalOffsetImmediate(Vector3.zero);
                }
                else Debug.LogError("No PhysicsLanternSway script found on lantern prefab!");

                // Attempt to find Light and LightFlicker if not directly assigned from parts
                if (lanternLight == null) lanternLight = currentLanternInstance.GetComponentInChildren<Light>();
                if (lightFlicker == null && lanternLight != null) lightFlicker = lanternLight.GetComponent<LightFlicker>();

                if (lanternLight == null) Debug.LogError("LanternController: Could not find a Light component on the lantern prefab or its children!", currentLanternInstance);

            }

            currentLanternInstance.SetActive(true);
            isRaised = false;
            outOfFuel = (currentFuel <= 0);

            if (currentPhysicsSwayScript != null)
            {
                currentPhysicsSwayScript.targetLocalOffset = Vector3.zero;
            }

            if (!outOfFuel && lanternLight != null) SetLightState(true, defaultIntensity, defaultRange);
            else if (lanternLight != null) SetLightState(false);
            Debug.Log("Lantern Equipped");

            // NEW CHANGE
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
            if (isRaised) StopRaising();
            else if (playerStatus != null) playerStatus.IsLanternRaised = false;

            if (lanternLight != null) SetLightState(false);
            if (currentLanternInstance != null)
            {
                currentLanternInstance.SetActive(false);
            }
            Debug.Log("Lantern Unequipped");

            // NEW CHANGE
            if (lanternPutAwaySoundEvent.Guid != System.Guid.Empty)
            {
                RuntimeManager.PlayOneShot(lanternPutAwaySoundEvent, transform.position);
            }
            StartGasBurnLoopSFX();
            // END CHANGE
        }
    }

    void StartRaising()
    {
        if (!isEquipped || isRaised || outOfFuel) return;

        isRaised = true;
        if (playerStatus != null) playerStatus.IsLanternRaised = true;
        SetLightState(true, raisedIntensity, raisedRange);
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
        if (!isRaised && !(isEquipped && outOfFuel)) return;

        bool wasActuallyRaised = isRaised;
        isRaised = false;
        if (playerStatus != null) playerStatus.IsLanternRaised = false;

        if (lanternLight != null)
        {
            if (!outOfFuel) SetLightState(true, defaultIntensity, defaultRange);
            else SetLightState(false);
        }
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
        if (playerStatus != null) playerStatus.IsLanternRaised = false;
        if (lanternLight != null) SetLightState(false);

        // NEW CHANGE
        StartGasBurnLoopSFX();
        // END CHANGE

        if (isRaised)
        {
            StopRaising();
        }
    }

    public void RefillFuel()
    {
        Debug.Log("Refilling Lantern Fuel");
        currentFuel = maxFuel;
        outOfFuel = false;

        if (isEquipped)
        {
            SetLightState(true, isRaised ? raisedIntensity : defaultIntensity, isRaised ? raisedRange : defaultRange);

            if (playerStatus != null) playerStatus.IsLanternRaised = isRaised;

            if (currentPhysicsSwayScript != null && !isRaised)
            {
                currentPhysicsSwayScript.targetLocalOffset = Vector3.zero;
            }
            // NEW CHANGE
            StartGasBurnLoopSFX();
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
            lanternLight.intensity = 0;
        }
    }

    IEnumerator MonsterInteractionCheck()
    {
        while (isRaised && !outOfFuel)
        {
            Collider[] hemannekenCols = Physics.OverlapSphere(transform.position, hemannekenRepelRadius, hemannekenLayer);
            foreach (Collider col in hemannekenCols)
            {
                HemannekenAI hemanneken = col.GetComponent<HemannekenAI>();
                if (hemanneken != null) hemanneken.Repel(transform.position);
            }
            Collider[] nixieCols = Physics.OverlapSphere(transform.position, nixieAttractRadius, nixieLayer);
            foreach (Collider col in nixieCols)
            {
                NixieAI nixie = col.GetComponent<NixieAI>();
                if (nixie != null) nixie.Attract(transform.position);
            }
            yield return new WaitForSeconds(interactionCheckInterval);
        }
        interactionCoroutine = null;
    }

    // NEW CHANGE
    private void StartGasBurnLoop()
    {
        if (isEquipped && !outOfFuel && !lanternGasBurnLoopEvent.IsNull && !gasBurnSoundInstance.isValid())
        {
            gasBurnSoundInstance = RuntimeManager.CreateInstance(lanternGasBurnLoopEvent);
            if (currentLanternInstance != null)
            {
                //RuntimeManager.AttachInstanceToGameObject(gasBurnSoundInstance, currentLanternInstance.transform); // Obsolete
                RuntimeManager.AttachInstanceToGameObject(gasBurnSoundInstance, currentLanternInstance); // Corrected line
                gasBurnSoundInstance.start();
            }
            else
            {
                Debug.LogError("FMOD: Tried to start gas burn loop, but currentLanternInstance is null while isEquipped is true.");
            }
        }
    }

    private void StartGasBurnLoopSFX()
    {
        if (gasBurnSoundInstance.isValid())
        {
            gasBurnSoundInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            gasBurnSoundInstance.release();
        }
    }
    // END CHANGE


    void OnDrawGizmosSelected()
    {
        Vector3 interactionCenter = lanternHandAnchor != null ? lanternHandAnchor.position : transform.position;

        if (isRaised)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(interactionCenter, hemannekenRepelRadius);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(interactionCenter, nixieAttractRadius);
        }
        else if (isEquipped)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(interactionCenter, hemannekenRepelRadius);
            Gizmos.DrawWireSphere(interactionCenter, nixieAttractRadius);
        }
    }
}