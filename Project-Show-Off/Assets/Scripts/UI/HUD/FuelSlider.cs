using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FuelSlider : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The main Slider component. Its value will represent the current fuel.")]
    [SerializeField] private Slider FuelBar;

    [Tooltip("The Image that lags behind, showing the change in fuel.")]
    [SerializeField] private Image LerpBar;

    [Header("References")]
    [SerializeField] private LanternController lanternController;

    [Header("Settings")]
    [Tooltip("How quickly the bar animates when fuel is GAINED.")]
    [SerializeField] private float fillSpeed = 5f;

    [Tooltip("How quickly the delayed bar animates when fuel is DRAINED.")]
    [SerializeField] private float drainSpeed = 3f;

    // References to our running coroutines so we can stop them.
    private Coroutine _fillAnimationCoroutine;
    private Coroutine _drainAnimationCoroutine;

    /// <summary>
    /// Awake runs before Start. We use it to set the initial state
    /// so the UI appears full immediately on scene load.
    /// </summary>
    private void Awake()
    {
        // FIX 1: Set the UI to be full by default when the game starts.
        if (FuelBar != null)
        {
            FuelBar.value = FuelBar.maxValue;
        }
        if (LerpBar != null)
        {
            LerpBar.fillAmount = 1f;
        }
    }

    private void OnEnable()
    {
        if (lanternController != null)
        {
            lanternController.OnFuelChanged += HandleFuelChanged;
        }
    }

    private void OnDisable()
    {
        if (lanternController != null)
        {
            lanternController.OnFuelChanged -= HandleFuelChanged;
        }
    }

    // We no longer need Update() for the animation logic.

    /// <summary>
    /// This method is called by the OnFuelChanged event.
    /// It now stops any running animations and starts the correct new one.
    /// </summary>
    private void HandleFuelChanged(float current, float max)
    {
        if (FuelBar == null || LerpBar == null) return;

        // Ensure the max value is set correctly if it ever changes mid-game.
        if (FuelBar.maxValue != max)
        {
            FuelBar.maxValue = max;
        }

        // Stop any animations that are currently running. This is crucial
        // for handling rapid changes in fuel.
        StopAllCoroutines();

        float previousFuelValue = FuelBar.value;

        // Determine if fuel was lost or gained
        if (current < previousFuelValue)
        {
            // --- FUEL DECREASED ---
            FuelBar.value = current; // Instantly update the main slider.
            // Start the coroutine to make the LerpBar catch up.
            _drainAnimationCoroutine = StartCoroutine(AnimateLerpBarDrain());
        }
        else if (current > previousFuelValue)
        {
            // --- FUEL INCREASED ---
            LerpBar.fillAmount = current / max; // Instantly update the LerpBar.
            // Start the coroutine to make the main FuelBar catch up.
            _fillAnimationCoroutine = StartCoroutine(AnimateFuelBarFill());
        }
    }

    private IEnumerator AnimateLerpBarDrain()
    {
        float targetFill = FuelBar.normalizedValue;
        // Loop until the lerp bar is close enough to the target.
        while (LerpBar.fillAmount > targetFill)
        {
            LerpBar.fillAmount = Mathf.Lerp(LerpBar.fillAmount, targetFill, drainSpeed * Time.deltaTime);
            yield return null; // Wait for the next frame
        }
        // Snap to the final value to ensure it's precise.
        LerpBar.fillAmount = targetFill;
    }

    private IEnumerator AnimateFuelBarFill()
    {
        float targetValue = LerpBar.fillAmount * FuelBar.maxValue;
        // Loop until the fuel bar is close enough to the target.
        while (FuelBar.value < targetValue)
        {
            FuelBar.value = Mathf.Lerp(FuelBar.value, targetValue, fillSpeed * Time.deltaTime);
            yield return null; // Wait for the next frame
        }
        // Snap to the final value to ensure it's precise.
        FuelBar.value = targetValue;
    }
}