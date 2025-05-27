using System.Collections;
using UnityEngine;

public class HemannekenVisuals : MonoBehaviour
{
    [Header("Models")]
    [SerializeField] private GameObject _hemannekenTrueModelPrefab;
    [SerializeField] private GameObject _hemannekenRabbitModelPrefab;
    
    // Existing variables (example)
    [Header("Effects")]
    [SerializeField] private ParticleSystem _particleSystem; // Assign in Inspector

    // --- ADD THESE NEW VARIABLES FOR STUN RETREAT ---
    [Header("Stun Retreat Behavior")]
    [Tooltip("How far the entity will move backwards when stunned.")]
    [SerializeField] private float _stunRetreatDistance = 0.5f;
    [Tooltip("How long the backward retreat movement will take.")]
    [SerializeField] private float _stunRetreatDuration = 0.3f;
    [Tooltip("Optional: Small upward movement for a 'jump back' feel.")]
    [SerializeField] private float _stunRetreatUpwardOffset = 0.1f;
    
    private GameObject _currentModelInstance;
    private Coroutine _activeRetreatCoroutine; // To manage an ongoing retreat
    private PlayerSensor _playerSensor; // To manage an ongoing retreat
    public bool IsTrueForm { get; private set; }

    public void Initialize()
    {
        _playerSensor = GetComponent<PlayerSensor>();
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
    public void PlayStunEffects()
    {
        Debug.Log("SFX/VFX: " + gameObject.name + " Stunned");
        if (_particleSystem != null) _particleSystem.Play();
        else{Debug.Log("Particle system is null!");}

        // If a retreat is already in progress, stop it to start the new one
        if (_activeRetreatCoroutine != null)
        {
            StopCoroutine(_activeRetreatCoroutine);
        }
        else{Debug.Log("Coroutine is null!");}

        // Start the scared retreat movement
        _activeRetreatCoroutine = StartCoroutine(StunRetreatCoroutine());
    }
    public void StopStunEffects()
    {
        if (_particleSystem != null) _particleSystem.Play();

        if (_activeRetreatCoroutine != null)
        {
            StopCoroutine(_activeRetreatCoroutine);
        }
    }

    // --- ADD THIS NEW COROUTINE METHOD TO YOUR CLASS ---
    private IEnumerator StunRetreatCoroutine()
    {
        if (!_playerSensor)
        {
            Debug.Log("Player sensor is null!");
            yield break;
        }
        
        Vector3 startPosition = transform.position;
        // Calculate the direction: directly backwards from current facing direction
        Vector3 backwardDirection = startPosition - _playerSensor.PlayerTransform.position;

        // Incorporate the optional upward offset for a "jump back" feel
        Vector3 retreatDirection = (backwardDirection.normalized + Vector3.up * _stunRetreatUpwardOffset).normalized;
        // If no upward offset, just pure backward movement
        if (Mathf.Approximately(_stunRetreatUpwardOffset, 0f))
        {
            retreatDirection = backwardDirection.normalized;
        }

        Vector3 targetPosition = startPosition + retreatDirection * _stunRetreatDistance;

        float elapsedTime = 0f;
        while (elapsedTime < _stunRetreatDuration)
        {
            // Interpolate position smoothly over time
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / _stunRetreatDuration);
            elapsedTime += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // Ensure the entity reaches the exact target position
        transform.position = targetPosition;
        _activeRetreatCoroutine = null; // Mark coroutine as finished
    }
    // public void StopStunEffects() 
    // { 
    //     Debug.Log("SFX/VFX: Hemanneken Stun Effects Stopped"); 
    //     if (_particleSystem != null) _particleSystem.Stop(); // Example
    // }

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