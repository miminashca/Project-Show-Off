using UnityEngine;
using System.Collections;

public class LanternController : MonoBehaviour
{
    [Header("Lantern Setup")]
    public GameObject lanternPrefab;
    private GameObject currentLanternInstance;
    public Light lanternLight;
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
    public LightFlicker lightFlicker;

    [Header("Fuel System")]
    public float maxFuel = 100f;
    public float currentFuel;
    [Tooltip("Fuel units consumed per second when lantern is normally equipped.")]
    public float passiveDrainRate = 0.1f; // 1% per 10 seconds if maxFuel=100
    [Tooltip("Additional fuel units consumed per second when lantern is raised.")]
    public float activeDrainRate = 1.0f; // 1% per second if maxFuel=100

    [Header("Interaction")]
    public float hemannekenRepelRadius = 7f;
    public float nixieAttractRadius = 20f; // Example radius
    public LayerMask hemannekenLayer;
    public LayerMask nixieLayer;
    public float interactionCheckInterval = 0.25f; // How often to check for nearby monsters when raised

    [Header("Input")]
    public KeyCode equipKey = KeyCode.F;
    public KeyCode raiseKey = KeyCode.Mouse1; // Right Mouse Button

    // Internal refs
    private Coroutine interactionCoroutine;
    private LanternSway lanternSway;

    private PlayerInput playerInputActions;

    private void Awake()
    {
        playerInputActions = new PlayerInput();
    }

    void Start()
    {
        if (lanternPrefab == null || lanternHoldPosition == null) // New: We get light later
        {
            Debug.LogError("LanternController: Missing Lantern PREFAB or LanternHoldPosition reference!");
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
            if (lanternLight.enabled) SetLightState(false);
        }
    }

    void OnEnable()
    {
        playerInputActions.Enable();
        // playerInputActions.YourLanternActionMapName.Enable(); // More specific
    }

    void OnDisable()
    {
        playerInputActions.Disable();
        // playerInputActions.YourLanternActionMapName.Disable();
    }

    void HandleInput()
    {
        // Equip/Unequip Toggle
        // Assumes your action map is "Lantern" and action is "EquipLantern"
        if (playerInputActions.Lantern.EquipLantern.WasPressedThisFrame())
        {
            ToggleEquip();
        }

        // Raise/Lower Logic
        if (isEquipped && !outOfFuel)
        {
            if (playerInputActions.Lantern.RaiseLantern.WasPressedThisFrame()) // Pressed
            {
                StartRaising();
            }
            else if (playerInputActions.Lantern.RaiseLantern.WasReleasedThisFrame()) // Released
            {
                StopRaising();
            }
            // If you want "hold" behavior (action triggered every frame it's held after initial press)
            // else if (playerInputActions.Lantern.RaiseLantern.IsPressed()) { /* Might be useful for other things */ }
        }
        else if (isRaised && playerInputActions.Lantern.RaiseLantern.WasReleasedThisFrame())
        {
            StopRaising();
        }
    }

    void ToggleEquip()
    {
        isEquipped = !isEquipped;

        if (isEquipped)
        {
            if (currentLanternInstance == null) // Instantiate if it doesn't exist
            {
                currentLanternInstance = Instantiate(lanternPrefab, lanternHoldPosition);
                currentLanternInstance.transform.localPosition = Vector3.zero;
                currentLanternInstance.transform.localRotation = Quaternion.identity;

                // Get components from the INSTANTIATED lantern
                lanternLight = currentLanternInstance.GetComponentInChildren<Light>(); // Get light from children
                if (lanternLight == null) Debug.LogError("No Light component found on instantiated lantern prefab or its children!");

                lightFlicker = currentLanternInstance.GetComponentInChildren<LightFlicker>(); // Get flicker
                if (lightFlicker == null) Debug.LogWarning("No LightFlicker component found on instantiated lantern prefab.");

                lanternSway = currentLanternInstance.GetComponent<LanternSway>();
                if (lanternSway == null) Debug.LogWarning("No LanternSway component found on instantiated lantern prefab.");
                else
                {
                    // Crucial: Set Player Camera Transform for LanternSway on the instance
                    // Assuming CameraMovement script is on Camera.main.transform or parent of LanternHoldPosition
                    if (lanternHoldPosition != null && lanternHoldPosition.parent != null)
                        lanternSway.playerCameraTransform = lanternHoldPosition.parent; // This should be the Camera's transform
                    else
                        Debug.LogError("LanternSway cannot find player camera through lanternHoldPosition.parent!");
                }
            }
            currentLanternInstance.SetActive(true); // Activate the instantiated object

            // ... rest of ToggleEquip logic for equipping ...
            // Make sure lanternLight is referenced correctly after instantiation
            lanternLight.color = lightColor; // Set color here
            outOfFuel = (currentFuel <= 0);
            // ...
        }
        else // Unequipping
        {
            if (currentLanternInstance != null)
            {
                currentLanternInstance.SetActive(false);
            }
            StopRaising();
            SetLightState(false);
            Debug.Log("Lantern Unequipped");
        }
    }

