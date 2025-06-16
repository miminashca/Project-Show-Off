// PlayerTestInput.cs
using UnityEngine;

public class PlayerTestInput : MonoBehaviour
{
    private PlayerStatus playerStatus;

    void Awake()
    {
        playerStatus = GetComponent<PlayerStatus>();
    }

    void Update()
    {
        // Press 'L' to toggle the lantern
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (playerStatus != null)
            {
                playerStatus.IsLanternOn = !playerStatus.IsLanternOn;
                Debug.Log("TEST: Player Lantern is now " + (playerStatus.IsLanternOn ? "ON" : "OFF"));
            }
        }

        // Press 'H' to perform a "HEY!" shout
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("TEST: Player shouted HEY!");
            PlayerActionEventBus.PlayerShouted(transform.position);
        }
    }
}