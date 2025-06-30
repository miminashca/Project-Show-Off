using UnityEngine;
using System.Collections;
public class IntroMessage : MonoBehaviour
{
    [SerializeField] private CanvasGroup BackgroundImage;
    [SerializeField] private CanvasGroup MessageText;

    [Header("Timing Settings")]
    [Tooltip("Delay before the sequence starts.")]
    [SerializeField] private float startDelay = 1f;
    [Tooltip("How long it takes for the text to fade in.")]
    [SerializeField] private float textFadeInDuration = 1.5f;
    [Tooltip("How long the text stays fully visible on screen.")]
    [SerializeField] private float displayDuration = 3f;
    [Tooltip("How long it takes for the text to fade out.")]
    [SerializeField] private float textFadeOutDuration = 1.5f;
    [Tooltip("How long it takes for the background to fade out after the text is gone.")]
    [SerializeField] private float backgroundFadeOutDuration = 2f;

    private bool isFading = false;

    private void Awake()
    {
        if (BackgroundImage == null || MessageText == null)
        {
            Debug.LogError("IntroMessage: CanvasGroup references are not assigned!", this);
            enabled = false;
            return;
        }
        BackgroundImage.alpha = 1f;
        MessageText.alpha = 0f;
    }
    private void Start()
    {
        // Start the main sequence coroutine
        StartCoroutine(ShowIntroSequence());
    }

    private IEnumerator ShowIntroSequence()
    {
        // 1. Initial delay before anything happens
        yield return new WaitForSeconds(startDelay);

        // 2. Fade in the message text
        Debug.Log("Fading in text...");
        yield return StartCoroutine(FadeCanvasGroup(MessageText, 0f, 1f, textFadeInDuration));

        // 3. Wait for the specified display duration
        Debug.Log("Displaying text...");
        yield return new WaitForSeconds(displayDuration);

        // 4. Fade out the message text
        Debug.Log("Fading out text...");
        yield return StartCoroutine(FadeCanvasGroup(MessageText, 1f, 0f, textFadeOutDuration));

        // 5. Fade out the background image
        Debug.Log("Fading out background...");
        yield return StartCoroutine(FadeCanvasGroup(BackgroundImage, 1f, 0f, backgroundFadeOutDuration));

        Debug.Log("Intro sequence complete.");
    }




    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
    {
        // Prevent multiple fades on the same object
        while (isFading)
        {
            yield return null;
        }
        isFading = true;

        float elapsed = 0f;

        // If fading in, make sure the object is active and interactable
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

        // Ensure the final alpha value is set
        cg.alpha = end;

        // If faded out, disable the object for performance
        if (end == 0f)
        {
            cg.interactable = false;
            cg.blocksRaycasts = false;
            cg.gameObject.SetActive(false);
        }

        isFading = false;
    }

}
