using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    private float _currentMinIntensityMultiplier = 0.95f;
    private float _currentMaxIntensityMultiplier = 1.05f;
    private float _currentMinRangeMultiplier = 0.98f;
    private float _currentMaxRangeMultiplier = 1.02f;
    private float _currentFlickerSpeed = 1f;

    // Internal State
    private Light targetLight;
    private float baseIntensity;
    private float baseRange;
    private float randomOffset; // To ensure multiple flickers aren't identical

    void Awake()
    {
        targetLight = GetComponent<Light>();
        randomOffset = Random.Range(0f, 1000f);
    }

    /// <summary>
    /// Sets the base intensity and range the flicker will modulate.
    /// Called by LanternController when the light state (e.g., raised/lowered) changes.
    /// </summary>
    public void SetBaseValues(float intensity, float range)
    {
        baseIntensity = intensity;
        baseRange = range;
    }


    /// <summary>
    /// Updates the parameters that control the flicker's behavior (speed, intensity).
    /// Called by LanternController for subtle flicker or intense Nixie flicker.
    /// </summary>
    public void UpdateFlickerParameters(float speed, float minIntensity, float maxIntensity)
    {
        _currentFlickerSpeed = speed;
        _currentMinIntensityMultiplier = minIntensity;
        _currentMaxIntensityMultiplier = maxIntensity;
        // You could also add min/max range here if you want Nixies to affect range too
    }

    void Update()
    {
        if (baseIntensity <= 0 || !targetLight.enabled) return;

        float timeInput = (Time.time + randomOffset) * _currentFlickerSpeed;
        float intensityNoise = Mathf.PerlinNoise(timeInput, timeInput * 0.3f);
        float rangeNoise = Mathf.PerlinNoise(timeInput * 0.7f, timeInput);

        targetLight.intensity = baseIntensity * Mathf.Lerp(_currentMinIntensityMultiplier, _currentMaxIntensityMultiplier, intensityNoise);
        targetLight.range = baseRange * Mathf.Lerp(_currentMinRangeMultiplier, _currentMaxRangeMultiplier, rangeNoise);
    }
}