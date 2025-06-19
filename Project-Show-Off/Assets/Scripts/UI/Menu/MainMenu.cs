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
    public void QuitGame()
    {
              // Logic to quit the game
      Debug.Log("Quitting Game...");
      Application.Quit();
    }
    
}
