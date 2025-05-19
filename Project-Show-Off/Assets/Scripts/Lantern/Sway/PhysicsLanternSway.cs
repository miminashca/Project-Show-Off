// PhysicsLanternSway.cs
using UnityEngine;

public class PhysicsLanternSway : MonoBehaviour
{
    [Header("References")]
    public Transform playerCameraTransform;     // First-person camera (assigned by LanternController)
    public Transform lanternHoldTarget;         // The ideal position/rotation the hand is aiming for (assigned by LanternController)

    [Header("Physics Parts (for Reset)")]
    public Rigidbody handleRigidbody;           // Kinematic RB (child of this transform, assigned by LanternController)
    public Rigidbody swingingLanternBodyRB;     // Dynamic RB with HingeJoint (child of this transform, assigned by LanternController)

    [Header("Overall Positional Sway")]
    [Tooltip("How quickly the lantern's position catches up to the hand anchor. Higher = more lag.")]
    public float positionSmoothTime = 0.08f;
    [Tooltip("Maximum distance the lantern can sway positionally from the hand anchor due to movement/look.")]
    public float maxPositionalSway = 0.1f;
    [Tooltip("Multiplier for positional sway based on player look speed (mouse input).")]
    public float lookPositionSwayAmount = 0.005f; // Smaller values for subtle effect
    [Tooltip("Multiplier for positional sway based on player movement speed.")]
    public float movePositionSwayAmount = 0.01f;

    [Header("Overall Rotational Sway")]
    [Tooltip("How quickly the lantern's rotation catches up to the hand anchor. Higher = more lag.")]
    public float rotationSmoothTime = 0.1f;
    [Tooltip("Maximum angle the lantern can sway rotationally from the hand anchor.")]
    public float maxRotationalSway = 10f;
    [Tooltip("Multiplier for rotational sway (tilt) based on player look speed.")]
    public float lookRotationSwayAmount = 0.5f;
    [Tooltip("Multiplier for rotational sway (tilt) based on player movement speed.")]
    public float moveRotationSwayAmount = 1.0f;


    [Header("Overall Gravity/Settling (for entire assembly)")]
    [Tooltip("How strongly the ENTIRE lantern assembly tries to orient itself to hang down. Set to 0 to let Hinge Joint exclusively handle body hanging.")]
    public float overallGravityInfluence = 0.0f; // <<<< START WITH 0
    [Tooltip("How quickly the overall assembly settles towards gravity-influenced orientation (if influence > 0).")]
    public float overallSettlingSpeed = 4f;

    // Internal State
    private Vector3 currentSwayPosition;
    private Quaternion currentSwayRotation;
    private Vector3 positionVelocity;

    // To get inputs from PlayerController and CameraController if needed
    private PlayerMovement playerMovementController;
    private CameraMovement cameraMovementController; // If you need direct access to its raw look inputs before smoothing
    private PlayerInput playerInputActions; // Access via LanternController or directly

    void Start()
    {
        // --- Get PlayerInput instance ---
        // Assuming LanternController is on the same GameObject as PlayerMovement, or a parent
        LanternController lanternController = GetComponentInParent<LanternController>();
        if (lanternController != null)
        {
            // This is a bit of a hacky way to get it if LanternController already news it up.
            // Ideally, PlayerInput is a singleton or passed around.
            // For now, let's assume we can new it up here if needed, or LanternController provides it.
            // If LanternController news up PlayerInput, it should provide access.
            // For simplicity of this script, we'll assume it's available globally or through a Find.
            // This is NOT ideal for production.
            var playerObject = GameObject.FindGameObjectWithTag("Player"); // Assuming player has "Player" tag
            if (playerObject != null)
            {
                playerInputActions = playerObject.GetComponent<PlayerInput>();
                playerMovementController = playerObject.GetComponent<PlayerMovement>();
            }
            if (playerCameraTransform != null)
            { // playerCameraTransform is assigned by LanternController
                cameraMovementController = playerCameraTransform.GetComponent<CameraMovement>();
            }


        }
        if (playerInputActions == null)
        {
            Debug.LogWarning("PhysicsLanternSway: PlayerInput actions not found. Sway might not react to input correctly. Trying to new one up.");
            playerInputActions = new PlayerInput(); // Fallback, ensure it's enabled
            playerInputActions.Enable();
        }
        if (playerMovementController == null && lanternController != null)
        {
            playerMovementController = lanternController.GetComponentInParent<PlayerMovement>(); // If LanternController is on a child of player
        }
        if (playerMovementController == null) Debug.LogWarning("PhysicsLanternSway: PlayerMovement controller not found.");
        if (cameraMovementController == null && playerCameraTransform != null)
        {
            cameraMovementController = playerCameraTransform.GetComponent<CameraMovement>();
        }
        if (cameraMovementController == null) Debug.LogWarning("PhysicsLanternSway: CameraMovement controller not found.");


        // --- Reference Checks ---
        if (!lanternHoldTarget) // playerCameraTransform is not strictly needed for this version of sway
        {
            Debug.LogError("PhysicsLanternSway: LanternHoldTarget not assigned! Disabling script.", this);
            enabled = false;
            return;
        }
        if (handleRigidbody != null) handleRigidbody.isKinematic = true;

        // --- Initialize State ---
        currentSwayPosition = lanternHoldTarget.position;
        currentSwayRotation = lanternHoldTarget.rotation;
        transform.SetPositionAndRotation(currentSwayPosition, currentSwayRotation);
    }

