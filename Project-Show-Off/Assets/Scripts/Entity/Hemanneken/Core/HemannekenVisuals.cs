using System.Collections;
using UnityEngine;
using UnityEngine.VFX; // Added for VisualEffect

public class HemannekenVisuals : MonoBehaviour
{
    [Header("Models")]
    [SerializeField] private GameObject _hemannekenTrueModelPrefab;
    [SerializeField] private GameObject _hemannekenRabbitModelPrefab;

    // Particle system for general effects, initialized in Initialize()
    private ParticleSystem _particleSystem;

    [Header("Stun Retreat Behavior")]
    [Tooltip("How far the entity will move backwards when stunned.")]
    [SerializeField] private float _stunRetreatDistance = 0.5f;
    [Tooltip("How long the backward retreat movement will take.")]
    [SerializeField] private float _stunRetreatDuration = 0.3f;
    [Tooltip("Optional: Small upward movement for a 'jump back' feel.")]
    [SerializeField] private float _stunRetreatUpwardOffset = 0.1f;

    [Header("Stun Circling Behavior")]
    [Tooltip("Radius of the small circle the entity makes while stunned.")]
    [SerializeField] private float _stunCircleRadius = 0.2f;
    [Tooltip("Speed at which the entity moves around the circle (degrees per second).")]
    [SerializeField] private float _stunCircleSpeed = 90.0f; // Degrees per second
    [Tooltip("Amplitude of the vertical movement during circling. Set to 0 for no vertical motion.")]
    [SerializeField] private float _stunVerticalRadius = 0.1f;

    [Header("Death Sequence Behavior")]
    [Tooltip("Distance the entity moves in front of the player before playing death particles.")]
    [SerializeField] private float _deathMoveDistanceInFrontOfPlayer = 2.0f;
    [Tooltip("Should the entity look at the player after moving for death effects?")]
    [SerializeField] private bool _deathMoveLookAtPlayer = true;
    // Optional: If you have a specific particle system just for death, you could add:
    // [SerializeField] private ParticleSystem _deathSpecificParticleSystem;

    private Coroutine _activeStunBehaviorCoroutine;
    private bool _isStunBehaviorActive = false; // Flag to control the stun coroutine's loops

    private GameObject _currentModelInstance;
    private PlayerSensor _playerSensor; // To get player's transform
    public bool IsTrueForm { get; private set; }

    // --- NEW FLAG FOR DEATH PROCESSING ---
    private bool _isProcessingDeath = false;

    public void Initialize()
    {
        _playerSensor = GetComponent<PlayerSensor>();
        if (_playerSensor == null)
        {
            Debug.LogWarning("HemannekenVisuals: PlayerSensor component not found. Some behaviors might not work as expected.", this);
        }

        // Get the general particle system.
        // If you have multiple and need a specific one for death, assign it directly or add _deathSpecificParticleSystem.
        _particleSystem = GetComponentInChildren<ParticleSystem>();
        if (_particleSystem == null)
        {
            Debug.LogWarning("HemannekenVisuals: ParticleSystem component not found in children. Effects might not play.", this);
        }
    }

    public void SetForm(bool isTrue, Transform parentTransform)
    {
        IsTrueForm = isTrue;
        GameObject prefabToInstantiate = IsTrueForm ? _hemannekenTrueModelPrefab : _hemannekenRabbitModelPrefab;

        if (_currentModelInstance != null)
        {
            Destroy(_currentModelInstance);
        }

        if (prefabToInstantiate != null)
        {
            _currentModelInstance = Instantiate(prefabToInstantiate, parentTransform);
            _currentModelInstance.transform.localPosition = Vector3.zero;
            _currentModelInstance.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogError($"HemannekenVisuals: {(IsTrueForm ? "True" : "Rabbit")} model prefab is not assigned!", this);
        }
    }

    public void SetModelVisibility(bool visible)
    {
        if (_currentModelInstance != null)
        {
            _currentModelInstance.SetActive(visible);
        }
    }

