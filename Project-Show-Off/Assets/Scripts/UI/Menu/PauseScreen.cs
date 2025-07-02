using UnityEngine;

// This script's only job is to respond to UI button clicks on the Pause Screen.
// It calls the appropriate methods in the central GameManager.
public class PauseScreen : MonoBehaviour
{
    // This method should be linked to the "Resume" button's OnClick() event in the Inspector.
    public void OnResumeClicked()
    {
        // Tell the GameManager to resume the game.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }
    }

    // This method should be linked to the "Main Menu" button's OnClick() event.
    public void OnMainMenuClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GoToMainMenu();
        }
    }

    // This method should be linked to the "Quit" button's OnClick() event.
    public void OnQuitClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
    }
}