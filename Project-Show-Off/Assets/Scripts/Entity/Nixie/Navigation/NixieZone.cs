using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NixieZone : MonoBehaviour
{
    private Collider _collider;

    void Awake()
    {
        _collider = GetComponent<Collider>();
        if (_collider == null)
        {
            Debug.LogError($"NixieZone on '{gameObject.name}' is missing a Collider component.", this);
        }
        else if (!_collider.isTrigger)
        {
            Debug.LogWarning($"NixieZone on '{gameObject.name}'s Collider is not set to 'Is Trigger'. Player detection will not work.", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStatus playerStatus = other.GetComponent<PlayerStatus>();
            if (playerStatus != null)
            {
                // Tell the player they are now in THIS specific Nixie zone.
                playerStatus.CurrentNixieZone = this;
                Debug.Log($"Player entered NixieZone: {gameObject.name}");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStatus playerStatus = other.GetComponent<PlayerStatus>();
            // Only clear the zone if the player is exiting THIS zone.
            // This prevents issues if zones overlap.
            if (playerStatus != null && playerStatus.CurrentNixieZone == this)
            {
                playerStatus.CurrentNixieZone = null;
                Debug.Log($"Player exited NixieZone: {gameObject.name}");
            }
        }
    }

    void OnDrawGizmos()
    {
        if (_collider == null) _collider = GetComponent<Collider>();
        if (_collider == null) return;

        // Draw a semi-transparent green box to represent the zone's bounds.
        Gizmos.color = new Color(0.1f, 0.8f, 0.2f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;

        if (_collider is BoxCollider box)
        {
            Gizmos.DrawCube(box.center, box.size);
        }
        else if (_collider is SphereCollider sphere)
        {
            Gizmos.DrawSphere(sphere.center, sphere.radius);
        }
    }
}