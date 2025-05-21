using UnityEngine;

public class PhysicsLanternSway : MonoBehaviour
{
    [Header("References")]
    public Transform playerCameraTransform;
    public Transform lanternHoldTarget;

    [Header("Physics Parts (for Reset)")]
    public Rigidbody handleRigidbody;
    public Rigidbody swingingLanternBodyRB;

    [Header("Target Offset (for Raise/Lower)")]
    [Tooltip("The target local offset from the lanternHoldTarget. Set by LanternController.")]
    public Vector3 targetLocalOffset = Vector3.zero;
    [Tooltip("How quickly the lantern animates to the targetLocalOffset (e.g., for raising/lowering).")]
    public float localOffsetSmoothTime = 0.2f; // This will replace raiseAnimationDuration
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

    public PlayerInput PlayerInputActionsInstance { get; set; }

    void Start()
    {
        if (PlayerInputActionsInstance == null)
        {
            Debug.LogError("PhysicsLanternSway: PlayerInputActionsInstance was not assigned! Sway will not react to input correctly.", this);
        }

        if (!lanternHoldTarget)
        {
            Debug.LogError("PhysicsLanternSway: LanternHoldTarget not assigned! Disabling script.", this);
            enabled = false;
            return;
        }
        if (handleRigidbody != null) handleRigidbody.isKinematic = true;

        // Initialize currentAppliedLocalOffset to the initial targetLocalOffset
        currentAppliedLocalOffset = targetLocalOffset;

        // Initialize State based on the possibly offset position
        currentSwayPosition = lanternHoldTarget.TransformPoint(currentAppliedLocalOffset);
        currentSwayRotation = lanternHoldTarget.rotation;
        transform.SetPositionAndRotation(currentSwayPosition, currentSwayRotation);
    }

    void Update()
    {
        if (!enabled || !lanternHoldTarget) return;
        if (PlayerInputActionsInstance == null) return;

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

        // Transform positionalOffset to be relative to the lanternHoldTarget's orientation,
        // then apply it in world space.
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
            // Calculate target rotation to align 'up' with 'world down'
            Quaternion gravityAlignedTarget = Quaternion.FromToRotation(currentUp, Vector3.down) * transform.rotation;

            float inputMagnitude = lookInput.magnitude + moveInput.magnitude;
            float dynamicInfluence = Mathf.Lerp(overallGravityInfluence, overallGravityInfluence * 0.2f, Mathf.Clamp01(inputMagnitude)); // Reduce influence during fast movement/look
            targetRotWithSway = Quaternion.Slerp(targetRotWithSway, gravityAlignedTarget, dynamicInfluence * overallSettlingSpeed * Time.deltaTime);
        }

        targetRotWithSway = Quaternion.RotateTowards(baseTargetRot, targetRotWithSway, maxRotationalSway);

        // --- 3. Smoothly Update Transform ---
        currentSwayPosition = Vector3.SmoothDamp(currentSwayPosition, targetPosWithSway, ref positionVelocity, positionSmoothTime, Mathf.Infinity, Time.deltaTime);
        // For rotation, using a Lerp factor derived from rotationSmoothTime provides smoother results than Slerp with a fixed step.
        float rotLerpFactor = (rotationSmoothTime > 0.001f) ? Time.deltaTime / rotationSmoothTime : 1.0f;
        currentSwayRotation = Quaternion.Slerp(currentSwayRotation, targetRotWithSway, rotLerpFactor);


        transform.SetPositionAndRotation(currentSwayPosition, currentSwayRotation);
    }

    public void ResetSway(bool useTargetOffset = true) // Added parameter
    {
        if (lanternHoldTarget)
        {
            // When resetting, decide if we snap to the current targetLocalOffset or (0,0,0) local.
            // Typically, we want to respect the current targetOffset (e.g. if raised).
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
                // If your lantern body is parented and might get an odd initial rotation, reset it too
                // swingingLanternBodyRB.transform.localRotation = Quaternion.identity;
            }
            // Reset handle if it's not purely kinematic or might have gotten an odd rotation
            // if (handleRigidbody != null) {
            //    handleRigidbody.transform.localRotation = Quaternion.identity;
            // }
        }
    }

    // Call this to instantly set the local offset without smoothing (e.g., on equip)
    public void SetTargetLocalOffsetImmediate(Vector3 offset)
    {
        targetLocalOffset = offset;
        currentAppliedLocalOffset = offset;
        localOffsetVelocity = Vector3.zero; // Reset velocity to prevent overshoot from a previous SmoothDamp
        // Recalculate currentSwayPosition based on the new immediate offset to prevent a jump
        if (lanternHoldTarget)
        {
            currentSwayPosition = lanternHoldTarget.TransformPoint(currentAppliedLocalOffset);
        }
    }
}