using UnityEngine;

public class ClueObject : MonoBehaviour
{
    [Header("Clue Properties")]
    public string clueID; // Unique identifier for this clue
    public string clueName = "Mysterious Object"; // Display name
    [TextArea]
    public string clueDescription = "An interesting object worth inspecting.";

    public Vector3 inspectionRotationOffset = Vector3.zero;
    public float inspectionScaleFactor = 1f;

    private bool isInteractable = true;

    private Renderer objectRenderer;
    private Color originalColor;
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 1f); // A light yellow

    void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null && objectRenderer.material != null)
        {
            // Check if the material has a "_Color" property (common for URP/HDRP Lit and Unlit shaders)
            if (objectRenderer.material.HasProperty("_Color"))
            {
                originalColor = objectRenderer.material.color;
            }
            // For some shaders, the base color property might be "_BaseColor" (e.g., HDRP/Lit)
            else if (objectRenderer.material.HasProperty("_BaseColor"))
            {
                originalColor = objectRenderer.material.GetColor("_BaseColor");
            }
            else
            {
                Debug.LogWarning($"ClueObject '{gameObject.name}': Material does not have a recognized '_Color' or '_BaseColor' property. Highlighting by color change may not work as expected. Original color not stored.", this);
                // No original color to revert to, so highlighting might be one-way or problematic.
                // Consider disabling the color-based highlight for this object or using a different highlight method.
                objectRenderer = null; // Set to null so Highlight method won't try to change color
            }
        }
        else
        {
            Debug.LogWarning($"ClueObject '{gameObject.name}' is missing a Renderer or Material for highlighting.", this);
        }
    }

    public void SetInteractable(bool state)
    {
        isInteractable = state;
    }

    public bool IsInteractable()
    {
        return isInteractable;
    }

    public void Highlight(bool shouldHighlight) // Renamed parameter for clarity
    {
        if (objectRenderer != null && objectRenderer.material != null) // Ensure renderer and material are still valid
        {
            if (objectRenderer.material.HasProperty("_Color"))
            {
                objectRenderer.material.color = shouldHighlight ? highlightColor : originalColor;
            }
            else if (objectRenderer.material.HasProperty("_BaseColor")) // For HDRP/Lit or similar
            {
                objectRenderer.material.SetColor("_BaseColor", shouldHighlight ? highlightColor : originalColor);
            }
            // If neither property exists, and we didn't set objectRenderer to null in Awake,
            // this highlight method won't do anything for color, which is fine.
        }
    }

    public void OnCollected()
    {
        Debug.Log($"Clue '{clueName}' ({clueID}) collected!");
        if (ClueEventManager.Instance != null)
        {
            ClueEventManager.Instance.RegisterClueCollected(clueID);
        }
        Destroy(gameObject);
    }
}