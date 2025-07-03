using UnityEngine;
using System.Collections;

public class FeedbackController : MonoBehaviour
{
    // Note: FMOD logic is templated. Your sound designer will need the FMOD for Unity integration.
    // using FMOD.Studio; // Add this when FMOD is integrated

    [Header("Component References")]
    [SerializeField] private Camera playerCamera;
    [Tooltip("Reference to the player's CameraMovement script.")]
    [SerializeField] private CameraMovement cameraMovement;
    [SerializeField] private PlayerHealth playerHealth; // Assign your player health script
    // [SerializeField] private LanternController lanternController; // Assign your lantern script
    [SerializeField] private GameObject breathVFXPrefab;

    private LadyAIConfig _config;
    private Coroutine _fovRestoreCoroutine;
    private float _originalFOV;
    private GameObject _currentBreathVFX;
    // private EventInstance _breathAudioInstance; // FMOD instance

    // --- NEW: Variables for Combined Dolly Zoom & Shrink Effect ---
    private bool _isFovEffectActive = false;
    private Transform _fovTarget;
    private float _fovEffectTimer;
    private float _initialDollySize; // The starting perceived size of the target
    private float _targetDollySize;  // The final perceived size after shrinking
    
    private void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        _originalFOV = playerCamera.fieldOfView;
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

    // --- EFFECT TRIGGERS ---

    public void StartCreepingEffects()
    {
        if (_config == null) return;
        Debug.Log("Feedback: Starting Creeping effects.");

        // Player Breath SFX & VFX
        // PlayFMODEvent(ref _breathAudioInstance, _config.playerBreathAudioEvent);
        if (breathVFXPrefab != null && _currentBreathVFX == null)
        {
            _currentBreathVFX = Instantiate(breathVFXPrefab, playerCamera.transform.position + playerCamera.transform.forward, playerCamera.transform.rotation, playerCamera.transform);
        }

        // Lantern Flicker
        // lanternController?.StartSorrowfulFlicker();
    }
    
    // --- NEW: Method to control the camera pull ---
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

        // IMPORTANT: Activate the pull effect when the state begins.
        SetGazePullActive(true);
    }
    public void StopAllEffects()
    {
        Debug.Log("Feedback: Stopping all effects.");
        if (_config == null) return;
        
        // IMPORTANT: Deactivate the pull effect when the encounter ends.
        SetGazePullActive(false);

        _isFovEffectActive = false;
        if (_currentBreathVFX != null) Destroy(_currentBreathVFX);

        if (_fovRestoreCoroutine != null) StopCoroutine(_fovRestoreCoroutine);
        _fovRestoreCoroutine = StartCoroutine(RestoreFovCoroutine());
        
        // Stop other effects
        // lanternController?.ReturnToNormalFlicker();
        // Disable screen-space shader effect here.
    }

    // --- PLAYER STATE CHANGES ---

    public void InflictHealthDamage()
    {
        if (_config == null) return;
        Debug.Log($"Feedback: Inflicting {_config.gazeDamageAmount} damage.");
        // PlayFMODEvent(_config.gazeDamageAudioEvent);
        // playerHealth?.TakeDamage(_config.gazeDamageAmount);
    }

    public void KillPlayer()
    {
        Debug.Log("Feedback: Player has been killed by the gaze.");
        playerHealth?.Die();
        // You would trigger your game over sequence here.
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
    
    
    // --- FMOD TEMPLATES ---
    /*
    private void PlayFMODEvent(ref EventInstance instance, string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        instance = FMODUnity.RuntimeManager.CreateInstance(path);
        instance.start();
    }
    
    private void PlayFMODEvent(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        FMODUnity.RuntimeManager.PlayOneShot(path, transform.position);
    }

    private void StopFMODEvent(ref EventInstance instance)
    {
        instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        instance.release();
    }
    */
}