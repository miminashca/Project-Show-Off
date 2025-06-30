using UnityEngine;
using System.Collections;

public class FeedbackController : MonoBehaviour
{
    // Note: FMOD logic is templated. Your sound designer will need the FMOD for Unity integration.
    // using FMOD.Studio; // Add this when FMOD is integrated

    [Header("Component References")]
    [SerializeField] private Camera playerCamera;
    // [SerializeField] private PlayerHealth playerHealth; // Assign your player health script
    // [SerializeField] private LanternController lanternController; // Assign your lantern script
    [SerializeField] private GameObject breathVFXPrefab;

    private LadyAIConfig _config;
    private Coroutine _fovCoroutine;
    private float _originalFOV;
    private GameObject _currentBreathVFX;
    // private EventInstance _creepingAudioInstance; // FMOD instance
    // private EventInstance _breathAudioInstance; // FMOD instance


    private void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        _originalFOV = playerCamera.fieldOfView;
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

        // Req 3.1.1: Player Breath SFX & VFX
        // PlayFMODEvent(ref _breathAudioInstance, _config.playerBreathAudioEvent);
        if (breathVFXPrefab != null && _currentBreathVFX == null)
        {
            _currentBreathVFX = Instantiate(breathVFXPrefab, playerCamera.transform.position + playerCamera.transform.forward, playerCamera.transform.rotation, playerCamera.transform);
        }

        // Req 3.1.1: Lantern Flicker
        // lanternController?.StartSorrowfulFlicker();
    }

    public void StartSeenEffects(Transform target)
    {
        if (_config == null) return;
        Debug.Log("Feedback: Starting Seen effects.");

        // Req 3.2.1: FOV Constriction
        if (_fovCoroutine != null) StopCoroutine(_fovCoroutine);
        _fovCoroutine = StartCoroutine(ChangeFOV(true));

        // Req 3.2.1: Visual Distortion (placeholder)
        // You would enable your screen-space shader effect here.
    }
    
    public void UpdateSeenEffects(Transform target)
    {
        if (_config == null) return;
        
        // Req 3.2.1: Camera Zoom/Magnetism
        Vector3 directionToTarget = (target.position - playerCamera.transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        playerCamera.transform.rotation = Quaternion.Slerp(playerCamera.transform.rotation, targetRotation, Time.deltaTime * _config.cameraPullStrength);
    }

    public void StopAllEffects()
    {
        Debug.Log("Feedback: Stopping all effects.");
        if (_config == null) return;
        
        // Stop SFX
        // StopFMODEvent(ref _breathAudioInstance);
        
        // Remove VFX
        if (_currentBreathVFX != null)
        {
            Destroy(_currentBreathVFX);
        }

        // Revert FOV
        if (_fovCoroutine != null) StopCoroutine(_fovCoroutine);
        _fovCoroutine = StartCoroutine(ChangeFOV(false));
        
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
        // playerHealth?.Die();
        // You would trigger your game over sequence here.
    }

    // --- HELPER COROUTINES & METHODS ---

    private IEnumerator ChangeFOV(bool constrict)
    {
        float targetFOV = constrict ? _config.minFovValue : _originalFOV;
        float startingFOV = playerCamera.fieldOfView;
        float duration = _config.fovTransitionDuration;
        float elapsed = 0f;

        // Calculate elapsed time based on current FOV to ensure smooth transitions if interrupted
        float progress = Mathf.InverseLerp(startingFOV, targetFOV, playerCamera.fieldOfView);
        elapsed = duration * progress;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            playerCamera.fieldOfView = Mathf.Lerp(startingFOV, targetFOV, t);
            yield return null;
        }

        playerCamera.fieldOfView = targetFOV;
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