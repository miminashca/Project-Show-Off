using UnityEngine;

public class PauseScreen : MonoBehaviour
{

    [SerializeField] private GameObject pauseScreen;

    void Awake()
    {
        pauseScreen.SetActive(false); // keep the pause screen disabled by default
    }
    private void TogglePause()
    {
        // Toggle the active state
        pauseScreen.SetActive(!pauseScreen.activeSelf);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Paused");
            TogglePause();
        }
    }
}
