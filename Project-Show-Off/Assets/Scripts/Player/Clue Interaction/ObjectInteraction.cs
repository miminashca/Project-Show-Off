using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class ObjectInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private Transform cameraTransform;

    [Header("UI")]
    [SerializeField] private GameObject interactionPromptUI;
    [SerializeField] private GameObject interactionDotUI;
    [SerializeField] private CanvasGroup interactionPromptCanvasGroup;
    //new fuel code
    [SerializeField] private GameObject fuelPickUpPromptUI;
    [SerializeField] private CanvasGroup fuelPickUpCanvasGroup;
    //end of fuel code
    //new offer code
    [SerializeField] private GameObject offerPromptUI;
    [SerializeField] private CanvasGroup offerPromptCanvasGroup;
    //end of offer code

    //new code
    [SerializeField] private float fadeDuration = 0.3f;
    private Coroutine fadeCoroutine;
    //end of new code

    private PlayerInput playerInputActions;
    private ClueObject currentInteractableClue;
    private ClueObject lastHighlightedClue;
    
    private FuelPickup currentInteractableFuel;
    
    private ClueReceiver currentClueReceiver;


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

        //new code
        if (interactionPromptCanvasGroup != null)
        {
            interactionPromptCanvasGroup.alpha = 0f;
            interactionPromptCanvasGroup.gameObject.SetActive(false);
        }

        if (fuelPickUpPromptUI != null) fuelPickUpPromptUI.SetActive(false);
        if (fuelPickUpCanvasGroup != null)
        {
            fuelPickUpCanvasGroup.alpha = 0f;
            fuelPickUpCanvasGroup.gameObject.SetActive(false);
        }
        //end of new code
        if (offerPromptUI != null) 
        { 
            offerPromptUI.SetActive(false); 
            offerPromptCanvasGroup.gameObject.SetActive(false);
        }
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

        //new code
        if (interactionPromptCanvasGroup != null)
        {
            interactionPromptCanvasGroup.alpha = 0f;
            interactionPromptCanvasGroup.gameObject.SetActive(false);
        }

        if (fuelPickUpPromptUI != null) fuelPickUpPromptUI.SetActive(false);
        if (fuelPickUpCanvasGroup != null)
        {
            fuelPickUpCanvasGroup.alpha = 0f;
            fuelPickUpCanvasGroup.gameObject.SetActive(false);
        }
        //end of new code
        //new offer code
        if (offerPromptUI != null) offerPromptUI.SetActive(false);
        if (offerPromptCanvasGroup != null)
        {
            offerPromptCanvasGroup.alpha = 0f;
            offerPromptCanvasGroup.gameObject.SetActive(false);
        }
        //end of offer code
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
            FuelPickup fuel = hit.collider.GetComponent<FuelPickup>();
            ClueReceiver clueReceiver = hit.collider.GetComponent<ClueReceiver>();
            
            if ((clue != null && clue.IsInteractable()))
            {
                currentInteractableClue = clue;
                currentInteractableFuel = null; //new code
                if (lastHighlightedClue != currentInteractableClue)
                {
                    if (lastHighlightedClue != null) lastHighlightedClue.Highlight(false);
                    currentInteractableClue.Highlight(true);
                    lastHighlightedClue = currentInteractableClue;
                }
                SetInteractionPrompt(true);
                foundInteractableThisFrame = true;
            }
            else if (fuel != null)
            {
                currentInteractableFuel = fuel;
                currentInteractableClue = null; //new code
                currentClueReceiver = null; //new code
                if (lastHighlightedClue != currentInteractableClue)
                {
                    if (lastHighlightedClue != null) lastHighlightedClue.Highlight(false);
                }
                SetInteractionPrompt(true); //new code
                foundInteractableThisFrame = true;
            }
            else if (clueReceiver != null)
            {
                currentClueReceiver = clueReceiver;
                currentInteractableClue = null; //new code
                currentInteractableFuel = null; //new code
                if (lastHighlightedClue != currentInteractableClue)
                {
                    if (lastHighlightedClue != null) lastHighlightedClue.Highlight(false);
                }
                SetInteractionPrompt(true); //new code
                foundInteractableThisFrame = true;
            }
            else
            {
                ClearCurrentInteractable();
            }
        }
        else
        {
            ClearCurrentInteractable();
        }

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
        currentInteractableFuel = null; 
        currentClueReceiver = null; 
        SetInteractionPrompt(false);
    }

    private void TryInitiateInteraction(InputAction.CallbackContext context)
    {
        if (InspectionManager.Instance != null && InspectionManager.Instance.IsInspecting())
        {
            return;
        }

        //CLUE
        if (currentInteractableClue != null)
        {
            Debug.Log("PlayerInteraction: Interacting with " + currentInteractableClue.clueName);
            if (InspectionManager.Instance != null)
            {
                InspectionManager.Instance.StartInspection(currentInteractableClue);
                ClearCurrentInteractable();
            }
            else
            {
                Debug.LogError("InspectionManager instance not found!");
            }
        }

        //FUEL
        if (currentInteractableFuel != null)
        {
            InspectionManager.Instance.TryPickupFuel(currentInteractableFuel);
        }
        
        //CLUE RECEIVER
        if (currentClueReceiver != null)
        {
            InspectionManager.Instance.SubmitClue(currentClueReceiver);
        }
    }

    private void SetInteractionPrompt(bool show)
    {
        //new code
        if (currentInteractableClue != null)
        {
            if (interactionPromptCanvasGroup != null)
                FadeCanvasGroup(interactionPromptCanvasGroup, show);
            else if (interactionPromptUI != null)
                interactionPromptUI.SetActive(show);
        }
        else if (currentInteractableFuel != null)
        {
            if (fuelPickUpCanvasGroup != null)
                FadeCanvasGroup(fuelPickUpCanvasGroup, show);
            else if (fuelPickUpPromptUI != null)
                fuelPickUpPromptUI.SetActive(show);
        }
        else if (currentClueReceiver != null)
        {
            if (offerPromptCanvasGroup != null)
                FadeCanvasGroup(offerPromptCanvasGroup, show);
            else if (offerPromptUI != null)
                offerPromptUI.SetActive(show);
        }
        else
        {
            if (interactionPromptCanvasGroup != null)
                FadeCanvasGroup(interactionPromptCanvasGroup, false);
            if (fuelPickUpCanvasGroup != null)
                FadeCanvasGroup(fuelPickUpCanvasGroup, false);
            if (offerPromptCanvasGroup != null)
                FadeCanvasGroup(offerPromptCanvasGroup, false);

            if (interactionPromptUI != null)
                interactionPromptUI.SetActive(false);
            if (fuelPickUpPromptUI != null)
                fuelPickUpPromptUI.SetActive(false);
            if (offerPromptUI != null)
                offerPromptUI.SetActive(false);
        }
        //end of new code
    }

    //new code fade canvas logic
    private void FadeCanvasGroup(CanvasGroup canvasGroup, bool fadeIn)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine(canvasGroup, fadeIn));
    }

    private IEnumerator FadeRoutine(CanvasGroup canvasGroup, bool fadeIn)
    {
        float startAlpha = canvasGroup.alpha;
        float endAlpha = fadeIn ? 1f : 0f;
        float elapsed = 0f;

        if (fadeIn) canvasGroup.gameObject.SetActive(true);

        while (elapsed < fadeDuration)
        {
            elapsed += Mathf.Min(Time.deltaTime, fadeDuration);
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;

        if (!fadeIn) canvasGroup.gameObject.SetActive(false);
    }
    //end of new code
}
