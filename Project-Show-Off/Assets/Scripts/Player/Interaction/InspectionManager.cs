// InspectionManager.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class InspectionManager : MonoBehaviour
{
    public static InspectionManager Instance { get; private set; }

    [Header("Inspection Settings")]
    [SerializeField] private Transform inspectionPoint;
    [SerializeField] private float rotationSpeed = 150f;
    [SerializeField] private float inspectionObjectBaseScale = 1f;
    [SerializeField] private float objectLerpSpeed = 10f;

    [Header("UI")]
    [SerializeField] private GameObject blurBackgroundPanel;
    [SerializeField] private Text clueNameText;
    [SerializeField] private Text clueDescriptionText;

    [Header("References - Auto-fetched if null")]
    [SerializeField] private CameraMovement cameraMovement;
    [SerializeField] private PlayerMovement playerMovement;

    private PlayerInput playerInputActions;

    private GameObject currentInspectedObject;
    private ClueObject currentClueData;
    private Vector3 originalObjectPosition;
    private Quaternion originalObjectRotation;
    private Vector3 originalObjectScale;
    private Transform originalObjectParent;

    private bool isInspecting = false;
    private Vector3 previousMousePosition;
    private bool isRotatingObject = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        playerInputActions = new PlayerInput();

        if (cameraMovement == null) cameraMovement = FindFirstObjectByType<CameraMovement>();
        if (playerMovement == null) playerMovement = FindFirstObjectByType<PlayerMovement>();

        if (blurBackgroundPanel != null) blurBackgroundPanel.SetActive(false);
        if (clueNameText != null) clueNameText.gameObject.SetActive(false);
        if (clueDescriptionText != null) clueDescriptionText.gameObject.SetActive(false);

        if (inspectionPoint == null)
        {
            Debug.LogError("InspectionPoint is not assigned in InspectionManager! Please assign a Transform child of the Main Camera.");
            enabled = false;
        }
    }

    private void OnEnable()
    {
        playerInputActions.Enable(); // Enable all action maps

        // Player Action Map
        playerInputActions.Player.Interact.performed += OnInteractPerformed;
        playerInputActions.Player.RotateObject.started += OnRotateObjectStarted;
        playerInputActions.Player.RotateObject.canceled += OnRotateObjectCanceled;

        // Check if you want the Player map's Cancel or UI map's Cancel.
        // Let's assume for inspection, the UI mode's cancel is primary.
        // If you only want to use one, remove the other from your input actions asset.

        // UI Action Map
        if (playerInputActions.UI.Cancel != null) // This check is more for safety if the action might not be there
        {
            playerInputActions.UI.Cancel.performed += OnCancelPerformed;
        }
        else
        {
            Debug.LogWarning("InspectionManager: 'Cancel' action not found in 'UI' action map. Escape to cancel inspection might not work from UI map.");
        }

        // If you also want the Player map's Cancel to work for inspection:
        // (This might be redundant if UI map is active during inspection)
        // if (playerInputActions.Player.Cancel != null)
        // {
        //    playerInputActions.Player.Cancel.performed += OnCancelPerformed;
        // }


        // IMPORTANT: Enabling/Disabling Specific Action Maps
        // When inspection starts, you'll likely want to disable the 'Player' action map
        // and enable the 'UI' action map (or a dedicated 'Inspection' action map).
        // And reverse this when inspection ends.
    }

    private void OnDisable()
    {
        // Player Action Map
        playerInputActions.Player.Interact.performed -= OnInteractPerformed;
        playerInputActions.Player.RotateObject.started -= OnRotateObjectStarted;
        playerInputActions.Player.RotateObject.canceled -= OnRotateObjectCanceled;

        // UI Action Map
        if (playerInputActions.UI.Cancel != null)
        {
            playerInputActions.UI.Cancel.performed -= OnCancelPerformed;
        }

        // if (playerInputActions.Player.Cancel != null)
        // {
        //     playerInputActions.Player.Cancel.performed -= OnCancelPerformed;
        // }

        playerInputActions.Disable(); // Disable all action maps
    }

    void Update()
    {
        if (isInspecting && currentInspectedObject != null)
        {
            currentInspectedObject.transform.position = Vector3.Lerp(currentInspectedObject.transform.position, inspectionPoint.position, Time.deltaTime * objectLerpSpeed);
            float targetScale = inspectionObjectBaseScale * (currentClueData != null ? currentClueData.inspectionScaleFactor : 1f);
            currentInspectedObject.transform.localScale = Vector3.Lerp(currentInspectedObject.transform.localScale, Vector3.one * targetScale, Time.deltaTime * objectLerpSpeed);

            if (isRotatingObject)
            {
                Vector3 currentMousePosition = Mouse.current.position.ReadValue();
                Vector3 deltaMouse = currentMousePosition - previousMousePosition;

                float rotX = deltaMouse.y * rotationSpeed * Time.deltaTime;
                float rotY = -deltaMouse.x * rotationSpeed * Time.deltaTime;

                currentInspectedObject.transform.Rotate(inspectionPoint.right, rotX, Space.World);
                currentInspectedObject.transform.Rotate(inspectionPoint.up, rotY, Space.World);

                previousMousePosition = currentMousePosition;
            }
        }
    }

    private void OnRotateObjectStarted(InputAction.CallbackContext context)
    {
        if (isInspecting)
        {
            isRotatingObject = true;
            previousMousePosition = Mouse.current.position.ReadValue();
        }
    }

    private void OnRotateObjectCanceled(InputAction.CallbackContext context)
    {
        if (isInspecting)
        {
            isRotatingObject = false;
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (isInspecting)
        {
            CollectCurrentClue();
        }
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        if (isInspecting)
        {
            CancelInspection();
        }
    }

    public void StartInspection(ClueObject clueToInspect)
    {
        if (isInspecting || clueToInspect == null) return;

        isInspecting = true;
        currentClueData = clueToInspect;
        currentInspectedObject = clueToInspect.gameObject;

        originalObjectPosition = currentInspectedObject.transform.position;
        originalObjectRotation = currentInspectedObject.transform.rotation;
        originalObjectScale = currentInspectedObject.transform.localScale;
        originalObjectParent = currentInspectedObject.transform.parent;

        // --- Action Map Switching ---
        playerInputActions.Player.Disable(); // Disable player movement/look
        playerInputActions.UI.Enable();      // Enable UI controls like Cancel

        if (playerMovement != null) playerMovement.enabled = false; // Still good to explicitly disable scripts
        if (cameraMovement != null) cameraMovement.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        currentInspectedObject.transform.rotation = inspectionPoint.rotation * Quaternion.Euler(currentClueData.inspectionRotationOffset);

        if (blurBackgroundPanel != null) blurBackgroundPanel.SetActive(true);
        if (clueNameText != null)
        {
            clueNameText.text = currentClueData.clueName;
            clueNameText.gameObject.SetActive(true);
        }
        if (clueDescriptionText != null)
        {
            clueDescriptionText.text = currentClueData.clueDescription;
            clueDescriptionText.gameObject.SetActive(true);
        }

        currentClueData.SetInteractable(false);
        Collider col = currentClueData.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }
        Debug.Log($"Started inspecting {currentClueData.clueName}");
    }

    private void EndInspectionCleanup() // Renamed from EndInspection to avoid confusion with a public method
    {
        isInspecting = false;
        isRotatingObject = false;

        // --- Action Map Switching ---
        playerInputActions.UI.Disable();    // Disable UI controls
        playerInputActions.Player.Enable(); // Re-enable player movement/look


        if (playerMovement != null) playerMovement.enabled = true; // Still good to explicitly disable scripts
        if (cameraMovement != null) cameraMovement.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (blurBackgroundPanel != null) blurBackgroundPanel.SetActive(false);
        if (clueNameText != null) clueNameText.gameObject.SetActive(false);
        if (clueDescriptionText != null) clueDescriptionText.gameObject.SetActive(false);

        if (currentInspectedObject != null) currentInspectedObject = null;
        if (currentClueData != null) currentClueData = null;
    }

    private void CollectCurrentClue()
    {
        if (!isInspecting || currentClueData == null) return;

        Debug.Log($"Collecting {currentClueData.clueName}");
        currentClueData.OnCollected();

        currentInspectedObject = null;
        currentClueData = null;
        EndInspectionCleanup();
    }

    public void CancelInspection()
    {
        if (!isInspecting) return;

        Debug.Log($"Cancelling inspection of {(currentClueData != null ? currentClueData.clueName : "object")}");
        if (currentInspectedObject != null)
        {
            currentInspectedObject.transform.SetParent(originalObjectParent, true);
            currentInspectedObject.transform.position = originalObjectPosition;
            currentInspectedObject.transform.rotation = originalObjectRotation;
            currentInspectedObject.transform.localScale = originalObjectScale;
            Collider col = currentInspectedObject.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;
            }
            if (currentClueData != null)
            {
                currentClueData.SetInteractable(true);
            }
        }

        EndInspectionCleanup();
    }

    public bool IsInspecting()
    {
        return isInspecting;
    }
}