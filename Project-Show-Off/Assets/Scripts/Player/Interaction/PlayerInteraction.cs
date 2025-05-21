using UnityEngine;
using UnityEngine.InputSystem; // For PlayerInput

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactableLayer; // Set this layer on your clue objects
    [SerializeField] private Transform cameraTransform; // Assign your player camera transform

    [Header("UI")]
    [SerializeField] private GameObject interactionPromptUI; // Optional: A small UI element (e.g., "Press E to Interact")

    private PlayerInput playerInputActions; // Your generated Input Action Asset class
    private ClueObject currentInteractableClue;
    private ClueObject lastHighlightedClue;


    void Awake()
    {
        playerInputActions = new PlayerInput(); // Or YourPlayerControlsClassName
        if (cameraTransform == null)
        {
            Camera cam = Camera.main; // Prefer Camera.main if it's tagged
            if (cam != null) cameraTransform = cam.transform;
            else
            {
                cam = GetComponentInChildren<Camera>(); // Fallback
                if (cam != null) cameraTransform = cam.transform;
                else Debug.LogError("PlayerInteraction: Camera Transform not found or assigned!");
            }
        }

        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
    }

    private void OnEnable()
    {
        playerInputActions.Enable();
        // This Interact is for INITIATING interaction from the world
        playerInputActions.Player.Interact.performed += TryInitiateInteraction;
    }

    private void OnDisable()
    {
        playerInputActions.Player.Interact.performed -= TryInitiateInteraction;
        playerInputActions.Disable();

        if (lastHighlightedClue != null)
        {
            lastHighlightedClue.Highlight(false);
            lastHighlightedClue = null;
        }
        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
        currentInteractableClue = null;
    }

    void Update()
    {
        CheckForInteractable();
    }

    private void CheckForInteractable()
    {
        if (InspectionManager.Instance != null && InspectionManager.Instance.IsInspecting())
        {
            if (lastHighlightedClue != null)
            {
                lastHighlightedClue.Highlight(false);
                SetInteractionPrompt(false); // Hide prompt if inspecting
                lastHighlightedClue = null;
            }
            currentInteractableClue = null;
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, interactionDistance, interactableLayer))
        {
            ClueObject clue = hit.collider.GetComponent<ClueObject>();
            if (clue != null && clue.IsInteractable())
            {
                currentInteractableClue = clue;
                if (lastHighlightedClue != currentInteractableClue)
                {
                    if (lastHighlightedClue != null) lastHighlightedClue.Highlight(false);
                    currentInteractableClue.Highlight(true);
                    lastHighlightedClue = currentInteractableClue;
                }
                SetInteractionPrompt(true);
            }
            else // Hit something on interactable layer, but not a ClueObject or not interactable
            {
                ClearCurrentInteractable();
            }
        }
        else // Raycast hit nothing
        {
            ClearCurrentInteractable();
        }
    }

    private void ClearCurrentInteractable()
    {
        if (lastHighlightedClue != null)
        {
            lastHighlightedClue.Highlight(false);
            lastHighlightedClue = null;
        }
        currentInteractableClue = null;
        SetInteractionPrompt(false);
    }

    private void TryInitiateInteraction(InputAction.CallbackContext context)
    {
        // This is called when the player presses the "Interact" key in the world.
        // If InspectionManager is already inspecting, its own input handling will take over.
        if (InspectionManager.Instance != null && InspectionManager.Instance.IsInspecting())
        {
            return; // Let InspectionManager handle its inputs
        }

        if (currentInteractableClue != null)
        {
            Debug.Log("PlayerInteraction: Interacting with " + currentInteractableClue.clueName);
            if (InspectionManager.Instance != null)
            {
                InspectionManager.Instance.StartInspection(currentInteractableClue);
                ClearCurrentInteractable(); // Hide prompt and clear current after starting inspection
            }
            else
            {
                Debug.LogError("InspectionManager instance not found!");
            }
        }
    }

    private void SetInteractionPrompt(bool show)
    {
        if (interactionPromptUI != null)
        {
            interactionPromptUI.SetActive(show);
        }
    }
}