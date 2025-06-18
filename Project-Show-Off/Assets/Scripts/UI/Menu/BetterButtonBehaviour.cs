using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class BetterButtonBehaviour : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Image background;
    public TMP_Text label;

    public Color normalBackgroundColor = Color.black;
    public Color normalTextColor = Color.white;
    public Color invertedBackgroundColor = Color.white;
    public Color invertedTextColor = Color.black;

    private bool isSelected = false;
    private bool isHovered = false;

    void Start()
    {
        SetNormalColors();
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        SetInvertedColors();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        if (!isHovered)
            SetNormalColors();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        SetInvertedColors();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (!isSelected)
            SetNormalColors();
    }

    private void SetNormalColors()
    {
        if (background) background.color = normalBackgroundColor;
        if (label) label.color = normalTextColor;
    }

    private void SetInvertedColors()
    {
        if (background) background.color = invertedBackgroundColor;
        if (label) label.color = invertedTextColor;
    }
}