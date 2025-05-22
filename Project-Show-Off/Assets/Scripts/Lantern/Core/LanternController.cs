using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

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
    // public float raiseAnimationDuration = 0.3f; // REMOVED - Now controlled by PhysicsLanternSway.localOffsetSmoothTime

    // private Vector3 defaultLocalLanternPosition = Vector3.zero; // REMOVED
    // private Coroutine raiseAnimationCoroutine; // REMOVED

    private Coroutine interactionCoroutine;
    private PlayerInput playerInputActions;

    private void Awake()
    {
        if (playerInputActions == null)
        {
            playerInputActions = new PlayerInput();
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
                StartRaising();
            }
            else if (playerInputActions.Player.RaiseLantern.WasReleasedThisFrame())
            {
                StopRaising();
            }
        }
        else if (isRaised && playerInputActions.Player.RaiseLantern.WasReleasedThisFrame()) // Allow lowering even if out of fuel
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
                // No need to set localPosition here, PhysicsLanternSway will handle it based on its targetLocalOffset
                currentLanternInstance.transform.localRotation = Quaternion.identity;

                currentPhysicsSwayScript = currentLanternInstance.GetComponent<PhysicsLanternSway>();
                if (currentPhysicsSwayScript == null)
                {
                    currentPhysicsSwayScript = currentLanternInstance.GetComponentInChildren<PhysicsLanternSway>();
                }

                if (currentPhysicsSwayScript != null)
                {
                    currentPhysicsSwayScript.PlayerInputActionsInstance = this.playerInputActions;
                    Camera mainCam = Camera.main; // Or your player camera reference
                    if (mainCam != null) currentPhysicsSwayScript.playerCameraTransform = mainCam.transform;
                    else Debug.LogError("LanternController cannot find Player Camera for PhysicsLanternSway!");

                    currentPhysicsSwayScript.lanternHoldTarget = lanternHandAnchor;

                    Transform handleTransform = currentLanternInstance.transform.Find("Lantern RoamWaypoints");
                    if (handleTransform != null) currentPhysicsSwayScript.handleRigidbody = handleTransform.GetComponent<Rigidbody>();
                    else Debug.LogError("PhysicsLanternSway: Could not find 'Lantern RoamWaypoints'.");

                    Transform lanternBodyPartTransform = currentLanternInstance.transform.Find("Lantern Body");
                    if (lanternBodyPartTransform != null) currentPhysicsSwayScript.swingingLanternBodyRB = lanternBodyPartTransform.GetComponent<Rigidbody>();
                    else Debug.LogError("PhysicsLanternSway: Could not find 'Lantern Body'.");

                    // Set initial offset immediately and reset sway to snap to it
                    currentPhysicsSwayScript.SetTargetLocalOffsetImmediate(Vector3.zero); // Start at default position
                    currentPhysicsSwayScript.ResetSway(true); // Reset sway respecting the new immediate offset
                }
                else Debug.LogError("No PhysicsLanternSway script found on lantern prefab!");

                lanternLight = currentLanternInstance.GetComponentInChildren<Light>();
                lightFlicker = currentLanternInstance.GetComponentInChildren<LightFlicker>();
            }

            currentLanternInstance.SetActive(true);
            isRaised = false; // Ensure it's not marked as raised
            outOfFuel = (currentFuel <= 0);

            if (currentPhysicsSwayScript != null)
            {
                // Ensure sway script aims for default position if re-equipping
                currentPhysicsSwayScript.targetLocalOffset = Vector3.zero;
            }


            if (!outOfFuel && lanternLight != null) SetLightState(true, defaultIntensity, defaultRange);
            else if (lanternLight != null) SetLightState(false);
            Debug.Log("Lantern Equipped");
        }
        else // Unequipping
        {
            if (isRaised) StopRaising(); // Lower it first if it was raised

            if (lanternLight != null) SetLightState(false);
            if (currentLanternInstance != null)
            {
                // No need to manage localPosition directly here
                currentLanternInstance.SetActive(false);
            }
            Debug.Log("Lantern Unequipped");
        }
    }

    // IEnumerator AnimateLanternPosition(...) // REMOVED

    void StartRaising()
    {
        if (!isEquipped || isRaised || outOfFuel || lanternLight == null) return;

        isRaised = true;
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
        if (!isRaised && !(isEquipped && outOfFuel)) return; // Only proceed if it was raised or if it's being forced down due to fuel

        bool wasActuallyRaised = isRaised; // Store if it was in the "raised" state for light logic
        isRaised = false;

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
        if (lanternLight != null) SetLightState(false);

        if (isRaised) // If it was raised, force it to lower
        {
            StopRaising(); // This will also set targetLocalOffset to zero
        }
    }

    public void RefillFuel()
    {
        Debug.Log("Refilling Lantern Fuel");
        currentFuel = maxFuel;
        outOfFuel = false;

        if (isEquipped && lanternLight != null)
        {
            // If it was raised, it will remain visually raised unless StopRaising is called.
            // If you want it to reset to lowered position on refill:
            if (isRaised)
            {
                // Optionally decide if refilling should lower it or keep it raised
                // StopRaising(); // Uncomment to lower on refill
            }
            else // If not raised, ensure light is at default values
            {
                SetLightState(true, defaultIntensity, defaultRange);
            }

            if (currentPhysicsSwayScript != null && !isRaised) // If not raised, ensure it's at default offset
            {
                currentPhysicsSwayScript.targetLocalOffset = Vector3.zero;
            }
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

    void OnDrawGizmosSelected()
    {
        // Gizmos should be drawn from lanternHandAnchor or player's actual position
        // depending on what the radii are relative to.
        // If radii are relative to player:
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