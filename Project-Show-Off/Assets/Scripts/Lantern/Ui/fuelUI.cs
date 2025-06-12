using UnityEngine;
using UnityEngine.UI;

public class FuelUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider FuelBar;

    [Header("References")]
    [SerializeField] private LanternController lanternController;

    private void OnEnable()
    {
        // --- This is where the magic happens ---
        // We subscribe our UpdateFuelBar method to the lantern's OnFuelChanged event.
        if (lanternController != null)
        {
            lanternController.OnFuelChanged += UpdateFuelBar;
        }
    }

    private void OnDisable()
    {
        // --- Crucial for preventing errors and memory leaks ---
        // We unsubscribe when this UI object is disabled or destroyed.
        if (lanternController != null)
        {
            lanternController.OnFuelChanged -= UpdateFuelBar;
        }
    }

    /// <summary>
    /// This method is called automatically by the OnFuelChanged event.
    /// It updates the slider's max and current values.
    /// </summary>
    /// <param name="current">The lantern's current fuel level.</param>
    /// <param name="max">The lantern's maximum fuel capacity.</param>
    private void UpdateFuelBar(float current, float max)
    {
        if (FuelBar == null) return;

        // Ensure the max value is set correctly, in case it changes.
        if (FuelBar.maxValue != max)
        {
            FuelBar.maxValue = max;
        }

        FuelBar.value = current;
    }

    // The Start() and Update() methods are no longer needed for this functionality.
}