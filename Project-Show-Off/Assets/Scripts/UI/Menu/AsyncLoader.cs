using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;



public class AsyncLoader : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private GameObject loadingScene;
    [SerializeField] private GameObject menuScene;
    [Header("UI Elements")]
    [SerializeField] private Slider loadingSlider;

    public void LoadSceneButton(string sceneName)
    {
        if (menuScene)
        {
            menuScene.SetActive(false);
            loadingScene.SetActive(true);
        }
        
        StartCoroutine(LoadLevelAsync(sceneName));
    }
    //Run async coroutine to load the scene
    IEnumerator LoadLevelAsync(string sceneName)
    {
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false; // Prevent the scene from activating immediately
            while (asyncLoad.progress < 0.9f)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f); // Normalize progress to 0-1 range
                loadingSlider.value = progress; // Update the slider value
                yield return null; // Wait for the next frame

            }
        yield return new WaitForSeconds(0.5f);
        //Debug.LogError("Loading complete, activating scene now.");
        asyncLoad.allowSceneActivation = true;
    }

}
