using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem; // <<< MAKE SURE THIS IS HERE

public class LanternController : MonoBehaviour
{
    [Header("Lantern Setup")]
    public GameObject lanternPrefab;
    private GameObject currentLanternInstance;
    public Light lanternLight;             // This will be assigned from the instantiated prefab
    public Transform lanternHoldPosition;

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
    public LightFlicker lightFlicker;     // This will be assigned from the instantiated prefab

    [Header("Fuel System")]
    public float maxFuel = 100f;
    public float currentFuel;
    [Tooltip("Fuel units consumed per second when lantern is normally equipped.")]
    public float passiveDrainRate = 0.1f;
    [Tooltip("Additional fuel units consumed per second when lantern is raised.")]
    public float activeDrainRate = 1.0f;

    [Header("Interaction")]
    public float hemannekenRepelRadius = 7f;
    public float nixieAttractRadius = 20f;
    public LayerMask hemannekenLayer;
    public LayerMask nixieLayer;
    public float interactionCheckInterval = 0.25f;

    // [Header("Input")] // These are now effectively fallbacks or can be removed
    // public KeyCode equipKey = KeyCode.F;
    // public KeyCode raiseKey = KeyCode.Mouse1;

    // Internal refs
    private Coroutine interactionCoroutine;
    private LanternSway lanternSway;      // This will be assigned from the instantiated prefab

    private PlayerInput playerInputActions; // This should be your generated class

    private void Awake()
    {
        // Ensure playerInputActions is initialized.
        // If your .inputactions file is named "PlayerInput.inputactions",
        // the generated C# class is named "PlayerInput".
        if (playerInputActions == null)
        {
            playerInputActions = new PlayerInput();
        }
    }

    void Start()
    {
        if (lanternPrefab == null || lanternHoldPosition == null)
        {
            Debug.LogError("LanternController: Missing Lantern PREFAB or LanternHoldPosition reference!");
            enabled = false;
            return;
        }
        currentFuel = maxFuel;
        // Note: currentLanternInstance, lanternLight, lightFlicker, lanternSway are null here.
        // They are assigned in ToggleEquip when the lantern is first equipped.
    }

    void Update()
    {
        HandleInput(); // Make sure playerInputActions is not null

        if (isEquipped && !outOfFuel)
        {
            DrainFuel(Time.deltaTime);
        }
        else if (outOfFuel && isEquipped)
        {
            // Ensure light is off if it was on
            if (lanternLight != null && lanternLight.enabled) SetLightState(false);
        }
    }

    void OnEnable()
    {
        if (playerInputActions == null) // Defensive check from Awake
        {
            playerInputActions = new PlayerInput();
        }
        playerInputActions.Lantern.Enable(); // Enable the specific "Lantern" action map
    }

    void OnDisable()
    {
        if (playerInputActions != null) // Check if it was initialized
        {
            playerInputActions.Lantern.Disable(); // Disable the specific "Lantern" action map
        }
    }

