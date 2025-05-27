using UnityEngine;

public class HemannekenVisuals : MonoBehaviour
{
    [SerializeField] private GameObject hemannekenTrueModelPrefab;
    [SerializeField] private GameObject hemannekenRabbitModelPrefab;
    
    private GameObject _currentModelInstance;
    private ParticleSystem _particleSystem; // Assuming one main particle system

    public bool IsTrueForm { get; private set; }

    public void Initialize()
    {
        _particleSystem = GetComponentInChildren<ParticleSystem>(); // Or assign explicitly
        if (_particleSystem == null)
        {
            Debug.LogWarning("HemannekenVisuals: No ParticleSystem found in children.", this);
        }
    }

    public void SetForm(bool isTrue, Transform parentTransform)
    {
        IsTrueForm = isTrue;
        GameObject prefabToInstantiate = IsTrueForm ? hemannekenTrueModelPrefab : hemannekenRabbitModelPrefab;

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
    
    public void PlayStunEffects() 
    { 
        Debug.Log("SFX/VFX: Hemanneken Stunned"); 
        if (_particleSystem != null) _particleSystem.Play(); // Example
    }
    public void StopStunEffects() 
    { 
        Debug.Log("SFX/VFX: Hemanneken Stun Effects Stopped"); 
        if (_particleSystem != null) _particleSystem.Stop(); // Example
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