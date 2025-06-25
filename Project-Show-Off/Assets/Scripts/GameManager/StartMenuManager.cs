// StartMenuManager.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartMenuManager : MonoBehaviour
{
    [Header("Scene to Load")]
    [SerializeField] private string gameSceneName = "YourGameSceneName"; // IMPORTANT: Set this in the Inspector!

    [Header("UI Elements")]
    [SerializeField] private Button continueButton;

    private void Start()
    {
        // Check if a save file exists. The GameManager holds the key.
        bool saveExists = PlayerPrefs.HasKey("gameSaveData");
        
        if (continueButton != null)
        {
            // The "Continue" button should only be clickable if there is a save file.
            continueButton.interactable = saveExists;
        }
        else
        {
            Debug.LogError("Continue Button is not assigned in the StartMenuManager inspector!");
        }
    }

    public void OnClickNewGame()
    {
        // Tell the persistent GameManager our choice
        GameManager.Instance.SetStartState(GameStartState.NewGame);
        
        // Load the main game scene
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnClickContinue()
    {
        // Tell the persistent GameManager our choice
        GameManager.Instance.SetStartState(GameStartState.Continue);

        // Load the main game scene
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnClickQuit()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}