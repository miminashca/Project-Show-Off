using UnityEngine;

public class PhysicsLanternSway : MonoBehaviour
{
    [Header("References")]
    public Transform playerCameraTransform;
    public Transform lanternHoldTarget;

    [Header("Physics Parts")] // Renamed for clarity, these are assigned by controller or prefab
    public Rigidbody handleRigidbody;
    public Rigidbody swingingLanternBodyRB;

    [Header("Target Offset (for Raise/Lower)")]
    [Tooltip("The target local offset from the lanternHoldTarget. Set by LanternController.")]
    public Vector3 targetLocalOffset = Vector3.zero;
    [Tooltip("How quickly the lantern animates to the targetLocalOffset (e.g., for raising/lowering).")]
    public float localOffsetSmoothTime = 0.2f;
    private Vector3 currentAppliedLocalOffset;
    private Vector3 localOffsetVelocity;

    [Header("Overall Positional Sway")]
    public float positionSmoothTime = 0.08f;
    public float maxPositionalSway = 0.1f;
    public float lookPositionSwayAmount = 0.005f;
    public float movePositionSwayAmount = 0.01f;

    [Header("Overall Rotational Sway")]
    public float rotationSmoothTime = 0.1f;
    public float maxRotationalSway = 10f;
    public float lookRotationSwayAmount = 0.5f;
    public float moveRotationSwayAmount = 1.0f;

    [Header("Overall Gravity/Settling (for entire assembly)")]
    public float overallGravityInfluence = 0.0f;
    public float overallSettlingSpeed = 4f;

    private Vector3 currentSwayPosition;
    private Quaternion currentSwayRotation;
    private Vector3 positionVelocity;

    public PlayerInput PlayerInputActionsInstance { get; private set; } // Setter can be private if only set via Initialize

    private bool isInitialized = false;

    // Call this method from LanternController after instantiating and getting references
    public void InitializeSway(PlayerInput inputActions, Transform camTransform, Transform holdTarget, Rigidbody handleRB, Rigidbody swingRB)
    {
        PlayerInputActionsInstance = inputActions;
        playerCameraTransform = camTransform; // Or keep public and let it be set via Inspector/Controller
        lanternHoldTarget = holdTarget;       // Or keep public
        handleRigidbody = handleRB;
        swingingLanternBodyRB = swingRB;

        if (PlayerInputActionsInstance == null)
        {
            Debug.LogError("PhysicsLanternSway: PlayerInputActionsInstance was not assigned during Initialize! Sway will not react to input correctly.", this);
        }
        if (playerCameraTransform == null)
        {
            Debug.LogWarning("PhysicsLanternSway: playerCameraTransform was not assigned during Initialize. Some sway features might be limited if it's needed later and not set.", this);
        }
        if (!lanternHoldTarget)
        {
            Debug.LogError("PhysicsLanternSway: LanternHoldTarget not assigned during Initialize! Disabling script.", this);
            enabled = false; // Or just don't set isInitialized = true
            return;
        }

        if (handleRigidbody != null)
        {
            handleRigidbody.isKinematic = true;
        }
        else
        {
            Debug.LogError("PhysicsLanternSway: handleRigidbody not assigned during Initialize! The lantern handle might not behave correctly.", this);
        }
        if (swingingLanternBodyRB == null)
        {
            // This RB is mostly for the ResetSway method to clear velocities.
            // Debug.LogWarning("PhysicsLanternSway: swingingLanternBodyRB not assigned during Initialize.", this);
        }

        // Initialize currentAppliedLocalOffset to the current targetLocalOffset
        // targetLocalOffset might have been set by LanternController before calling InitializeSway
        // or it defaults to Vector3.zero.
        currentAppliedLocalOffset = targetLocalOffset;
        localOffsetVelocity = Vector3.zero;

        // Initialize current sway state
        if (lanternHoldTarget != null) // Check again in case it was null and script wasn't disabled
        {
            currentSwayPosition = lanternHoldTarget.TransformPoint(currentAppliedLocalOffset);
            currentSwayRotation = lanternHoldTarget.rotation;
            transform.SetPositionAndRotation(currentSwayPosition, currentSwayRotation);
            positionVelocity = Vector3.zero;
        }
        
        isInitialized = true;
        if (!enabled) enabled = true; // Ensure script is enabled if it was disabled due to missing lanternHoldTarget initially
    }

