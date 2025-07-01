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
    // [SerializeField, Range(0.1f, 10f)] private float damageFlashDuration = 5.0f; // REMOVED: No longer needed
    [SerializeField, Range(1f, 20f)] private float fadeSpeed = 5f;

    private Vignette vignette;
    private bool isChokeActive = false;
    // private float damageFlashTimer = 0f; // REMOVED: No longer needed

    // --- ADDED: Wound level tracking ---
    private PlayerHealth playerHealth;
    private int currentWoundLevel = 0;
    private int maxWoundLevel = 3;
    // ---

    private Color targetColor;
    private float targetIntensity;

    private void Awake()
    {
        // --- ADDED: Cache PlayerHealth and get initial state ---
        playerHealth = GetComponent<PlayerHealth>();
        currentWoundLevel = playerHealth.CurrentWoundLevel;
        maxWoundLevel = playerHealth.MaxWoundLevel > 0 ? playerHealth.MaxWoundLevel : 1; // Avoid division by zero
        // ---

        if (postProcessingVolume == null)
        {
            Debug.LogError("PlayerVignetteController: Post Processing Volume is not assigned!", this);
            enabled = false;
            return;
        }

        if (!postProcessingVolume.profile.TryGet(out vignette))
        {
            Debug.LogError("PlayerVignetteController: Vignette component not found on the assigned Volume's Profile!", this);
            enabled = false;
            return;
        }

        vignette.intensity.value = 0f;
        vignette.active = false;
    }

    private void OnEnable()
    {
        // Subscribe to events
        HemannekenEventBus.OnHemannekenAttached += StartChokeVignette;
        HemannekenEventBus.OnHemannekenDetached += StopChokeVignette;
        // MODIFIED: Changed event subscription to get wound level data
        PlayerHealth.OnWoundLevelChanged += UpdateWoundVignette;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        HemannekenEventBus.OnHemannekenAttached -= StartChokeVignette;
        HemannekenEventBus.OnHemannekenDetached -= StopChokeVignette;
        // MODIFIED: Unsubscribe from the new event
        PlayerHealth.OnWoundLevelChanged -= UpdateWoundVignette;

        if (vignette != null)
        {
            vignette.intensity.value = 0f;
            vignette.active = false;
        }
    }

    private void Update()
    {
        // REMOVED: Damage flash timer countdown
        // if (damageFlashTimer > 0)
        // {
        //     damageFlashTimer -= Time.deltaTime;
        // }

        // MODIFIED: Determine target state based on wound level, then choke status.
        if (currentWoundLevel > 0)
        {
            targetColor = damageColor;
            // Calculate intensity based on how wounded the player is
            float woundRatio = (float)currentWoundLevel / maxWoundLevel;
            targetIntensity = Mathf.Clamp(woundRatio * maxIntensity, 0f, maxIntensity);
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

    // RENAMED & MODIFIED: This is now the handler for wound level changes
    private void UpdateWoundVignette(int currentWounds, int maxWounds)
    {
        currentWoundLevel = currentWounds;
        // Update max wounds in case it can change, and prevent division by zero
        maxWoundLevel = maxWounds > 0 ? maxWounds : 1;
    }
}