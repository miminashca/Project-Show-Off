using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    [Header("Flicker Settings")]
    [Tooltip("Minimum multiplier for base intensity")]
    public float minIntensityMultiplier = 0.8f;
    [Tooltip("Maximum multiplier for base intensity")]
    public float maxIntensityMultiplier = 1.2f;

    [Tooltip("Minimum multiplier for base range")]
    public float minRangeMultiplier = 0.9f;
    [Tooltip("Maximum multiplier for base range")]
    public float maxRangeMultiplier = 1.1f;

    [Tooltip("How fast the flickering noise changes")]
    public float flickerSpeed = 5f;

    // Internal State
    private Light targetLight;
    private float baseIntensity;
    private float baseRange;
    private float randomOffset; // To ensure multiple flickers aren't identical

    void Awake()
    {
        targetLight = GetComponent<Light>();
        // Initialize base values from the light's current settings
        // These will be overwritten by LanternController::SetLightState
        baseIntensity = targetLight.intensity;
        baseRange = targetLight.range;
        // Add a random offset to the time used in Perlin noise
        randomOffset = Random.Range(0f, 1000f);
    }

    // Call this from LanternController when changing light states
    public void SetBaseValues(float intensity, float range)
    {
        baseIntensity = intensity;
        baseRange = range;
        // Make sure the light component has roughly correct starting values
        // though the Update loop will quickly take over.
        targetLight.intensity = intensity;
        targetLight.range = range;
    }


    void Update()
    {
        if (baseIntensity <= 0) return; // Don't flicker if base intensity is zero (light off)

        // Use Perlin noise for smoother, more natural flickering
        float timeInput = (Time.time + randomOffset) * flickerSpeed;
        float intensityNoise = Mathf.PerlinNoise(timeInput, timeInput * 0.3f); // 2D noise for more variation
        float rangeNoise = Mathf.PerlinNoise(timeInput * 0.7f, timeInput);    // Use slightly different inputs

        // Map noise (0-1 range) to our desired multiplier range
        targetLight.intensity = baseIntensity * Mathf.Lerp(minIntensityMultiplier, maxIntensityMultiplier, intensityNoise);
        targetLight.range = baseRange * Mathf.Lerp(minRangeMultiplier, maxRangeMultiplier, rangeNoise);
    }
}