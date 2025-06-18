// PlayerShoutController.cs
using UnityEngine;

public class PlayerShoutController : MonoBehaviour
{
    private PlayerInput controls;
    private Transform playerTransform; // To get the shout position

    //NEW CHANGEFMOD
    private PlayerHeySound playerHeySoundController;
    //END CHANGEFMOD

    void Awake()
    {
        controls = new PlayerInput(); // Assuming PlayerInput is generated and accessible
        playerTransform = transform; // Assuming this script is on the player

        //NEW CHANGEFMOD
        playerHeySoundController = GetComponent<PlayerHeySound>();
        //END CHANGEFMOD
    }

    void OnEnable()
    {
        controls.Enable();
        controls.Player.Enable();
    }

    void OnDisable()
    {
        controls.Disable();
        controls.Player.Disable();
    }

    void Update()
    {
        if (controls.Player.Hey.triggered)
        {
            PerformShout();
        }
    }

    void PerformShout()
    {
        if (playerTransform == null)
        {
            Debug.LogError("PlayerShoutController: PlayerTransform is null!", this);
            return;
        }

        Vector3 shoutPosition = playerTransform.position;
        Debug.Log($"Player shouted at: {shoutPosition}");

        // Broadcast the shout event with position to any interested listeners (Hunter, Hemanneken)
        PlayerActionEventBus.PlayerShouted(shoutPosition);

        //NEW CHANGEFMOD
        if (playerHeySoundController != null)
        {
            playerHeySoundController.PlayPlayerHeySound();
        }
        //END CHANGEFMOD

    }
}