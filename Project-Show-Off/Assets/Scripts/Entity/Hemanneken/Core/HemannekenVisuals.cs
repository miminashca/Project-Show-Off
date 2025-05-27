using System.Collections;
using UnityEngine;

public class HemannekenVisuals : MonoBehaviour
{
    [Header("Models")]
    [SerializeField] private GameObject _hemannekenTrueModelPrefab;
    [SerializeField] private GameObject _hemannekenRabbitModelPrefab;
    
    // Existing variables (example)
    //[Header("Effects")]
    private ParticleSystem _particleSystem; // Assign in Inspector

    // --- ADD THESE NEW VARIABLES FOR STUN RETREAT ---
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
    
    private Coroutine _activeStunBehaviorCoroutine;
    private bool _isStunBehaviorActive = false; // Flag to control the coroutine's loops
    
    private GameObject _currentModelInstance;
    private PlayerSensor _playerSensor; // To manage an ongoing retreat
    public bool IsTrueForm { get; private set; }

    public void Initialize()
    {
        _playerSensor = GetComponent<PlayerSensor>();
        _particleSystem = GetComponentInChildren<ParticleSystem>();
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
            // A more robust way would be to iterate through all MeshRenderers in _currentModelInstance
            // and toggle their enabled state. For simplicity, toggling the parent.
            // This assumes the model's root contains all renderers or controls them.
            _currentModelInstance.SetActive(visible);
        }
    }
    
    // public void PlayStunEffects() 
    // { 
    //     Debug.Log("SFX/VFX: Hemanneken Stunned"); 
    //     if (_particleSystem != null) _particleSystem.Play(); // Example
    // }
    // Call this to start the stun visual effects and the retreat/circling behavior
    public void StartStunEffectsAndBehavior()
    {
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

            // Calculate horizontal offsets (XZ plane)
            float xOffset = Mathf.Cos(currentCircleAngle * Mathf.Deg2Rad) * _stunCircleRadius;
            float zOffset = Mathf.Sin(currentCircleAngle * Mathf.Deg2Rad) * _stunCircleRadius;

            // --- MODIFIED: Calculate vertical offset (Y axis) ---
            // Using Sin for vertical to make it potentially out of phase with Cos on X, leading to a more "rolling" circle.
            // You can use Cos here as well, or use a different angle (e.g., currentCircleAngle + 90)
            // or apply a _stunVerticalFrequencyMultiplier to currentCircleAngle if you want different speeds.
            float yOffset = Mathf.Sin(currentCircleAngle * Mathf.Deg2Rad /* * _stunVerticalFrequencyMultiplier (if using) */) * _stunVerticalRadius;
            // --- END OF MODIFICATION ---

            Vector3 nextCirclePosition = circleCenter + new Vector3(xOffset, yOffset, zOffset);

            transform.position = Vector3.Lerp(transform.position, nextCirclePosition, Time.deltaTime * 10f);

            yield return null;
        }

        Debug.Log(gameObject.name + " circling phase ended because _isStunBehaviorActive is false.");
        _activeStunBehaviorCoroutine = null;
    }

    public void PlayReplyHeySound() { Debug.Log("SFX: Hemanneken replies 'Hey'"); /* Implement sound */ }

    public void PlayTransformationEffects()
    {
        Debug.Log("SFX/VFX: Hemanneken Transforming");
        if (_particleSystem != null) _particleSystem.Play();
    }
    public void StopTransformationEffects() 
    { 
        Debug.Log("SFX/VFX: Hemanneken Transformation Effects Stopped"); 
        if (_particleSystem != null) _particleSystem.Stop();
    }

    public void PlayDeathEffects()
    {
        Debug.Log("SFX/VFX: Hemanneken Dying");
        if (_particleSystem != null) _particleSystem.Play();
    }
}