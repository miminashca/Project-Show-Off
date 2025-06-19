using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections; // Required for Coroutines

public class BetterButtonBehaviour : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Image background;
    public TMP_Text label;

    public Color normalBackgroundColor = Color.black;
    public Color normalTextColor = Color.white;
    public Color invertedBackgroundColor = Color.white;
    public Color invertedTextColor = Color.black;

    public float transitionDuration = 0.2f; // Duration of the color transition in seconds

    private bool isSelected = false;
    private bool isHovered = false;

    private Coroutine colorTransitionCoroutine;

    void Start()
    {
        // Ensure initial colors are set instantly at Start
        if (background) background.color = normalBackgroundColor;
        if (label) label.color = normalTextColor;
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        TransitionColors(invertedBackgroundColor, invertedTextColor);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        if (!isHovered)
            TransitionColors(normalBackgroundColor, normalTextColor);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        TransitionColors(invertedBackgroundColor, invertedTextColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (!isSelected)
            TransitionColors(normalBackgroundColor, normalTextColor);
    }

    private void TransitionColors(Color targetBackgroundColor, Color targetTextColor)
    {
        // Stop any existing transition to prevent conflicts
        if (colorTransitionCoroutine != null)
        {
            StopCoroutine(colorTransitionCoroutine);
        }
        colorTransitionCoroutine = StartCoroutine(LerpColors(targetBackgroundColor, targetTextColor));
    }

    private IEnumerator LerpColors(Color targetBackgroundColor, Color targetTextColor)
    {
        Color startBackgroundColor = background ? background.color : targetBackgroundColor;
        Color startTextColor = label ? label.color : targetTextColor;

        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            float t = elapsedTime / transitionDuration;
            // You can use different easing functions here, e.g., t = Mathf.SmoothStep(0, 1, t);
            if (background)
                background.color = Color.Lerp(startBackgroundColor, targetBackgroundColor, t);
            if (label)
                label.color = Color.Lerp(startTextColor, targetTextColor, t);

            elapsedTime += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // Ensure the colors are exactly the target colors at the end
        if (background) background.color = targetBackgroundColor;
        if (label) label.color = targetTextColor;
    }
}