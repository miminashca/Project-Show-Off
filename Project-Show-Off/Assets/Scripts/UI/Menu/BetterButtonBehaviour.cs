using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class BetterButtonBehaviour : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Image background;
    public TMP_Text label;

    public Color normalBackgroundColor = Color.black;
    public Color normalTextColor = Color.white;
    public Color invertedBackgroundColor = Color.white;
    public Color invertedTextColor = Color.black;

    public float transitionDuration = 0.2f;

    private bool isSelected = false;
    private bool isHovered = false;

    private Coroutine colorTransitionCoroutine;

    // This method is called when the script is first loaded.
    void Start()
    {
        // On the very first load, ensure colors are set correctly.
        ResetToNormalState();
    }

    // <<< THE FIX IS HERE >>>
    // This method is called every time the GameObject is set to active.
    // This includes when you return to the main menu and the panel is re-enabled.
    void OnEnable()
    {
        // We reset the button's state to ensure it's not stuck in a hovered
        // or selected state from a previous interaction.
        ResetToNormalState();
    }

    private void ResetToNormalState()
    {
        // Stop any transition that might be running.
        if (colorTransitionCoroutine != null)
        {
            StopCoroutine(colorTransitionCoroutine);
        }

        // Reset our internal state trackers.
        isSelected = false;
        isHovered = false;

        // Instantly apply the normal colors without a transition.
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
        // The selection logic in Unity's EventSystem can be a bit aggressive.
        // It's good practice to also explicitly select the button on hover
        // for better keyboard/controller navigation feel.
        EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (!isSelected)
            TransitionColors(normalBackgroundColor, normalTextColor);
    }

    private void TransitionColors(Color targetBackgroundColor, Color targetTextColor)
    {
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
            if (background)
                background.color = Color.Lerp(startBackgroundColor, targetBackgroundColor, t);
            if (label)
                label.color = Color.Lerp(startTextColor, targetTextColor, t);

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        if (background) background.color = targetBackgroundColor;
        if (label) label.color = targetTextColor;
    }
}