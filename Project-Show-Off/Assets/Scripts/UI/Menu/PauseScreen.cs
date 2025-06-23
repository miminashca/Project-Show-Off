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
        bool isPaused = !pauseScreen.activeSelf;
        pauseScreen.SetActive(isPaused);

        if (isPaused)
        {
            // Show and unlock the cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Optional: Pause time
            Time.timeScale = 0f;
        }
        else
        {
            // Hide and lock the cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Optional: Resume time
            Time.timeScale = 1f;
        }
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
