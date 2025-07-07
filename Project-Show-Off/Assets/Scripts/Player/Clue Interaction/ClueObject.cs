using System;
using UnityEngine;
using UnityEngine.VFX;

public class ClueObject : MonoBehaviour
{
    public enum InteractableType { Clue, Note }

    [Header("Clue Properties")]
    public InteractableType objectType = InteractableType.Clue; // Default to Clue
    public string clueID;
    public string clueName = "Mysterious Object";
    [TextArea]
    public string clueDescription = "An interesting object worth inspecting.";

    [Header("Inspection Settings")]
    public Vector3 inspectionRotationOffset = Vector3.zero;
    public float inspectionScaleFactor = 1f;

    [Header("Effects")]
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 1f);
    [SerializeField] private FirefliesVfxController fireflies;
    [Tooltip("Assign the ProximityLight component here. Only used if Object Type is 'Note'.")]
    [SerializeField] private ProximityLight proximityLight;

    private bool isInteractable = true;
    private Renderer objectRenderer;
    private Color originalColor;

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
    void Start()
    {
        if (objectType == InteractableType.Clue && proximityLight != null)
        {
            // Clues should not have a proximity light. Disable it.
            proximityLight.SetEnabled(false);
            proximityLight.enabled = false;
        }

        if (ClueEventManager.Instance != null)
        {
            ClueEventManager.Instance.OnGameDataLoaded += CheckStatusOnLoad;
        }
        else
        {
            Debug.LogError($"ClueObject '{clueID}' cannot subscribe to load event because ClueEventManager.Instance is null.");
        }

        if (ClueEventManager.Instance.IsClueCollected(clueID) || ClueEventManager.Instance.IsClueSubmitted(clueID))
        {
            Destroy(gameObject);
        }
    }

    public void SetProximityLightActive(bool state)
    {
        if (proximityLight != null)
        {
            proximityLight.SetEnabled(state);
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

    public void Highlight(bool shouldHighlight)
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

    private void OnDestroy()
    {
        if(fireflies) fireflies.TurnOff();
        
        if (ClueEventManager.Instance != null)
        {
            ClueEventManager.Instance.OnGameDataLoaded -= CheckStatusOnLoad;
        }
    }
    
    // This method will be called after the GameManager has loaded the data.
    private void CheckStatusOnLoad()
    {
        // It's possible this object was destroyed by other means before the event fired.
        if (!this) return; 

        if (ClueEventManager.Instance.IsClueCollected(clueID) || ClueEventManager.Instance.IsClueSubmitted(clueID))
        {
            Debug.Log($"ClueObject '{clueID}' is already processed. Destroying this instance post-load.");
            Destroy(gameObject);
        }
    }
}