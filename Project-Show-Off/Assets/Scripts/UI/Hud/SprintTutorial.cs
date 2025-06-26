using UnityEngine;
using System.Collections;

public class SprintTutorial : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject SprintTutorialUI;
    [SerializeField] private CanvasGroup SprintCanvas;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float displayDuration = 5f;
    private bool hasBeenShown = false;

    private void Awake()
    {
        if (SprintTutorialUI != null)
        {
            SprintTutorialUI.SetActive(false);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasBeenShown)
        {
            SprintTutorialUI.SetActive(true);
            StartCoroutine(FadeCanvasGroup(SprintCanvas, 0f, 1f, fadeDuration));
            StartCoroutine(HideAfterDelay(displayDuration));
            Debug.Log("Sprint tutorial triggered");
        }
    }
    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return FadeCanvasGroup(SprintCanvas, 1f, 0f, fadeDuration);
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