    void StartRaising()
    {
        if (!isEquipped || isRaised || outOfFuel) return;

        isRaised = true;
        SetLightState(true, raisedIntensity, raisedRange);
        Debug.Log("Lantern Raised");

        // Start checking for monster interactions periodically
        if (interactionCoroutine != null) StopCoroutine(interactionCoroutine);
        interactionCoroutine = StartCoroutine(MonsterInteractionCheck());

        // Optional: Tell sway script intensity changed / maybe trigger animation
        if (lanternSway != null) lanternSway.NotifyRaised(true);
    }

    void StopRaising()
    {
        if (!isRaised) return;

        isRaised = false;

        if (!outOfFuel)
        {
            SetLightState(true, defaultIntensity, defaultRange);
        }
        else
        {
            SetLightState(false);
        }

        Debug.Log("Lantern Lowered");

        if (interactionCoroutine != null)
        {
            StopCoroutine(interactionCoroutine);
            interactionCoroutine = null;
        }
        // Optional: Tell sway script intensity changed / maybe trigger animation
        if (lanternSway != null) lanternSway.NotifyRaised(false);
    }

    void DrainFuel(float deltaTime)
    {
        if (currentFuel <= 0) return;

        float drain = isRaised ? (passiveDrainRate + activeDrainRate) : passiveDrainRate;
        currentFuel -= drain * deltaTime;
        currentFuel = Mathf.Clamp(currentFuel, 0f, maxFuel);

        // Update UI Fuel Gauge here (e.g., call a method on a UI Manager)
        // UIManager.Instance.UpdateFuelGauge(currentFuel / maxFuel);

        if (currentFuel <= 0)
        {
            OutOfFuel();
        }
    }

    void OutOfFuel()
    {
        Debug.Log("Lantern Out of Fuel!");
        outOfFuel = true;
        SetLightState(false);

        if (isRaised) StopRaising();
    }

    public void RefillFuel()
    {
        Debug.Log("Refilling Lantern Fuel");
        currentFuel = maxFuel;
        outOfFuel = false;


        if (isEquipped)
        {
            isRaised = false;
            SetLightState(true, defaultIntensity, defaultRange);
            // Update UI
            // UIManager.Instance.UpdateFuelGauge(1f);
        }
        // Play sound effect for refill
    }

    void SetLightState(bool enabled, float intensity = 0, float range = 0)
    {
        lanternLight.enabled = enabled;
        if (enabled)
        {
            if (lightFlicker != null)
            {
                lightFlicker.SetBaseValues(intensity, range);
            }
            else
            {
                lanternLight.intensity = intensity;
                lanternLight.range = range;
            }
        }
        else if (lightFlicker != null)
        {
            lightFlicker.enabled = false;
            lanternLight.intensity = 0;
        }

        if (enabled && lightFlicker != null && !lightFlicker.enabled)
        {
            lightFlicker.enabled = true;
        }
    }


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

    // Optional: Gizmo for visualizing interaction radii in editor
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
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, hemannekenRepelRadius);
            Gizmos.DrawWireSphere(transform.position, nixieAttractRadius);
        }
    }
}