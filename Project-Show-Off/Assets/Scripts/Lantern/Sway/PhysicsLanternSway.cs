using UnityEngine;
// NEW CHANGE
using FMODUnity; // Required for FMOD EventReference and RuntimeManager
// END CHANGE

public class PhysicsLanternSway : MonoBehaviour
{
    [Header("References")]
    public Transform playerCameraTransform;
    public Transform lanternHoldTarget;

    [Header("Physics Parts")]
    public Rigidbody handleRigidbody;
    public Rigidbody swingingLanternBodyRB; // This is key for squeak detection

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

    public PlayerInput PlayerInputActionsInstance { get; private set; }

    private bool isInitialized = false;

    // NEW CHANGE
    [Header("FMOD Squeak Sound")]
    [SerializeField]
    private EventReference lanternSqueakEvent; // Assign your FMOD event here in the Inspector
    [SerializeField]
    [Tooltip("The magnitude of angular velocity (radians/sec) of swingingLanternBodyRB needed to trigger a squeak.")]
    private float squeakAngularVelocityThreshold = 2.5f; // Example value, tune this!
    [SerializeField]
    [Tooltip("Minimum time (seconds) between squeak sounds.")]
    private float squeakCooldown = 0.5f; // Example value, tune this!

    private float lastSqueakTime = -1f; // Initialize to allow the first squeak immediately if conditions met
    // END CHANGE

    public void InitializeSway(PlayerInput inputActions, Transform camTransform, Transform holdTarget, Rigidbody handleRB, Rigidbody swingRB)
    {
        PlayerInputActionsInstance = inputActions;
        playerCameraTransform = camTransform;
        lanternHoldTarget = holdTarget;
        handleRigidbody = handleRB;
        swingingLanternBodyRB = swingRB; // Make sure this is assigned

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
            enabled = false;
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
            // For squeaks, it's essential.
            Debug.LogWarning("PhysicsLanternSway: swingingLanternBodyRB not assigned during Initialize. Lantern squeak sounds will not work.", this);
        }

        currentAppliedLocalOffset = targetLocalOffset;
        localOffsetVelocity = Vector3.zero;

        if (lanternHoldTarget != null)
        {
            currentSwayPosition = lanternHoldTarget.TransformPoint(currentAppliedLocalOffset);
            currentSwayRotation = lanternHoldTarget.rotation;
            transform.SetPositionAndRotation(currentSwayPosition, currentSwayRotation);
            positionVelocity = Vector3.zero;
        }

        isInitialized = true;
        if (!enabled) enabled = true;
    }

    void Update()
    {
        if (!isInitialized || !enabled || !lanternHoldTarget || PlayerInputActionsInstance == null) return;

        currentAppliedLocalOffset = Vector3.SmoothDamp(currentAppliedLocalOffset, targetLocalOffset, ref localOffsetVelocity, localOffsetSmoothTime, Mathf.Infinity, Time.deltaTime);

        Vector3 baseTargetPos = lanternHoldTarget.TransformPoint(currentAppliedLocalOffset);
        Quaternion baseTargetRot = lanternHoldTarget.rotation;

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

        currentSwayPosition = Vector3.SmoothDamp(currentSwayPosition, targetPosWithSway, ref positionVelocity, positionSmoothTime, Mathf.Infinity, Time.deltaTime);
        float rotLerpFactor = (rotationSmoothTime > 0.001f) ? Time.deltaTime / rotationSmoothTime : 1.0f;
        currentSwayRotation = Quaternion.Slerp(currentSwayRotation, targetRotWithSway, rotLerpFactor);

        transform.SetPositionAndRotation(currentSwayPosition, currentSwayRotation);

        // NEW CHANGE
        HandleLanternSqueak();
        // END CHANGE
    }

    // NEW CHANGE
    void HandleLanternSqueak()
    {
        // Ensure the swinging body Rigidbody is assigned, the FMOD event is valid, and the script is active
        if (swingingLanternBodyRB == null || lanternSqueakEvent.IsNull || !this.enabled)
        {
            return;
        }

        // Check if enough time has passed since the last squeak
        if (Time.time < lastSqueakTime + squeakCooldown)
        {
            return;
        }

        // Get the current angular speed of the lantern's swinging part
        float currentAngularSpeed = swingingLanternBodyRB.angularVelocity.magnitude;

        // If the speed exceeds the threshold, play the squeak sound
        if (currentAngularSpeed > squeakAngularVelocityThreshold)
        {
            RuntimeManager.PlayOneShotAttached(lanternSqueakEvent, swingingLanternBodyRB.gameObject);
            lastSqueakTime = Time.time; // Update the time of the last squeak
        }
    }
    // END CHANGE

    public void ResetSway(bool useTargetOffset = true)
    {
        if (!isInitialized || !lanternHoldTarget)
        {
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
        if (isInitialized && lanternHoldTarget)
        {
            currentAppliedLocalOffset = offset;
            localOffsetVelocity = Vector3.zero;
            currentSwayPosition = lanternHoldTarget.TransformPoint(currentAppliedLocalOffset);
            transform.position = currentSwayPosition;
        }
    }
}