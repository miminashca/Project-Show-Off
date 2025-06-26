using UnityEngine;

public class PauseScreen : MonoBehaviour
{

    [SerializeField] private GameObject pauseScreen;

    CharacterController playerMovment;
    CameraMovement cameraMovment;
    PlayerInput input;
    HeadbobController bob;

    private void Start()
    {
       playerMovment = FindFirstObjectByType<CharacterController>();
       input = new PlayerInput();
       cameraMovment = FindFirstObjectByType<CameraMovement>();
        bob = FindFirstObjectByType<HeadbobController>();
    }
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
            bob.enabled = false; // Disable headbob when paused
            cameraMovment.enabled = false;
            playerMovment.enabled = false;
            input.Disable();
            

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

            
            playerMovment.enabled = true;
            input.Enable();
            cameraMovment.enabled = true;
            bob.enabled = true; // Re-enable headbob when unpaused

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
