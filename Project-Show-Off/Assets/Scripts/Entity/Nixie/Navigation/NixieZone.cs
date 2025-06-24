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
            return;
        }

        if (!_collider.isTrigger)
        {
            Debug.LogWarning($"NixieZone on '{gameObject.name}'s Collider is not set to 'Is Trigger'. Player detection will not work correctly.", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStatus playerStatus = other.GetComponent<PlayerStatus>();
            if (playerStatus != null)
            {
                // Tell the player's status component that it is now inside THIS specific zone.
                playerStatus.CurrentNixieZone = this;
                Debug.Log($"Player entered Nixie's territory: {gameObject.name}. AI can now react.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // This logic is equally ESSENTIAL
        if (other.CompareTag("Player"))
        {
            PlayerStatus playerStatus = other.GetComponent<PlayerStatus>();
            // Only clear the zone if the player is exiting THIS specific zone.
            // This prevents bugs if zones overlap.
            if (playerStatus != null && playerStatus.CurrentNixieZone == this)
            {
                playerStatus.CurrentNixieZone = null;
                Debug.Log($"Player exited Nixie's territory: {gameObject.name}. AI will revert to staring/roaming.");
            }
        }
    }

    void OnDrawGizmos()
    {
        if (_collider == null) _collider = GetComponent<Collider>();
        if (_collider == null) return;

        Gizmos.color = new Color(0.8f, 0.1f, 0.2f, 0.25f); // Changed color to red to distinguish from other zones
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