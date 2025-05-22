using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private Transform cameraTransform;

    [Header("UI")]
    [SerializeField] private GameObject interactionPromptUI;
    [SerializeField] private GameObject interactionDotUI;

    private PlayerInput playerInputActions;
    private ClueObject currentInteractableClue;
    private ClueObject lastHighlightedClue;


    void Awake()
    {
        playerInputActions = new PlayerInput();
        if (cameraTransform == null)
        {
            Camera cam = Camera.main;
            if (cam != null) cameraTransform = cam.transform;
            else
            {
                cam = GetComponentInChildren<Camera>();
                if (cam != null) cameraTransform = cam.transform;
                else Debug.LogError("PlayerInteraction: Camera Transform not found or assigned!");
            }
        }

        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
        if (interactionDotUI != null) interactionDotUI.SetActive(false);
    }

    private void OnEnable()
    {
        playerInputActions.Player.Enable();
        playerInputActions.Player.Interact.performed += TryInitiateInteraction;
    }

    private void OnDisable()
    {
        playerInputActions.Player.Interact.performed -= TryInitiateInteraction;
        playerInputActions.Player.Disable();

        if (lastHighlightedClue != null)
        {
            lastHighlightedClue.Highlight(false);
            lastHighlightedClue = null;
        }
        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
        if (interactionDotUI != null) interactionDotUI.SetActive(false);
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
                lastHighlightedClue = null;
            }
            currentInteractableClue = null;
            SetInteractionPrompt(false);
            if (interactionDotUI != null) interactionDotUI.SetActive(false);
            return;
        }

        RaycastHit hit;
        bool foundInteractableThisFrame = false;

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
                foundInteractableThisFrame = true;
            }
            else // Hit something on layer, but not an interactable clue
            {
                ClearCurrentInteractable(); // This will hide prompt and set foundInteractableThisFrame effectively to false
            }
        }
        else // Raycast hit nothing
        {
            ClearCurrentInteractable(); // This will hide prompt
        }

        // Manage dot visibility based on whether an interactable was found THIS FRAME
        if (interactionDotUI != null)
        {
            interactionDotUI.SetActive(foundInteractableThisFrame);
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