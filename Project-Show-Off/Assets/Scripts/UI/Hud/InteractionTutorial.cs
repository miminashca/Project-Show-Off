using UnityEngine;
using System.Collections;

public class InteractionTutorial : MonoBehaviour
{
    [Header("Interaction Tutorial Settings")]
    [SerializeField] private GameObject interactionTutorialUI;
    [SerializeField] private CanvasGroup interactionCanvas;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float displayDuration = 5f;

    private Coroutine hideCoroutine;
    private bool hasBeenShown = false;

    private void Awake()
    {
        if (interactionTutorialUI != null)
        {
            interactionTutorialUI.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasBeenShown)
        {
            if (hideCoroutine != null) StopCoroutine(hideCoroutine);

            interactionTutorialUI.SetActive(true);
            StartCoroutine(FadeCanvasGroup(interactionCanvas, 0f, 1f, fadeDuration));
            hideCoroutine = StartCoroutine(HideAfterDelay(displayDuration));

            Debug.Log("Tutorial triggered");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && !hasBeenShown)
        {
            if (hideCoroutine != null) StopCoroutine(hideCoroutine);
            hideCoroutine = null;

            StartCoroutine(FadeCanvasGroup(interactionCanvas, 1f, 0f, fadeDuration));
            Debug.Log("Tutorial exited");
        }
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return FadeCanvasGroup(interactionCanvas, 1f, 0f, fadeDuration);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
    {
        float elapsed = 0f;

        if (end > 0f)
        {
            cg.gameObject.SetActive(true);
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

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

            // Prevent re-showing the tutorial
            hasBeenShown = true;
        }
    }
}
