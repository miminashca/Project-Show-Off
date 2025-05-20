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
    public float lookPositionSwayAmount = 0.005f;
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
    public float overallGravityInfluence = 0.0f;
    [Tooltip("How quickly the overall assembly settles towards gravity-influenced orientation (if influence > 0).")]
    public float overallSettlingSpeed = 4f;

    // Internal State
    private Vector3 currentSwayPosition;
    private Quaternion currentSwayRotation;
    private Vector3 positionVelocity;

    // Public property to be set by LanternController
    public PlayerInput PlayerInputActionsInstance { get; set; }

    // Removed: PlayerMovement, CameraMovement, and the private playerInputActions field

    void Start()
    {
        if (PlayerInputActionsInstance == null)
        {
            Debug.LogError("PhysicsLanternSway: PlayerInputActionsInstance was not assigned by LanternController! Sway will not react to input correctly. Update logic will be skipped.", this);
        }

        if (!lanternHoldTarget)
        {
            Debug.LogError("PhysicsLanternSway: LanternHoldTarget not assigned! Disabling script.", this);
            enabled = false;
            return;
        }
        if (handleRigidbody != null) handleRigidbody.isKinematic = true;

        // Initialize State
        currentSwayPosition = lanternHoldTarget.position;
        currentSwayRotation = lanternHoldTarget.rotation;
        transform.SetPositionAndRotation(currentSwayPosition, currentSwayRotation);
    }

    // OnEnable and OnDisable related to playerInputActions are removed,
    // as the lifecycle of PlayerInputActionsInstance is managed externally by LanternController.

    void Update()
    {
        if (!enabled || !lanternHoldTarget) return;
        if (PlayerInputActionsInstance == null) return; // Don't run update logic if input is not available

        // --- 1. Base Target (from hand anchor) ---
        Vector3 baseTargetPos = lanternHoldTarget.position;
        Quaternion baseTargetRot = lanternHoldTarget.rotation;

        // --- 2. Calculate Sway Offsets based on Player Input ---
        Vector2 lookInput = Vector2.zero;
        Vector2 moveInput = Vector2.zero;

        if (PlayerInputActionsInstance.Player.enabled) // Check if the Action Map is enabled
        {
            lookInput = PlayerInputActionsInstance.Player.Look.ReadValue<Vector2>();
            moveInput = PlayerInputActionsInstance.Player.Move.ReadValue<Vector2>();
        }

        // --- Positional Sway ---
        Vector3 positionalOffset = Vector3.zero;
        positionalOffset += new Vector3(-lookInput.x * lookPositionSwayAmount, -lookInput.y * lookPositionSwayAmount * 0.5f, 0f);
        positionalOffset += new Vector3(-moveInput.x * movePositionSwayAmount, 0f, -moveInput.y * movePositionSwayAmount * 0.75f);

        positionalOffset = lanternHoldTarget.TransformDirection(positionalOffset);
        positionalOffset = Vector3.ClampMagnitude(positionalOffset, maxPositionalSway);
        Vector3 targetPosWithSway = baseTargetPos + positionalOffset;

        // --- Rotational Sway ---
        float pitchSway = lookInput.y * lookRotationSwayAmount;
        float yawSway = -lookInput.x * lookRotationSwayAmount * 0.75f;
        float rollSwayLook = -lookInput.x * lookRotationSwayAmount * 0.25f;
        float pitchMoveSway = -moveInput.y * moveRotationSwayAmount * 0.5f;
        float rollMoveSway = -moveInput.x * moveRotationSwayAmount;

        Quaternion rotationalOffset = Quaternion.Euler(pitchSway + pitchMoveSway, yawSway, rollSwayLook + rollMoveSway);
        Quaternion targetRotWithSway = baseTargetRot * rotationalOffset;

        if (overallGravityInfluence > 0.001f)
        {
            Vector3 currentUp = transform.up;
            Quaternion gravityAlignedTarget = Quaternion.FromToRotation(currentUp, Vector3.down) * transform.rotation;
            float inputMagnitude = lookInput.magnitude + moveInput.magnitude;
            float dynamicInfluence = Mathf.Lerp(overallGravityInfluence, overallGravityInfluence * 0.2f, Mathf.Clamp01(inputMagnitude));
            targetRotWithSway = Quaternion.Slerp(targetRotWithSway, gravityAlignedTarget, dynamicInfluence * overallSettlingSpeed * Time.deltaTime);
        }

        targetRotWithSway = Quaternion.RotateTowards(baseTargetRot, targetRotWithSway, maxRotationalSway);

        // --- 3. Smoothly Update Transform ---
        currentSwayPosition = Vector3.SmoothDamp(currentSwayPosition, targetPosWithSway, ref positionVelocity, positionSmoothTime, Mathf.Infinity, Time.deltaTime);
        currentSwayRotation = Quaternion.Slerp(currentSwayRotation, targetRotWithSway, Time.deltaTime * (1.0f / rotationSmoothTime));

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