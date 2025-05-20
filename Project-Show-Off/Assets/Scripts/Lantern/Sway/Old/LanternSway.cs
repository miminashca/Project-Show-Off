using UnityEngine;

public class LanternSway : MonoBehaviour
{
    [Header("Sway Settings")]
    public float positionSwayAmount = 0.05f;
    public float rotationSwayAmount = 4f;
    public float smoothAmount = 6f;
    public float maxPositionSway = 0.1f;
    public float maxRotationSway = 8f;

    [Header("Dependencies")]
    public Transform playerCameraTransform; // Assign the first person camera transform

    // Internal state
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private Vector3 targetPosition;
    private Quaternion targetRotation;


    void Start()
    {
        if (playerCameraTransform == null)
        {
            Debug.LogError("LanternSway: Player Camera Transform not assigned!");
            // Try to find it dynamically? Might be fragile.
            Camera mainCam = Camera.main; // Assumes MainCamera tag is set
            if (mainCam != null) playerCameraTransform = mainCam.transform;
            else enabled = false;
        }
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
    }

    void Update()
    {
        ApplySway();
    }

    void ApplySway()
    {
        // --- Input Based Sway (Mouse Look) ---
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Calculate target position sway
        float targetPosX = Mathf.Clamp(-mouseX * positionSwayAmount, -maxPositionSway, maxPositionSway);
        float targetPosY = Mathf.Clamp(-mouseY * positionSwayAmount, -maxPositionSway, maxPositionSway);
        targetPosition = new Vector3(targetPosX, targetPosY, 0); // Sway on local X and Y

        // Calculate target rotation sway
        float targetRotX = Mathf.Clamp(mouseY * rotationSwayAmount, -maxRotationSway, maxRotationSway); // Pitch
        float targetRotY = Mathf.Clamp(-mouseX * rotationSwayAmount, -maxRotationSway, maxRotationSway); // Yaw
        // float targetRotZ = Mathf.Clamp(-mouseX * rotationSwayAmount * 0.5f, -maxRotationSway * 0.5f, maxRotationSway * 0.5f); // Optional Roll
        targetRotation = Quaternion.Euler(targetRotX, targetRotY, 0 /*targetRotZ*/);


        // --- Movement Based Sway (Optional but recommended for realism) ---
        // This part requires access to player movement input or velocity
        // Example using simple input axes:
        float moveX = Input.GetAxis("Horizontal"); // Strafe
        float moveZ = Input.GetAxis("Vertical");   // Forward/Backward

        // Add movement influence (adjust multipliers as needed)
        targetPosition.x += Mathf.Clamp(-moveX * positionSwayAmount * 0.5f, -maxPositionSway * 0.5f, maxPositionSway * 0.5f);
        targetPosition.z += Mathf.Clamp(-moveZ * positionSwayAmount * 0.3f, -maxPositionSway * 0.3f, maxPositionSway * 0.3f); // Bob forward/back slightly


        // --- Apply Smoothing ---
        // Smoothly interpolate towards the target sway from the initial local position/rotation
        transform.localPosition = Vector3.Lerp(transform.localPosition, initialLocalPosition + targetPosition, Time.deltaTime * smoothAmount);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, initialLocalRotation * targetRotation, Time.deltaTime * smoothAmount);
    }

    // Call this when equipping to prevent large initial jerks
    public void ResetSway()
    {
        transform.localPosition = initialLocalPosition;
        transform.localRotation = initialLocalRotation;
        targetPosition = Vector3.zero;
        targetRotation = Quaternion.identity;
    }

    // Optional: Can be used by LanternController to slightly change sway behavior when raised
    public void NotifyRaised(bool raised)
    {
        // Example: Slightly increase sway amount when raised?
        // positionSwayAmount = raised ? basePositionSway * 1.1f : basePositionSway;
    }
}