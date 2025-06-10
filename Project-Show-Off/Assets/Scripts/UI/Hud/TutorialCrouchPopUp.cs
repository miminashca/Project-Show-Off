using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class TutorialCrouchPopUp : MonoBehaviour
{
    [SerializeField] private GameObject creouchPanel;
    [SerializeField] private GameObject triggerArea;
    [SerializeField] private CanvasGroup crouchCanvas;
    private bool isCrouching = false;
    private bool isFading = false;

    private void Awake()
    {
        //creouchPanel.SetActive(false);
        crouchCanvas.alpha = 0f; 
       
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player" && !isCrouching)
        {
            StartCoroutine(FadeCanvasGroup(crouchCanvas, 0f, 1f, 0.5f));
            Debug.Log("triggered");
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl)) && !isCrouching)
        {
            isCrouching = true;
            StartCoroutine(FadeCanvasGroup(crouchCanvas, 1f, 0f, 0.5f));
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && !isCrouching)
        {
            StartCoroutine(FadeCanvasGroup(crouchCanvas, 1f, 0f, 0.5f));
        }
    }
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
    {
        isFading = true;
        float elapsed = 0f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        cg.alpha = end;

        if (end == 0f)
        {
            cg.interactable = false;
            cg.blocksRaycasts = false;
            cg.gameObject.SetActive(false);
        }

        isFading = false;
    }

}