    void OnEnable()
    {
        // Ensure input actions are enabled if we new'd them up
        if (playerInputActions != null && !playerInputActions.asset.enabled) // Check if the asset itself is enabled
        {
            playerInputActions.Enable();
        }
    }

    void OnDisable()
    {
        // It's good practice to disable actions if this script specifically enabled them
        // However, if PlayerInput is managed globally, this might not be necessary or desired.
        // if (playerInputActions != null && newlyCreatedPlayerInput) playerInputActions.Disable();
    }

    void Update()
    {
        if (!enabled || !lanternHoldTarget) return;

        // --- 1. Base Target (from hand anchor) ---
        Vector3 baseTargetPos = lanternHoldTarget.position;
        Quaternion baseTargetRot = lanternHoldTarget.rotation;

        // --- 2. Calculate Sway Offsets based on Player Input ---
        Vector2 lookInput = Vector2.zero;
        Vector2 moveInput = Vector2.zero;

        if (playerInputActions != null && playerInputActions.Movement.enabled) // Check if the Action Map is enabled
        {
            lookInput = playerInputActions.Movement.Look.ReadValue<Vector2>();
            moveInput = playerInputActions.Movement.Move.ReadValue<Vector2>();
        }


        // --- Positional Sway ---
        Vector3 positionalOffset = Vector3.zero;
        // Look-based positional sway (subtle "drag" when looking around)
        positionalOffset += new Vector3(-lookInput.x * lookPositionSwayAmount, -lookInput.y * lookPositionSwayAmount * 0.5f, 0f);
        // Movement-based positional sway (lantern bobs slightly opposite to movement)
        positionalOffset += new Vector3(-moveInput.x * movePositionSwayAmount, 0f, -moveInput.y * movePositionSwayAmount * 0.75f);

        // Transform offset to be local to the hold target's orientation for more natural feel
        positionalOffset = lanternHoldTarget.TransformDirection(positionalOffset);
        positionalOffset = Vector3.ClampMagnitude(positionalOffset, maxPositionalSway);

        Vector3 targetPosWithSway = baseTargetPos + positionalOffset;

        // --- Rotational Sway ---
        // Tilt from looking
        float pitchSway = lookInput.y * lookRotationSwayAmount;
        float yawSway = -lookInput.x * lookRotationSwayAmount * 0.75f; // Less yaw sway usually
        float rollSwayLook = -lookInput.x * lookRotationSwayAmount * 0.25f; // Subtle roll from fast horizontal look

        // Tilt from moving
        float pitchMoveSway = -moveInput.y * moveRotationSwayAmount * 0.5f; // Forward/back movement tilts lantern
        float rollMoveSway = -moveInput.x * moveRotationSwayAmount;       // Strafe movement rolls lantern

        Quaternion rotationalOffset = Quaternion.Euler(pitchSway + pitchMoveSway, yawSway, rollSwayLook + rollMoveSway);
        Quaternion targetRotWithSway = baseTargetRot * rotationalOffset;

        // --- Optional: Overall Gravity Influence on the ENTIRE Assembly ---
        if (overallGravityInfluence > 0.001f) // Only apply if influence is meaningful
        {
            Vector3 currentUp = transform.up; // Current up of the entire lantern assembly
            Quaternion gravityAlignedTarget = Quaternion.FromToRotation(currentUp, Vector3.down) * transform.rotation;

            // Reduce influence during fast input (optional, can make it feel more responsive)
            float inputMagnitude = lookInput.magnitude + moveInput.magnitude;
            float dynamicInfluence = Mathf.Lerp(overallGravityInfluence, overallGravityInfluence * 0.2f, Mathf.Clamp01(inputMagnitude));

            targetRotWithSway = Quaternion.Slerp(targetRotWithSway, gravityAlignedTarget, dynamicInfluence * overallSettlingSpeed * Time.deltaTime);
        }

        // Clamp total rotation to avoid extreme flipping, relative to the hand anchor's base rotation
        targetRotWithSway = Quaternion.RotateTowards(baseTargetRot, targetRotWithSway, maxRotationalSway);


        // --- 3. Smoothly Update Transform ---
        currentSwayPosition = Vector3.SmoothDamp(currentSwayPosition, targetPosWithSway, ref positionVelocity, positionSmoothTime, Mathf.Infinity, Time.deltaTime);
        currentSwayRotation = Quaternion.Slerp(currentSwayRotation, targetRotWithSway, Time.deltaTime * (1.0f / rotationSmoothTime)); // Consider a custom spring-damper for rotation for more bounce

        transform.SetPositionAndRotation(currentSwayPosition, currentSwayRotation);
    }

    public void ResetSway()
    {
        if (lanternHoldTarget)
        {
            currentSwayPosition = lanternHoldTarget.position;
            currentSwayRotation = lanternHoldTarget.rotation;
            transform.SetPositionAndRotation(currentSwayPosition, currentSwayRotation);
            positionVelocity = Vector3.zero;

            if (swingingLanternBodyRB != null)
            {
                swingingLanternBodyRB.linearVelocity = Vector3.zero;
                swingingLanternBodyRB.angularVelocity = Vector3.zero;
            }
        }
    }
}