    void HandleInput()
    {
        if (playerInputActions == null) return; // Should not happen if Awake/OnEnable worked

        // Equip/Unequip Toggle
        if (playerInputActions.Lantern.EquipLantern.WasPressedThisFrame()) // <<< CORRECTED
        {
            ToggleEquip();
        }

        // Raise/Lower Logic
        if (isEquipped && !outOfFuel)
        {
            if (playerInputActions.Lantern.RaiseLantern.WasPressedThisFrame())
            {
                StartRaising();
            }
            else if (playerInputActions.Lantern.RaiseLantern.WasReleasedThisFrame())
            {
                StopRaising();
            }
        }
        else if (isRaised && playerInputActions.Lantern.RaiseLantern.WasReleasedThisFrame())
        {
            // This handles the case where fuel runs out WHILE raised, or player unequips while raised.
            // Or if the lantern was unequipped for other reasons.
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
                currentLanternInstance = Instantiate(lanternPrefab, lanternHoldPosition);
                currentLanternInstance.transform.localPosition = Vector3.zero;
                currentLanternInstance.transform.localRotation = Quaternion.identity;

                lanternLight = currentLanternInstance.GetComponentInChildren<Light>();
                if (lanternLight == null) Debug.LogError("No Light component found on instantiated lantern prefab or its children!");

                lightFlicker = currentLanternInstance.GetComponentInChildren<LightFlicker>();
                if (lightFlicker == null) Debug.LogWarning("No LightFlicker component found on instantiated lantern prefab.");

                lanternSway = currentLanternInstance.GetComponent<LanternSway>();
                if (lanternSway == null) Debug.LogWarning("No LanternSway component found on instantiated lantern prefab.");
                else
                {
                    if (lanternHoldPosition != null && lanternHoldPosition.parent != null)
                        lanternSway.playerCameraTransform = lanternHoldPosition.parent;
                    else
                        Debug.LogError("LanternSway cannot find player camera through lanternHoldPosition.parent!");
                }
            }
            currentLanternInstance.SetActive(true);

            // Reset raised state in case it was unequipped while raised
            isRaised = false;
            outOfFuel = (currentFuel <= 0);

            if (!outOfFuel && lanternLight != null)
            {
                SetLightState(true, defaultIntensity, defaultRange);
                Debug.Log("Lantern Equipped");
            }
            else if (lanternLight != null)
            { // Still equip, but light stays off if no fuel
                SetLightState(false);
                Debug.Log("Lantern Equipped (Out of Fuel or No Light)");
            }

            if (lanternSway != null) lanternSway.ResetSway();
        }
        else // Unequipping
        {
            StopRaising(); // Ensure raise state logic is processed (like stopping coroutine)
            if (lanternLight != null) SetLightState(false); // Turn off light before deactivating
            if (currentLanternInstance != null)
            {
                currentLanternInstance.SetActive(false);
            }
            Debug.Log("Lantern Unequipped");
        }
    }

    void StartRaising()
    {
        if (!isEquipped || isRaised || outOfFuel || lanternLight == null) return;

        isRaised = true;
        SetLightState(true, raisedIntensity, raisedRange);
        Debug.Log("Lantern Raised");

        if (interactionCoroutine != null) StopCoroutine(interactionCoroutine);
        interactionCoroutine = StartCoroutine(MonsterInteractionCheck());

        if (lanternSway != null) lanternSway.NotifyRaised(true);
    }

    void StopRaising()
    {
        if (!isRaised) return;

        isRaised = false;

        if (lanternLight != null) // Only change light state if we have a light
        {
            if (!outOfFuel)
            {
                SetLightState(true, defaultIntensity, defaultRange);
            }
            else
            {
                SetLightState(false); // Turn off if out of fuel
            }
        }
        Debug.Log("Lantern Lowered");

        if (interactionCoroutine != null)
        {
            StopCoroutine(interactionCoroutine);
            interactionCoroutine = null;
        }
        if (lanternSway != null) lanternSway.NotifyRaised(false);
    }

    void DrainFuel(float deltaTime)
    {
        if (currentFuel <= 0) return;

        float drain = isRaised ? (passiveDrainRate + activeDrainRate) : passiveDrainRate;
        currentFuel -= drain * deltaTime;
        currentFuel = Mathf.Clamp(currentFuel, 0f, maxFuel);

        if (currentFuel <= 0)
        {
            OutOfFuel();
        }
    }

    void OutOfFuel()
    {
        Debug.Log("Lantern Out of Fuel!");
        outOfFuel = true;
        if (lanternLight != null) SetLightState(false);

        // If it was raised when it ran out of fuel, we should process StopRaising specific logic
        // (like stopping coroutines, notifying sway script, etc.)
        // The `isRaised` flag itself will be handled by normal input flow or ToggleEquip.
        if (isRaised)
        {
            // We don't want to call StopRaising() directly here if it might re-enable the light
            // because SetLightState(false) above already handled it.
            // Instead, just ensure related processes are stopped.
            if (interactionCoroutine != null)
            {
                StopCoroutine(interactionCoroutine);
                interactionCoroutine = null;
            }
            if (lanternSway != null) lanternSway.NotifyRaised(false); // Indicate it's no longer "raised" visually/mechanically
            // isRaised = false; // No, let HandleInput manage this. Or if equipping while OOF, ToggleEquip handles it.
        }
    }

    public void RefillFuel()
    {
        Debug.Log("Refilling Lantern Fuel");
        currentFuel = maxFuel;
        outOfFuel = false;

        if (isEquipped && lanternLight != null) // If equipped and we have a light component
        {
            // It's generally safer to revert to default state on refill,
            // rather than trying to resume a "raised" state that might have been interrupted.
            isRaised = false;
            SetLightState(true, defaultIntensity, defaultRange);
            if (lanternSway != null) lanternSway.NotifyRaised(false); // Ensure sway knows it's not "raised"
        }
    }

    void SetLightState(bool enabled, float intensity = 0, float range = 0)
    {
        if (lanternLight == null) // Safety check
        {
            // Debug.LogWarning("SetLightState called, but lanternLight is null.");
            return;
        }

        lanternLight.enabled = enabled;
        if (enabled)
        {
            if (lightFlicker != null)
            {
                lightFlicker.enabled = true; // Ensure flicker script is enabled
                lightFlicker.SetBaseValues(intensity, range);
            }
            else
            {
                lanternLight.intensity = intensity;
                lanternLight.range = range;
            }
        }
        else // Light is being disabled
        {
            if (lightFlicker != null)
            {
                lightFlicker.enabled = false; // Disable flicker script when light is off
            }
            // The Light component's intensity is effectively 0 when lanternLight.enabled = false,
            // but setting it explicitly can be good for clarity or if something else might re-enable it.
            lanternLight.intensity = 0;
        }
    }

    // ... (MonsterInteractionCheck and OnDrawGizmosSelected remain the same) ...
    IEnumerator MonsterInteractionCheck()
    {
        while (isRaised && !outOfFuel) // Keep checking while raised and fueled
        {
            // Check for Hemanneken
            Collider[] hemannekenCols = Physics.OverlapSphere(transform.position, hemannekenRepelRadius, hemannekenLayer);
            foreach (Collider col in hemannekenCols)
            {
                HemannekenAI hemanneken = col.GetComponent<HemannekenAI>();
                if (hemanneken != null)
                {
                    hemanneken.Repel(transform.position);
                }
            }

            // Check for Nixies
            Collider[] nixieCols = Physics.OverlapSphere(transform.position, nixieAttractRadius, nixieLayer);
            foreach (Collider col in nixieCols)
            {
                NixieAI nixie = col.GetComponent<NixieAI>();
                if (nixie != null)
                {
                    nixie.Attract(transform.position);
                }
            }

            yield return new WaitForSeconds(interactionCheckInterval);
        }
        interactionCoroutine = null;
    }

    void OnDrawGizmosSelected()
    {
        if (isRaised)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, hemannekenRepelRadius);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, nixieAttractRadius);
        }
        else if (isEquipped)
        {
            Gizmos.color = Color.gray; // Show potential range even if not raised
            Gizmos.DrawWireSphere(transform.position, hemannekenRepelRadius);
            Gizmos.DrawWireSphere(transform.position, nixieAttractRadius);
        }
    }
}