    public void StartStunEffectsAndBehavior()
    {
        if (_isProcessingDeath) return; // Don't start stun if already dying

        Debug.Log("SFX/VFX: " + gameObject.name + " Stun Behavior Started");

        if (_particleSystem != null)
        {
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _particleSystem.Play();
        }

        if (_activeStunBehaviorCoroutine != null)
        {
            StopCoroutine(_activeStunBehaviorCoroutine);
        }

        _isStunBehaviorActive = true;
        _activeStunBehaviorCoroutine = StartCoroutine(StunBehaviorCoroutine());
    }

    public void StopStunBehavior()
    {
        if (!_isStunBehaviorActive && _activeStunBehaviorCoroutine == null)
        {
            return;
        }

        Debug.Log(gameObject.name + " Stun Behavior Stopped by external call.");
        _isStunBehaviorActive = false;

        if (_activeStunBehaviorCoroutine != null)
        {
            StopCoroutine(_activeStunBehaviorCoroutine);
            _activeStunBehaviorCoroutine = null;
        }
    }

    private IEnumerator StunBehaviorCoroutine()
    {
        // --- Phase 1: Scared Retreat ---
        if (_playerSensor == null || _playerSensor.PlayerTransform == null)
        {
            Debug.LogError("PlayerSensor or PlayerTransform is not assigned. Cannot perform stun retreat.");
            _isStunBehaviorActive = false;
            _activeStunBehaviorCoroutine = null;
            yield break;
        }

        Vector3 startPosition = transform.position;
        Vector3 playerPos = _playerSensor.PlayerTransform.position;
        Vector3 backwardDirection = (startPosition - playerPos);
        backwardDirection.y = 0;
        backwardDirection = backwardDirection.normalized;

        Vector3 retreatDirection = (backwardDirection + Vector3.up * _stunRetreatUpwardOffset).normalized;
        if (Mathf.Approximately(_stunRetreatUpwardOffset, 0f))
        {
            retreatDirection = backwardDirection;
        }
        Vector3 targetRetreatPosition = startPosition + retreatDirection * _stunRetreatDistance;

        float retreatElapsedTime = 0f;
        while (retreatElapsedTime < _stunRetreatDuration)
        {
            if (!_isStunBehaviorActive)
            {
                Debug.Log("Stun behavior stopped during retreat phase.");
                _activeStunBehaviorCoroutine = null;
                yield break;
            }
            transform.position = Vector3.Lerp(startPosition, targetRetreatPosition, retreatElapsedTime / _stunRetreatDuration);
            retreatElapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = targetRetreatPosition;

        // --- Phase 2: Circling in Place (until StopStunBehavior is called) ---
        if (!_isStunBehaviorActive)
        {
            Debug.Log("Stun behavior stopped just before circling phase.");
            _activeStunBehaviorCoroutine = null;
            yield break;
        }

        Vector3 circleCenter = transform.position;
        float currentCircleAngle = 0f;

        Debug.Log(gameObject.name + " entering 3D circling phase. Will continue until StopStunBehavior is called.");

        while (_isStunBehaviorActive)
        {
            currentCircleAngle += _stunCircleSpeed * Time.deltaTime;
            if (currentCircleAngle >= 360f) currentCircleAngle -= 360f;

            float xOffset = Mathf.Cos(currentCircleAngle * Mathf.Deg2Rad) * _stunCircleRadius;
            float zOffset = Mathf.Sin(currentCircleAngle * Mathf.Deg2Rad) * _stunCircleRadius;
            float yOffset = Mathf.Sin(currentCircleAngle * Mathf.Deg2Rad) * _stunVerticalRadius;

            Vector3 nextCirclePosition = circleCenter + new Vector3(xOffset, yOffset, zOffset);
            transform.position = Vector3.Lerp(transform.position, nextCirclePosition, Time.deltaTime * 10f); // Smooth follow
            yield return null;
        }

        Debug.Log(gameObject.name + " circling phase ended because _isStunBehaviorActive is false.");
        _activeStunBehaviorCoroutine = null;
    }

    public void PlayReplyHeySound() { Debug.Log("SFX: Hemanneken replies 'Hey'"); /* Implement sound */ }

    public void PlayTransformationEffects()
    {
        Debug.Log("SFX/VFX: Hemanneken Transforming");
        
        VisualEffect vfx = GetComponentInChildren<VisualEffect>();
        if(vfx) vfx.enabled = false;
        
        if (_particleSystem != null) _particleSystem.Play();
    }

    public void StopTransformationEffects()
    {
        Debug.Log("SFX/VFX: Hemanneken Transformation Effects Stopped");
        
        VisualEffect vfx = GetComponentInChildren<VisualEffect>();
        if(vfx) vfx.enabled = true;
        
        if (_particleSystem != null) _particleSystem.Stop();
    }

    // --- MODIFIED PlayDeathEffects ---
    // Call this with StartCoroutine(yourHemannekenVisualsInstance.PlayDeathEffects());
    public IEnumerator PlayDeathEffects(float timer)
    {
        // if (_isProcessingDeath)
        // {
        //     yield break; // Already dying or death sequence started
        // }
        // _isProcessingDeath = true;
        //
        // if (_isStunBehaviorActive || _activeStunBehaviorCoroutine != null)
        // {
        //     StopStunBehavior(); // This should stop the coroutine and reset flags
        //     yield return null;  // Wait a frame to ensure stun coroutine has fully exited
        // }
        //
        // Transform playerActualTransform = null;
        // if (_playerSensor != null && _playerSensor.PlayerTransform != null)
        // {
        //     playerActualTransform = _playerSensor.PlayerTransform;
        // }
        // else
        // {
        //     if (_particleSystem != null)
        //     {
        //         _particleSystem.Play();
        //     }
        //     // Optionally destroy after particles. For now, just mark as not processing.
        //     // Destroy(gameObject, _particleSystem != null && _particleSystem.main.Is मृत्यु ? _particleSystem.main.duration + _particleSystem.main.startLifetime.constantMax : 2f);
        //     _isProcessingDeath = false; // Or handle destruction which makes this irrelevant
        //     yield break;
        // }
        //
        // // --- 1. Calculate Target Position ---
        // Vector3 targetPosition = playerActualTransform.position + playerActualTransform.forward  * _deathMoveDistanceInFrontOfPlayer;
        // // Optional: Adjust Y position. e.g., to match entity's original height or player's height.
        // // targetPosition.y = transform.position.y; // Maintain entity's Y
        // targetPosition.y += playerActualTransform.GetComponentInChildren<Camera>().transform.position.y; // Align with player's Y
        //
        // Vector3 startPosition = transform.position;
        // float elapsedTime = 0f;
        //
        // // --- 2. Move Entity ---
        // // Optional: Hide model during the fast move for a "poof" effect if desired
        // // if (_currentModelInstance != null) _currentModelInstance.SetActive(false);
        //
        // while (elapsedTime < timer-1f)
        // {
        //     transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / (timer-1f) );
        //     elapsedTime += Time.deltaTime;
        //     yield return null; // Wait for the next frame
        // }
        //
        // if (_currentModelInstance != null) _currentModelInstance.SetActive(false);
        //
        // transform.position = targetPosition; // Ensure it's exactly at the target position

        // Optional: Show model again if hidden
        // if (_currentModelInstance != null) _currentModelInstance.SetActive(true);
        
        // --- 4. Play Particle System ---

        
        PlayTransformationEffects();
        
        Debug.Log("SFX/VFX: Hemanneken Dying (after move)", this.gameObject);
        yield break;
        // --- 5. Optional: Clean up ---
        // Usually, after death effects, the GameObject is destroyed.
        // This can be handled here, by the particle system itself (Stop Action: Destroy), or by a managing script.
        // Example:
        // float particleEffectDuration = (systemToPlay != null && systemToPlay.main.duration > 0) ?
        //                                systemToPlay.main.duration + systemToPlay.main.startLifetime.constantMax :
        //                                2f; // Default if no particles or duration is zero
        // Destroy(gameObject, particleEffectDuration);
        // If not destroying, and the entity could somehow revive, you might set _isProcessingDeath = false;
        // For a typical death, the object is removed, making _isProcessingDeath reset unnecessary.
    }
}