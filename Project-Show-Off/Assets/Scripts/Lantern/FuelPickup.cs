using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FuelPickup : MonoBehaviour
{
    void Start()
    {
        // Ensure the collider is set to be a trigger
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning($"FuelPickup on {gameObject.name} needs its collider set to 'Is Trigger'. Setting it now.", this);
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger is the Player
        if (other.CompareTag("Player")) // Make sure your player GameObject has the "Player" tag
        {
            LanternController lantern = other.GetComponent<LanternController>();
            if (lantern != null)
            {
                Debug.Log("Player picked up fuel.");
                lantern.RefillFuel();

                // Optional: Play pickup sound
                // AudioSource.PlayClipAtPoint(pickupSound, transform.position);

                // Destroy the fuel bottle object
                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning($"Player tagged object entered FuelPickup trigger, but no LanternController found on {other.name}.", other);
            }
        }
    }
}