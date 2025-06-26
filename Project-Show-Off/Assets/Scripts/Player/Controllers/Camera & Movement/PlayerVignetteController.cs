// --- START OF FILE PlayerVignetteController.cs ---

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[RequireComponent(typeof(PlayerHealth))]
public class PlayerVignetteController : MonoBehaviour
{
    [Header("Vignette Settings")]
    [Tooltip("The Post Processing Volume that contains the Vignette effect.")]
    [SerializeField] private Volume postProcessingVolume;
    [SerializeField] private Color chokeColor = new Color(0.1f, 0.2f, 0.8f); // A deep blue
    [SerializeField] private Color damageColor = new Color(0.8f, 0.1f, 0.1f); // A blood red
    [SerializeField, Range(0f, 1f)] private float maxIntensity = 0.5f;
    [SerializeField, Range(0.1f, 10f)] private float damageFlashDuration = 5.0f;
    [SerializeField, Range(1f, 20f)] private float fadeSpeed = 5f;

    private Vignette vignette;
    private bool isChokeActive = false;
    private float damageFlashTimer = 0f;

    private Color targetColor;
    private float targetIntensity;

    private void Awake()
    {
        if (postProcessingVolume == null)
        {
            Debug.LogError("PlayerVignetteController: Post Processing Volume is not assigned!", this);
            enabled = false;
            return;
        }

        // Cache the Vignette effect from the volume's profile
        if (!postProcessingVolume.profile.TryGet(out vignette))
        {
            Debug.LogError("PlayerVignetteController: Vignette component not found on the assigned Volume's Profile!", this);
            enabled = false;
            return;
        }

        // Ensure the vignette is off by default
        vignette.intensity.value = 0f;
        vignette.active = false;
    }

    private void OnEnable()
    {
        // Subscribe to events
        HemannekenEventBus.OnHemannekenAttached += StartChokeVignette;
        HemannekenEventBus.OnHemannekenDetached += StopChokeVignette;
        PlayerHealth.OnPlayerTookDamage += FlashDamageVignette;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        HemannekenEventBus.OnHemannekenAttached -= StartChokeVignette;
        HemannekenEventBus.OnHemannekenDetached -= StopChokeVignette;
        PlayerHealth.OnPlayerTookDamage -= FlashDamageVignette;

        // Clean up the vignette state when the player is disabled
        if (vignette != null)
        {
            vignette.intensity.value = 0f;
            vignette.active = false;
        }
    }

    private void Update()
    {
        // Countdown the damage flash timer
        if (damageFlashTimer > 0)
        {
            damageFlashTimer -= Time.deltaTime;
        }

        // Determine the target state based on priority (Damage > Choke)
        if (damageFlashTimer > 0)
        {
            targetColor = damageColor;
            targetIntensity = maxIntensity;
        }
        else if (isChokeActive)
        {
            targetColor = chokeColor;
            targetIntensity = maxIntensity;
        }
        else
        {
            // No active effects, so fade out
            targetIntensity = 0f;
        }

        // Smoothly interpolate towards the target values
        vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, targetIntensity, Time.deltaTime * fadeSpeed);
        vignette.color.value = Color.Lerp(vignette.color.value, targetColor, Time.deltaTime * fadeSpeed);

        // Activate or deactivate the effect to save performance.
        // The small threshold prevents it from flickering on/off when intensity is near zero.
        vignette.active = vignette.intensity.value > 0.01f;
    }

    private void StartChokeVignette()
    {
        isChokeActive = true;
    }

    private void StopChokeVignette()
    {
        isChokeActive = false;
    }

    private void FlashDamageVignette()
    {
        // Simply reset the timer. The Update loop will handle the rest.
        damageFlashTimer = damageFlashDuration;
    }
}