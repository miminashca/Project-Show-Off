using UnityEngine;
using UnityEngine.UI;


public class FuelSlider : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider FuelBar;
    [SerializeField] private Image LerpBar;

    [Header("References")]
    [SerializeField] private LanternController lanternController;

    [Header("Settings")]
    [Tooltip("How quickly the bar animates when fuel is GAINED.")]
    [SerializeField] private float fillSpeed = 5f;

    [Tooltip("How quickly the delayed bar animates when fuel is DRAINED.")]
    [SerializeField] private float drainSpeed = 3f;

    private void OnEnable()
    {
        // --- This is where the magic happens ---
        // We subscribe our UpdateFuelBar method to the lantern's OnFuelChanged event.
        if (lanternController != null)
        {
            lanternController.OnFuelChanged += HandleFuelChanged;
        }
    }

    private void OnDisable()
    {
        // --- Crucial for preventing errors and memory leaks ---
        // We unsubscribe when this UI object is disabled or destroyed.
        if (lanternController != null)
        {
            lanternController.OnFuelChanged -= HandleFuelChanged;
        }
    }
    private void Start()
    {
        if (lanternController == null || FuelBar == null || LerpBar == null)
        {
            Debug.LogError("FuelSlider is missing required references!", this);
            return;
        }

        // Initialize both bars to the starting fuel value instantly.
        // Note: This requires your LanternController to have public properties for its fuel.
        float initialFuel = lanternController.currentFuel;
        float maxFuel = lanternController.maxFuel;

        FuelBar.maxValue = maxFuel;
        FuelBar.value = initialFuel;

        LerpBar.fillAmount = initialFuel / maxFuel;
    }

    private void Update()
    {
        // Safety check
        if (FuelBar == null || LerpBar == null) return;

        // --- THE CORE ANIMATION LOGIC ---

        // Case 1: Fuel was GAINED.
        // The LerpBar is now at the target, and the main FuelBar needs to catch up.
        if (FuelBar.value < LerpBar.fillAmount * FuelBar.maxValue)
        {
            FuelBar.value = Mathf.Lerp(FuelBar.value, LerpBar.fillAmount * FuelBar.maxValue, fillSpeed * Time.deltaTime);
        }

        // Case 2: Fuel was DRAINED.
        // The main FuelBar is now at the target, and the LerpBar needs to catch up.
        if (LerpBar.fillAmount > FuelBar.normalizedValue)
        {
            LerpBar.fillAmount = Mathf.Lerp(LerpBar.fillAmount, FuelBar.normalizedValue, drainSpeed * Time.deltaTime);
        }
    }
    /// <summary>
    /// This method is called automatically by the OnFuelChanged event.
    /// It updates the slider's max and current values.
    /// </summary>
    /// <param name="current">The lantern's current fuel level.</param>
    /// <param name="max">The lantern's maximum fuel capacity.</param>
    private void HandleFuelChanged(float current, float max)
    {
        if (FuelBar == null || LerpBar == null) return;

        // Ensure the max value is set correctly first.
        if (FuelBar.maxValue != max)
        {
            FuelBar.maxValue = max;
        }

        // Check if fuel was lost or gained by comparing with the current slider value
        if (current < FuelBar.value)
        {
            // --- FUEL DECREASED ---
            // Instantly update the main slider's value.
            // The LerpBar will catch up in the Update() method.
            FuelBar.value = current;
        }
        else if (current > FuelBar.value)
        {
            // --- FUEL INCREASED ---
            // Instantly update the LerpBar's fill amount.
            // The main FuelBar will catch up in the Update() method.
            LerpBar.fillAmount = current / max;
        }
    }
}
