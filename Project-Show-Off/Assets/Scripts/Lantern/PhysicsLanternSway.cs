using UnityEngine;

public class PhysicsLanternSway : MonoBehaviour
{
    [Header("References")]
    public Transform playerCameraTransform;     // First-person camera
    public Transform lanternHoldTarget;         // The ideal position/rotation the hand is aiming for (child of camera)
    public Transform lanternPivotToSway;        // The actual pivot of the lantern (e.g., top of handle)

    [Header("Positional Sway")]
    public float positionSmoothTime = 0.1f;
    public float maxPositionalSwayDistance = 0.2f; // Max distance from hold target
    public Vector3 positionSwayMultiplier = new Vector3(1f, 0.5f, 1f); // How much mouse/movement affects each axis

    [Header("Rotational Sway (Pendulum)")]
    public float rotationSmoothTime = 0.15f;
    public float maxRotationalSwayAngle = 15f; // Max angle from target rotation
    public Vector3 rotationSwayMultiplier = new Vector3(1f, 1f, 0.5f); // How much mouse/movement affects each axis (Pitch, Yaw, Roll)

    [Header("Movement Based Sway Tuning")]
    public float movementPositionSwayAmount = 0.02f;
    public float movementRotationSwayAmount = 2f;

    [Header("Gravity/Settling")]
    public float gravityInfluence = 0.5f; // How strongly it tries to hang down (0-1)
    public float settlingSpeed = 5f; // How quickly it settles to hanging straight when idle

    [Header("Input Settings")]
    public float mouseInputDeadzone = 0.001f; // Threshold below which mouse input is considered zero
    public float movementInputDeadzone = 0.01f; // Threshold below which movement input is considered zero

    // Internal State
    private Vector3 currentPivotPosition;
    private Quaternion currentPivotRotation;

    private Vector3 positionVelocity;
    // private Vector3 rotationVelocityEuler; // For SmoothDamp on Euler angles (can be tricky)

    private PlayerInput localPlayerInputActions;

    public void InitializeInput(PlayerInput inputActions)
    {
        localPlayerInputActions = inputActions;
    }

    // Or for more robust rotational spring:
    private Quaternion targetPivotRotationOffset = Quaternion.identity;


    void Start()
    {
        if (!playerCameraTransform || !lanternHoldTarget || !lanternPivotToSway)
        {
            Debug.LogError("PhysicsLanternSway: Missing one or more essential Transform references!", this);
            enabled = false;
            return;
        }
        // Initialize current state to avoid jarring start
        currentPivotPosition = lanternHoldTarget.position;
        currentPivotRotation = lanternHoldTarget.rotation;
        lanternPivotToSway.position = currentPivotPosition;
        lanternPivotToSway.rotation = currentPivotRotation;
    }