    // Removed Start() as initialization is now more controlled by InitializeSway()
    // If you need Start() for other purposes, ensure it doesn't conflict.

    void Update()
    {
        if (!isInitialized || !enabled || !lanternHoldTarget || PlayerInputActionsInstance == null) return;

        // --- 0. Smoothly update the applied local offset towards the target local offset ---
        currentAppliedLocalOffset = Vector3.SmoothDamp(currentAppliedLocalOffset, targetLocalOffset, ref localOffsetVelocity, localOffsetSmoothTime, Mathf.Infinity, Time.deltaTime);

        // --- 1. Base Target (from hand anchor, incorporating the dynamic local offset) ---
        Vector3 baseTargetPos = lanternHoldTarget.TransformPoint(currentAppliedLocalOffset);
        Quaternion baseTargetRot = lanternHoldTarget.rotation;

        // --- 2. Calculate Sway Offsets based on Player Input ---
        Vector2 lookInput = Vector2.zero;
        Vector2 moveInput = Vector2.zero;

        if (PlayerInputActionsInstance.Player.enabled)
        {
            lookInput = PlayerInputActionsInstance.Player.Look.ReadValue<Vector2>();
            moveInput = PlayerInputActionsInstance.Player.Move.ReadValue<Vector2>();
        }

        Vector3 positionalOffset = Vector3.zero;
        positionalOffset += new Vector3(-lookInput.x * lookPositionSwayAmount, -lookInput.y * lookPositionSwayAmount * 0.5f, 0f);
        positionalOffset += new Vector3(-moveInput.x * movePositionSwayAmount, 0f, -moveInput.y * movePositionSwayAmount * 0.75f);

        positionalOffset = lanternHoldTarget.TransformDirection(positionalOffset);
        positionalOffset = Vector3.ClampMagnitude(positionalOffset, maxPositionalSway);
        Vector3 targetPosWithSway = baseTargetPos + positionalOffset;

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
        float rotLerpFactor = (rotationSmoothTime > 0.001f) ? Time.deltaTime / rotationSmoothTime : 1.0f;
        currentSwayRotation = Quaternion.Slerp(currentSwayRotation, targetRotWithSway, rotLerpFactor);

        transform.SetPositionAndRotation(currentSwayPosition, currentSwayRotation);
    }

    public void ResetSway(bool useTargetOffset = true)
    {
        if (!isInitialized || !lanternHoldTarget) // Don't try to reset if not properly initialized
        {
            // Debug.LogWarning("PhysicsLanternSway: Attempted to ResetSway before initialization or without lanternHoldTarget.");
            return;
        }

        currentAppliedLocalOffset = useTargetOffset ? targetLocalOffset : Vector3.zero;
        localOffsetVelocity = Vector3.zero;

        currentSwayPosition = lanternHoldTarget.TransformPoint(currentAppliedLocalOffset);
        currentSwayRotation = lanternHoldTarget.rotation;
        transform.SetPositionAndRotation(currentSwayPosition, currentSwayRotation);
        positionVelocity = Vector3.zero;

        if (swingingLanternBodyRB != null)
        {
            swingingLanternBodyRB.linearVelocity = Vector3.zero;
            swingingLanternBodyRB.angularVelocity = Vector3.zero;
        }
    }

    public void SetTargetLocalOffsetImmediate(Vector3 offset)
    {
        targetLocalOffset = offset;
        // If not initialized yet, InitializeSway will pick up this targetLocalOffset.
        // If already initialized, update currentAppliedLocalOffset directly.
        if (isInitialized && lanternHoldTarget) 
        {
            currentAppliedLocalOffset = offset;
            localOffsetVelocity = Vector3.zero;
            currentSwayPosition = lanternHoldTarget.TransformPoint(currentAppliedLocalOffset);
            // No need to immediately snap rotation here, just position based on offset
            transform.position = currentSwayPosition; // Snap position
        }
    }
}