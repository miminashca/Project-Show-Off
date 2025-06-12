using UnityEngine;

public class WindTriggerZone : MonoBehaviour
{
    private Collider _collider;

    void Awake()
    {
        _collider = GetComponent<Collider>();
        if (_collider == null)
        {
            Debug.LogError($"WindTriggerZone on '{gameObject.name}' is missing a Collider component.", this);
        }
        else if (!_collider.isTrigger)
        {
            Debug.LogWarning($"WindTriggerZone on '{gameObject.name}'s Collider is not set to 'Is Trigger'. Player detection might not work.", this);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Make sure it's the player entering
        if (other.CompareTag("Player")) // Ensure your player GameObject has the "Player" tag
        {
            if (WindController.Instance != null)
            {
                WindController.Instance.PlayerEnteredWindZone();
                // Debug.Log($"Player entered wind zone: {gameObject.name}");
            }
            else
            {
                Debug.LogWarning("WindController.Instance is not found in the scene.", this);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Make sure it's the player exiting
        if (other.CompareTag("Player"))
        {
            if (WindController.Instance != null)
            {
                WindController.Instance.PlayerExitedWindZone();
                // Debug.Log($"Player exited wind zone: {gameObject.name}");
            }
            else
            {
                Debug.LogWarning("WindController.Instance is not found in the scene.", this);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (_collider == null) _collider = GetComponent<Collider>();
        if (_collider == null || !_collider.isTrigger) return;

        Gizmos.color = new Color(0.8f, 0.8f, 1f, 0.3f); // Light blueish
        Gizmos.DrawCube(_collider.bounds.center, _collider.bounds.size);
        Gizmos.color = new Color(0.8f, 0.8f, 1f, 0.7f);
        Gizmos.DrawWireCube(_collider.bounds.center, _collider.bounds.size);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position, "Wind Trigger Zone");
#endif
    }
}