    void LateUpdate()
    {
        if (!enabled) return;

        // --- 1. Determine Target Position & Rotation based on Hand Target ---
        lanternHoldTarget.GetPositionAndRotation(out Vector3 targetPos, out Quaternion targetRot);


        // --- 2. Calculate Input-Based Sway Offsets (Mouse & Movement) ---
        float rawMouseX = Input.GetAxis("Mouse X");
        float rawMouseY = Input.GetAxis("Mouse Y");
        float rawMoveHorizontal = Input.GetAxis("Horizontal"); // Strafe
        float rawMoveVertical = Input.GetAxis("Vertical");     // Forward/Backward

        // Apply deadzones
        float mouseX = Mathf.Abs(rawMouseX) > mouseInputDeadzone ? rawMouseX : 0f;
        float mouseY = Mathf.Abs(rawMouseY) > mouseInputDeadzone ? rawMouseY : 0f;
        float moveHorizontal = Mathf.Abs(rawMoveHorizontal) > movementInputDeadzone ? rawMoveHorizontal : 0f;
        float moveVertical = Mathf.Abs(rawMoveVertical) > movementInputDeadzone ? rawMoveVertical : 0f;

        // Positional sway based on mouse (subtle)
        Vector3 mousePositionSway = new Vector3(
            -mouseX * positionSwayMultiplier.x * 0.01f,
            -mouseY * positionSwayMultiplier.y * 0.01f,
            0
        );
        mousePositionSway = lanternHoldTarget.TransformDirection(mousePositionSway); // To local space of hold target

        // Rotational sway based on mouse
        Quaternion mouseRotationSway = Quaternion.Euler(
            mouseY * rotationSwayMultiplier.x,
            -mouseX * rotationSwayMultiplier.y,
            -mouseX * rotationSwayMultiplier.z // Optional roll
        );

        // Add movement-based sway
        Vector3 movementPosOffset = new Vector3(
            -moveHorizontal * movementPositionSwayAmount,
            0, // Bobbing could be added here or via a separate headbob script on camera
            -moveVertical * movementPositionSwayAmount * 0.5f // Less sway forward/back
        );
        movementPosOffset = lanternHoldTarget.TransformDirection(movementPosOffset);

        Quaternion movementRotOffset = Quaternion.Euler(
            moveVertical * movementRotationSwayAmount * 0.5f, // Pitch forward/back
            0,
            -moveHorizontal * movementRotationSwayAmount // Roll side to side
        );

        // Apply sway to target position
        targetPos += mousePositionSway + movementPosOffset;
        targetPos = Vector3.ClampMagnitude(targetPos - lanternHoldTarget.position, maxPositionalSwayDistance) + lanternHoldTarget.position;

        // Apply sway to target rotation
        targetRot *= mouseRotationSway * movementRotOffset;


        // --- 3. Smoothly Move Lantern Pivot to Target Position ---
        currentPivotPosition = Vector3.SmoothDamp(currentPivotPosition, targetPos, ref positionVelocity, positionSmoothTime);
        lanternPivotToSway.position = currentPivotPosition;


        // --- 4. Smoothly Rotate Lantern Pivot to Target Rotation (with Gravity Influence) ---
        // This is the trickiest part for a realistic pendulum.
        // A simpler approach is to SmoothDamp Euler angles, but can suffer from gimbal lock.
        // A more robust method involves springs or direct quaternion manipulation.

        // Simpler Slerp approach (less "springy" but stable)
        // currentPivotRotation = Quaternion.Slerp(currentPivotRotation, targetRot, Time.deltaTime * (1.0f / rotationSmoothTime));

        // Quaternion SmoothDamp (conceptual - Unity doesn't have a direct one, needs helper)
        // For this example, we'll use a slightly more direct influence model for rotation

        // Apply the target rotation derived from input
        Quaternion desiredRotation = targetRot;

        // Introduce "gravity" - pull the lantern's local down vector towards world down
        // This makes it want to hang straight down from the pivot.
        Vector3 lanternDown = lanternPivotToSway.up; // If your lantern model hangs down, its 'up' is the handle direction
        Vector3 worldDown = Vector3.down;

        // Create a rotation that would align lanternDown with worldDown
        Quaternion gravityAlignment = Quaternion.FromToRotation(lanternDown, worldDown) * lanternPivotToSway.rotation;

        // Blend between the desired input rotation and the gravity-aligned rotation
        // When there's strong input (fast mouse moves), desiredRotation dominates.
        // When idle, gravityInfluence pulls it straighter.
        float inputMagnitude = Mathf.Abs(mouseX) + Mathf.Abs(mouseY) + Mathf.Abs(moveHorizontal) + Mathf.Abs(moveVertical);
        float dynamicGravityInfluence = Mathf.Lerp(gravityInfluence, 0.1f, Mathf.Clamp01(inputMagnitude * 2f)); // Less gravity pull during fast movement

        Quaternion finalTargetRotation = Quaternion.Slerp(desiredRotation, gravityAlignment, dynamicGravityInfluence * Time.deltaTime * settlingSpeed);

        // Clamp total rotation sway
        float angleDiff = Quaternion.Angle(lanternHoldTarget.rotation, finalTargetRotation);
        if (angleDiff > maxRotationalSwayAngle)
        {
            finalTargetRotation = Quaternion.Slerp(lanternHoldTarget.rotation, finalTargetRotation, maxRotationalSwayAngle / angleDiff);
        }

        // Smooth the actual pivot rotation
        currentPivotRotation = Quaternion.Slerp(currentPivotRotation, finalTargetRotation, Time.deltaTime * (1.0f / rotationSmoothTime));
        lanternPivotToSway.rotation = currentPivotRotation;
    }

    public void ResetSway()
    {
        if (lanternHoldTarget && lanternPivotToSway)
        {
            currentPivotPosition = lanternHoldTarget.position;
            currentPivotRotation = lanternHoldTarget.rotation;
            lanternPivotToSway.SetPositionAndRotation(currentPivotPosition, currentPivotRotation);
            positionVelocity = Vector3.zero;
            // rotationVelocityEuler = Vector3.zero;
        }
    }
}