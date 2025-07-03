using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
   public void StartGame()
   {
      // Logic to start the game
      Debug.Log("Starting Game...");
      SceneManager.LoadScene("Environment Scene"); 
    }
    public void NewGame()
    {
        //new code to start a new game
        // Logic to start a new game
        //Debug.Log("Starting New Game...");
        //SceneManager.LoadScene("Environment Scene");
    }
    public void LoadGame()
    {
        // Logic to load a saved game
        Debug.Log("Loading Game...");
        // Here you would typically load the saved game data
        // For example, you might use PlayerPrefs or a custom save system
    }

    public void QuitGame()
    {
              // Logic to quit the game
      Debug.Log("Quitting Game...");
      Application.Quit();
    }
    
}
