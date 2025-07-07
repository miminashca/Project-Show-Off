using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class FeedbackController : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Camera playerCamera;
    [Tooltip("Reference to the player's CameraMovement script.")]
    [SerializeField] private CameraMovement cameraMovement;
    [SerializeField] private PlayerHealth playerHealth;
    
    [Header("Post-Processing")]
    [Tooltip("The Post Process Volume to control for fear effects.")]
    [SerializeField] private Volume ladyPostProcessingVolume;
    [Tooltip("How long (in seconds) the post-processing effects take to transition in or out.")]
    [SerializeField] private float postProcessTransitionDuration = 5.0f;
    
    [Header("Post-Processing Fear Values")]
    [Tooltip("Target intensity for the Vignette effect when seen.")]
    [Range(0f, 1f)]
    [SerializeField] private float targetVignetteIntensity = 0.6f;
    [Tooltip("Target intensity for the Film Grain effect when seen.")]
    [Range(0f, 1f)]
    [SerializeField] private float targetFilmGrainIntensity = 0.8f;
    [Tooltip("Target contrast for Color Adjustments when seen.")]
    [Range(-100f, 100f)]
    [SerializeField] private float targetContrast = -25f;
    [Tooltip("Target saturation for Color Adjustments when seen.")]
    [Range(-100f, 100f)]
    [SerializeField] private float targetSaturation = -50f;
    [Tooltip("Target intensity for the Chromatic Aberration effect when seen.")]
    [Range(0f, 1f)]
    [SerializeField] private float targetChromaticAberrationIntensity = 0.6f;

    private LadyAIConfig _config;
    private Coroutine _fovRestoreCoroutine;
    private Coroutine _postProcessingCoroutine;
    private float _originalFOV;
    
    private bool _isFovEffectActive = false;
    private Transform _fovTarget;
    private float _fovEffectTimer;
    private float _initialDollySize; // The starting perceived size of the target
    private float _targetDollySize;  // The final perceived size after shrinking
    
    // --- Post-Processing Effect References ---
    private Vignette _vignette;
    private FilmGrain _filmGrain;
    private ColorAdjustments _colorAdjustments;
    private ChromaticAberration _chromaticAberration;

    // --- Original Post-Processing Values ---
    private float _originalVignetteIntensity;
    private float _originalFilmGrainIntensity;
    private float _originalContrast;
    private float _originalSaturation;
    private float _originalChromaticAberrationIntensity;

    private void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        _originalFOV = playerCamera.fieldOfView;
        
        CacheOriginalPostProcessingValues();
    }

    private void Update()
    {
        // If the combined FOV effect is active, run its logic every frame.
        if (_isFovEffectActive && _fovTarget != null && _config != null)
        {
            ApplyShrinkingZoomAndResistance();
        }
    }

    public void Initialize(LadyAIConfig config)
    {
        _config = config;
    }
    
    // --- Control the camera pull ---
    public void SetGazePullActive(bool isActive)
    {
        if (cameraMovement != null)
        {
            cameraMovement.IsGazePullActive = isActive;
        }
    }

    public void StartSeenEffects(Transform target)
    {
        if (_config == null) return;
        Debug.Log("Feedback: Starting Seen effects (Combined Shrink & Dolly Zoom).");

        if (_fovRestoreCoroutine != null) StopCoroutine(_fovRestoreCoroutine);

        _fovTarget = target;
        _isFovEffectActive = true;
        _fovEffectTimer = 0f;

        float initialDistance = Vector3.Distance(playerCamera.transform.position, target.position);
        _initialDollySize = initialDistance * Mathf.Tan(playerCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        _targetDollySize = _initialDollySize * _config.dollyZoomTargetScale;
        
        SetGazePullActive(true);
        
        StartSeenVFX();
    }
    public void StopAllEffects()
    {
        Debug.Log("Feedback: Stopping all effects.");
        if (_config == null) return;
        
        // IMPORTANT: Deactivate the pull effect when the encounter ends.
        SetGazePullActive(false);

        _isFovEffectActive = false;

        if (_fovRestoreCoroutine != null) StopCoroutine(_fovRestoreCoroutine);
        _fovRestoreCoroutine = StartCoroutine(RestoreFovCoroutine());
        
        ResetSeenVFX();
    }

    // --- PLAYER STATE CHANGES ---

    public void InflictHealthDamage()
    {
        if (_config == null) return;
        Debug.Log($"Feedback: Inflicting {_config.gazeDamageAmount} damage.");
    }

    public void KillPlayer()
    {
        Debug.Log("Feedback: Player has been killed by the gaze.");
        playerHealth?.Die();
    }
    
    // --- FOV Effect Logic ---
    
    /// <summary>
    /// Called every frame to calculate and apply the shrinking dolly zoom and the intensifying camera pull.
    /// </summary>
    private void ApplyShrinkingZoomAndResistance()
    {
        float t = Mathf.Clamp01(_fovEffectTimer / _config.fovTransitionDuration);
        _fovEffectTimer += Time.deltaTime;
    
        // --- FOV LOGIC (UNCHANGED) ---
        float currentDollySize = Mathf.Lerp(_initialDollySize, _targetDollySize, t);
        float currentDistance = Vector3.Distance(playerCamera.transform.position, _fovTarget.position);
        if (currentDistance > 0.1f)
        {
            float targetFov = 2.0f * Mathf.Atan(currentDollySize / currentDistance) * Mathf.Rad2Deg;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, Time.deltaTime * _config.dynamicFovSmoothing);
        }

        // --- CAMERA RESISTANCE LOGIC (USING NEW LERP FACTOR) ---
        float currentPullLerp = Mathf.Lerp(_config.minCameraPullLerp, _config.maxCameraPullLerp, t);
        Vector3 directionToTarget = (_fovTarget.position - playerCamera.transform.position).normalized;

        // Provide the data to the CameraMovement script. It will handle the rest.
        if (cameraMovement != null)
        {
            cameraMovement.UpdateGazeData(directionToTarget, currentPullLerp);
        }
    }
    
    // --- Post Processing ---
    /// <summary>
    /// Starts the transition to the "fear" post-processing settings.
    /// </summary>
    public void StartSeenVFX()
    {
        if (ladyPostProcessingVolume == null) return;

        // Stop any previous coroutine to prevent conflicts
        if (_postProcessingCoroutine != null)
        {
            StopCoroutine(_postProcessingCoroutine);
        }
        // Start the new transition to activate the effects
        _postProcessingCoroutine = StartCoroutine(TransitionPostProcessing(true));
    }

    /// <summary>
    /// Starts the transition back to the normal post-processing settings.
    /// </summary>
    public void ResetSeenVFX()
    {
        if (ladyPostProcessingVolume == null) return;

        if (_postProcessingCoroutine != null)
        {
            StopCoroutine(_postProcessingCoroutine);
        }
        // Start the new transition to deactivate the effects
        _postProcessingCoroutine = StartCoroutine(TransitionPostProcessing(false));
    }
    

    // --- HELPER COROUTINE ---

    /// <summary>
    /// Coroutine to smoothly restore the camera's FOV to its original state. Unchanged.
    /// </summary>
    private IEnumerator RestoreFovCoroutine()
    {
        // This remains unchanged.
        float startingFOV = playerCamera.fieldOfView;
        float duration = 2.0f; 
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            playerCamera.fieldOfView = Mathf.Lerp(startingFOV, _originalFOV, elapsed / duration);
            yield return null;
        }
        playerCamera.fieldOfView = _originalFOV;
    }
    
    private void CacheOriginalPostProcessingValues()
    {
        if (ladyPostProcessingVolume == null || ladyPostProcessingVolume.profile == null)
        {
            Debug.LogWarning("FeedbackController: No Volume assigned. VFX will be disabled.");
            return;
        }

        // Use TryGet<T>() which is the correct method for HDRP Volumes
        ladyPostProcessingVolume.profile.TryGet(out _vignette);
        ladyPostProcessingVolume.profile.TryGet(out _filmGrain);
        ladyPostProcessingVolume.profile.TryGet(out _colorAdjustments);
        ladyPostProcessingVolume.profile.TryGet(out _chromaticAberration);

        // Store the default values so we can return to them
        if (_vignette != null) _originalVignetteIntensity = _vignette.intensity.value;
        if (_filmGrain != null) _originalFilmGrainIntensity = _filmGrain.intensity.value;
        if (_colorAdjustments != null)
        {
            _originalContrast = _colorAdjustments.contrast.value;
            _originalSaturation = _colorAdjustments.saturation.value;
        }
        if (_chromaticAberration != null) _originalChromaticAberrationIntensity = _chromaticAberration.intensity.value;
    }

    private IEnumerator TransitionPostProcessing(bool activate)
    {
        // Ensure we have valid references before starting
        if (_vignette == null || _filmGrain == null || _colorAdjustments == null)
        {
            Debug.LogError("One or more Post Processing effects are missing from the Volume Profile!");
            yield break;
        }

        float timer = 0f;

        // In HDRP, we usually control the effect by changing the volume's 'weight' or the effect's parameters.
        // We will animate the parameters directly.
        // We must also ensure the effect is active to be modified.
        if (activate)
        {
            _vignette.active = true;
            _filmGrain.active = true;
            _colorAdjustments.active = true;
            _chromaticAberration.active = true;
        }

        float startVignette = _vignette.intensity.value;
        float startGrain = _filmGrain.intensity.value;
        float startContrast = _colorAdjustments.contrast.value;
        float startSaturation = _colorAdjustments.saturation.value;
        float startChromaticAberration = _chromaticAberration.intensity.value;

        float endVignette = activate ? targetVignetteIntensity : _originalVignetteIntensity;
        float endGrain = activate ? targetFilmGrainIntensity : _originalFilmGrainIntensity;
        float endContrast = activate ? targetContrast : _originalContrast;
        float endSaturation = activate ? targetSaturation : _originalSaturation;
        float endChromaticAberration = activate ? targetChromaticAberrationIntensity : _originalChromaticAberrationIntensity;

        while (timer < postProcessTransitionDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / postProcessTransitionDuration);

            // Directly set the .value of the parameter
            _vignette.intensity.value = Mathf.Lerp(startVignette, endVignette, progress);
            _filmGrain.intensity.value = Mathf.Lerp(startGrain, endGrain, progress);
            _colorAdjustments.contrast.value = Mathf.Lerp(startContrast, endContrast, progress);
            _colorAdjustments.saturation.value = Mathf.Lerp(startSaturation, endSaturation, progress);
            _chromaticAberration.intensity.value = Mathf.Lerp(startChromaticAberration, endChromaticAberration, progress);

            yield return null;
        }

        // Ensure the final values are set exactly
        _vignette.intensity.value = endVignette;
        _filmGrain.intensity.value = endGrain;
        _colorAdjustments.contrast.value = endContrast;
        _colorAdjustments.saturation.value = endSaturation;
        _chromaticAberration.intensity.value = endChromaticAberration;

        // If deactivating, you can optionally set the effects back to inactive
        if (!activate)
        {
            // This is optional. Leaving them active at 0 intensity is usually fine.
            // _lensDistortion.active = false;
            // _chromaticAberration.active = false;
        }
    }
}