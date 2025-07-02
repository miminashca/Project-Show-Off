using UnityEngine;

public class DeathScreen : MonoBehaviour
{
    public void OnRetryClicked()
    {
        GameManager.Instance.Retry();
    }

    public void OnMainMenuClicked()
    {
        GameManager.Instance.GoToMainMenu();
    }

    public void OnQuitClicked()
    {
        GameManager.Instance.QuitGame();
    }
}