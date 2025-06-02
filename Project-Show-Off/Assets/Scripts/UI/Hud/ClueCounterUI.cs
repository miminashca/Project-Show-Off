using UnityEngine;
using TMPro;
using System.Collections; //new code
//end of new code

public class ClueCounterUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI clueCounterText;

    //new code
    [SerializeField] private CanvasGroup clueCanvasGroup;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float visibleDuration = 0.5f;

    private Coroutine fadeRoutine;
    //end of new code

    //new code
    private void Awake()
    {
        if (clueCanvasGroup != null)
        {
            clueCanvasGroup.alpha = 0f;
            clueCanvasGroup.gameObject.SetActive(false);
        }
    }
    //end of new code

    private void Start()
    {
        if (clueCounterText == null)
        {
            Debug.LogError("ClueCounterUI: TextMeshProUGUI reference is missing!");
            return;
        }

        if (InspectionManager.Instance != null)
        {
            InspectionManager.Instance.OnClueCollected += UpdateClueCounter;
        }

        UpdateClueCounter(0);
    }

    private void OnDestroy()
    {
        if (InspectionManager.Instance != null)
        {
            InspectionManager.Instance.OnClueCollected -= UpdateClueCounter;
        }
    }

    private void UpdateClueCounter(int count)
    {
        clueCounterText.text = $"{count}";

        //new code
        if (clueCanvasGroup != null)
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeCounterRoutine());
        }
        //end of new code
    }

    //new code
    private IEnumerator FadeCounterRoutine()
    {
        clueCanvasGroup.gameObject.SetActive(true);
        yield return StartCoroutine(FadeCanvasGroup(clueCanvasGroup, true));
        yield return new WaitForSeconds(visibleDuration);
        yield return StartCoroutine(FadeCanvasGroup(clueCanvasGroup, false));
        clueCanvasGroup.gameObject.SetActive(false);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, bool fadeIn)
    {
        float startAlpha = canvasGroup.alpha;
        float endAlpha = fadeIn ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Mathf.Min(Time.deltaTime, fadeDuration);
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
    }
    //end of new code
}
