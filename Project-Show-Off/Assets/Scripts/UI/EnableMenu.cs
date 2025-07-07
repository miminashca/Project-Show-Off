
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class EnableMenu : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel; //enable this panel on start scene
    [SerializeField] private Canvas loadingCanvas; //disable this canvas in environment scene

    private void EnableMenuTab()
    {
        EventSystem.current.SetSelectedGameObject(null);
        menuPanel.SetActive(true);
    }
    private void DisableLoadingCanvas()
    {
        
       if(loadingCanvas) loadingCanvas.enabled = false;
        
    }

    private void OnSceneLoad(Scene scene, LoadSceneMode mode)
    {
        if(scene.name == "StartScreen") //replace with your start scene name
        {
            EnableMenuTab();
        }
        else if (scene.name == "Environment Scene") //replace with your environment scene name
        {
            DisableLoadingCanvas();
        }
    }
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoad;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoad;
    }
    

}
