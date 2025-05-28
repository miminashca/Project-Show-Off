// PlayerHealth.cs (Placeholder)
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int CurrentHealth = 100;

    public void TakeDamage(int amount)
    {
        CurrentHealth -= amount;
        Debug.Log($"Player took {amount} damage. Current health: {CurrentHealth}");
        if (CurrentHealth <= 0)
        {
            Debug.Log("Player has died!");
            // Handle player death (e.g., GameManager.Instance.PlayerDied())
        }
    }
}