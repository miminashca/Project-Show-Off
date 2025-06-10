using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class InspectionManager : MonoBehaviour
{
    public static InspectionManager Instance { get; private set; }

    [Header("Inspection Settings")]
    [SerializeField] private Transform inspectionPoint;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float inspectionObjectBaseScale = 1f;
    [SerializeField] private float objectLerpSpeed = 10f;

    [Header("UI")]
    [SerializeField] private GameObject blurBackgroundPanel;
    [SerializeField] private TextMeshPro clueNameText;
    [SerializeField] private TextMeshPro clueDescriptionText;

    [Header("References - Auto-fetched if null")]
    [SerializeField] private CameraMovement cameraMovement;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private HeadbobController headbobController;
    
    [Header("Light")]
    [SerializeField] private Light inspectionLight;
    [SerializeField] private GameObject lantern;
    private bool lanternInitiallyActive = false;

    private PlayerInput playerInputActions;

    private Coroutine activateInspectionCoroutine;

    private GameObject currentInspectedObject;
    private ClueObject currentClueData;
    private Vector3 originalObjectPosition;
    private Quaternion originalObjectRotation;
    private Vector3 originalObjectScale;
    private Transform originalObjectParent;

    private bool isInspecting = false;
    private Vector3 previousMousePosition;
    private bool isRotatingObject = false;

    //new code below
    public event System.Action<int> OnClueCollected;
    private int clueCount = 0;
    //end of new code
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
        if (headbobController == null) headbobController = FindFirstObjectByType<HeadbobController>(); // << ADD THIS
        if (inspectionLight) inspectionLight.enabled = false;


        if (cameraMovement == null) Debug.LogWarning("InspectionManager: CameraMovement script not found! Assign manually or ensure it's in the scene.");
        if (playerMovement == null) Debug.LogWarning("InspectionManager: PlayerMovement script not found! Assign manually or ensure it's in the scene.");
        if (headbobController == null) Debug.LogWarning("InspectionManager: HeadbobController script not found! Assign manually or ensure it's in the scene.");


        if (blurBackgroundPanel != null) blurBackgroundPanel.SetActive(false);
        if (clueNameText != null) clueNameText.gameObject.SetActive(false);
        if (clueDescriptionText != null) clueDescriptionText.gameObject.SetActive(false);

        if (inspectionPoint == null)
        {
            Debug.LogError("InspectionPoint is not assigned in InspectionManager! Please assign a Transform child of the Main Camera.");
            enabled = false; // Consider disabling the component if critical references are missing
        }
    }

    private void OnEnable()
    {
        // Player Action Map
        playerInputActions.Inspection.RotateObject.started += OnRotateObjectStarted;
        playerInputActions.Inspection.RotateObject.canceled += OnRotateObjectCanceled;
        playerInputActions.Inspection.ConfirmInspection.performed += OnInteractPerformed;
        playerInputActions.Inspection.CancelInspection.performed += OnCancelPerformed;
    }

    private void OnDisable()
    {
        if (playerInputActions != null)
        {
            playerInputActions.Inspection.RotateObject.started -= OnRotateObjectStarted;
            playerInputActions.Inspection.RotateObject.canceled -= OnRotateObjectCanceled;
            playerInputActions.Inspection.ConfirmInspection.performed -= OnInteractPerformed;
            playerInputActions.Inspection.CancelInspection.performed -= OnCancelPerformed;

            playerInputActions.Inspection.Disable();
        }
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
            if(inspectionLight) inspectionLight.enabled = false;
            if (lantern && lanternInitiallyActive)
            {
                lantern.SetActive(true);
            }
            CollectCurrentClue();
        } 
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        if(inspectionLight) inspectionLight.enabled = false;
        if (lantern && lanternInitiallyActive)
        {
            lantern.SetActive(true);
        }

        if (isInspecting)
        {
            CancelInspection();
        }
    }

    public void StartInspection(ClueObject clueToInspect)
    {
        if (isInspecting || clueToInspect == null) return;

        if(inspectionLight) inspectionLight.enabled = true;
        if (lantern)
        {
            lanternInitiallyActive = lantern.activeInHierarchy;
            lantern.SetActive(false);
        }
        
        isInspecting = true;
        currentClueData = clueToInspect;
        currentInspectedObject = clueToInspect.gameObject;

        originalObjectPosition = currentInspectedObject.transform.position;
        originalObjectRotation = currentInspectedObject.transform.rotation;
        originalObjectScale = currentInspectedObject.transform.localScale;
        originalObjectParent = currentInspectedObject.transform.parent;

        if (playerMovement != null) playerMovement.enabled = false;
        if (cameraMovement != null) cameraMovement.enabled = false;
        if (headbobController != null) headbobController.enabled = false;

        // --- Action Map Switching ---
        playerInputActions.Player.Disable();

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

        Debug.Log($"Preparing to inspect {currentClueData.clueName}. Inspection input will activate next frame.");

        // Start coroutine to enable inspection input map after a frame delay
        activateInspectionCoroutine = StartCoroutine(EnableInspectionInputAfterFrame());
    }

    private IEnumerator EnableInspectionInputAfterFrame()
    {
        yield return null; // Wait for one frame

        // Check if inspection is still active (wasn't cancelled during the frame delay)
        if (isInspecting && currentInspectedObject != null)
        {
            playerInputActions.Inspection.Enable(); // NOW enable the "Inspection" action map
            Debug.Log("Inspection input map enabled.");
        }
        else
        {
            Debug.LogWarning("Inspection was cancelled or object became null before inspection input could be enabled.");
            // Ensure cleanup if something went wrong
            if (playerInputActions != null) playerInputActions.Inspection.Disable();
        }
        activateInspectionCoroutine = null; // Coroutine finished
    }

    private void StopAndClearActivateInspectionCoroutine()
    {
        if (activateInspectionCoroutine != null)
        {
            StopCoroutine(activateInspectionCoroutine);
            activateInspectionCoroutine = null;
            // If coroutine was stopped, ensure the inspection map is not left enabled unintentionally
            if (playerInputActions != null) playerInputActions.Inspection.Disable();
            Debug.Log("ActivateInspectionMode coroutine stopped; Inspection map ensured disabled.");
        }
    }

    private void EndInspectionCleanup()
    {
        StopAndClearActivateInspectionCoroutine();

        isInspecting = false;
        isRotatingObject = false;

        // --- Action Map Switching ---
        playerInputActions.Inspection.Disable();

        if (playerMovement != null) playerMovement.enabled = true;
        if (cameraMovement != null) cameraMovement.enabled = true;
        if (headbobController != null) headbobController.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (blurBackgroundPanel != null) blurBackgroundPanel.SetActive(false);
        if (clueNameText != null) clueNameText.gameObject.SetActive(false);
        if (clueDescriptionText != null) clueDescriptionText.gameObject.SetActive(false);

        if (currentInspectedObject != null) currentInspectedObject = null;
        if (currentClueData != null) currentClueData = null;
    }

    public void CancelInspection()
    {
        if (!isInspecting) return;

        StopAndClearActivateInspectionCoroutine();

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

    private void CollectCurrentClue()
    {
        if (!isInspecting || currentClueData == null) return;

        Debug.Log($"Collecting {currentClueData.clueName}");
        currentClueData.OnCollected();

        //new code below
        clueCount++;
        OnClueCollected?.Invoke(clueCount);
        //end of new code

        currentInspectedObject = null;
        currentClueData = null;

        EndInspectionCleanup();
    }
}