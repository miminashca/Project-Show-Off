using UnityEngine;
using FMODUnity; // Required for FMOD EventReference and RuntimeManager

public class PlayerHeySound : MonoBehaviour
{
    [Header("FMOD Event - Player")]
    [SerializeField]
    [Tooltip("The FMOD Event for the player's 'Hey!' shout.")]
    private EventReference playerHeyEvent;

    // This method can be called from your player's input or shout logic script.
    public void PlayPlayerHeySound()
    {
        if (!playerHeyEvent.IsNull)
        {
            // Play the sound attached to this GameObject (the Player).
            // This ensures the sound originates from the player's position in 3D space.
            RuntimeManager.PlayOneShotAttached(playerHeyEvent, gameObject);
        }
        else
        {
            Debug.LogWarning("PlayerHeySound: 'playerHeyEvent' FMOD Event is not assigned. Cannot play sound.");
        }
    }
}