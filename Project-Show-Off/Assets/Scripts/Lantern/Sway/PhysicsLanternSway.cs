using UnityEngine;
using FMODUnity; // Keep for the squeak sounds

public class PhysicsLanternSway : MonoBehaviour
{
    [Header("Follow Target")]
    [Tooltip("The Transform the hand should follow (e.g., an anchor on the camera).")]
    public Transform lanternHoldTarget; // This is your lanternHandAnchor
    
    [Header("References")]
    public Rigidbody swingingLanternBodyRB; // For squeak sounds

    [Header("Target Offset (for Raise/Lower)")]
    [Tooltip("The target local offset from the lanternHoldTarget. Set by LanternController.")]
    public Vector3 targetLocalOffset = Vector3.zero;
    [Tooltip("How quickly the hand animates to the targetLocalOffset (e.g., for raising/lowering).")]
    public float localOffsetSmoothTime = 0.2f;

    [Header("Positional Lag")]
    [Tooltip("How smoothly the hand follows the target's position. Lower values are 'tighter', higher values are more 'laggy'.")]
    public float positionSmoothTime = 0.1f;
    
    [Header("Rotational Lag")]
    [Tooltip("How smoothly the hand follows the target's rotation. Lower values are 'tighter', higher values are more 'laggy'.")]
    public float rotationSmoothTime = 0.12f;

    // --- Private state variables ---
    private Vector3 currentAppliedLocalOffset;
    private Vector3 localOffsetVelocity;
    private Vector3 currentPositionVelocity;

    private PlayerInput PlayerInputActionsInstance;
    private bool isInitialized = false;

    [Header("FMOD Squeak Sound")]
    [SerializeField] private EventReference lanternSqueakEvent;
    [SerializeField] private float squeakAngularVelocityThreshold = 2.5f;
    [SerializeField] private float squeakCooldown = 0.5f;
    private float lastSqueakTime = -1f;

    // We don't need to pass the camera or handle RB anymore
    public void InitializeSway(PlayerInput inputActions, Transform holdTarget, Rigidbody swingRB)
    {
        PlayerInputActionsInstance = inputActions;
        lanternHoldTarget = holdTarget;
        swingingLanternBodyRB = swingRB;

        if (PlayerInputActionsInstance == null) Debug.LogError("Sway: PlayerInput not assigned!");
        if (lanternHoldTarget == null) Debug.LogError("Sway: Hold Target not assigned!");
        
        // Snap to the initial position to avoid a jump on startup
        transform.position = lanternHoldTarget.position;
        transform.rotation = lanternHoldTarget.rotation;

        currentAppliedLocalOffset = targetLocalOffset;
        localOffsetVelocity = Vector3.zero;

        isInitialized = true;
        if (!enabled) enabled = true;
    }

    // Use LateUpdate to apply movement AFTER the camera has moved for the frame.
    void LateUpdate()
    {
        if (!isInitialized || !enabled || lanternHoldTarget == null) return;

        // --- Step 1: Calculate the GOAL position and rotation ---

        // Smoothly update the raise/lower offset
        currentAppliedLocalOffset = Vector3.SmoothDamp(currentAppliedLocalOffset, targetLocalOffset, ref localOffsetVelocity, localOffsetSmoothTime, Mathf.Infinity, Time.deltaTime);
        
        // The goal position is the target anchor's position plus our smoothed offset
        Vector3 targetPosition = lanternHoldTarget.TransformPoint(currentAppliedLocalOffset);
        
        // The goal rotation is simply the target anchor's rotation
        Quaternion targetRotation = lanternHoldTarget.rotation;

        // --- Step 2: Smoothly move THIS object towards the goal ---
        
        // Positional Damping
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentPositionVelocity, positionSmoothTime);

        // Rotational Damping (Slerp is great for this)
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1 - Mathf.Exp(-1 / rotationSmoothTime * Time.deltaTime));

        // Note: The previous sway logic based on mouse input is removed. 
        // The lag from following the camera provides a much more natural sway.
        // If you still want that extra sway, it can be added back to the targetPosition/targetRotation calculation.
    }

    // Squeak logic can remain in a regular Update
    void Update()
    {
        if (!isInitialized) return;
        HandleLanternSqueak();
    }
    
    void HandleLanternSqueak()
    {
        if (swingingLanternBodyRB == null || lanternSqueakEvent.IsNull || !this.enabled) return;
        if (Time.time < lastSqueakTime + squeakCooldown) return;
        if (swingingLanternBodyRB.angularVelocity.magnitude > squeakAngularVelocityThreshold)
        {
            RuntimeManager.PlayOneShotAttached(lanternSqueakEvent, swingingLanternBodyRB.gameObject);
            lastSqueakTime = Time.time;
        }
    }

    // Simplified reset
    public void ResetSway()
    {
        if (!isInitialized || lanternHoldTarget == null) return;

        currentAppliedLocalOffset = targetLocalOffset;
        localOffsetVelocity = Vector3.zero;
        currentPositionVelocity = Vector3.zero;

        transform.position = lanternHoldTarget.TransformPoint(currentAppliedLocalOffset);
        transform.rotation = lanternHoldTarget.rotation;
    }

    // Simplified offset setter
    public void SetTargetLocalOffsetImmediate(Vector3 offset)
    {
        targetLocalOffset = offset;
        if (isInitialized)
        {
            currentAppliedLocalOffset = offset;
            localOffsetVelocity = Vector3.zero;
        }
    }
}