// PlayerMovement.cs
using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Move Settings")]
    [SerializeField, Range(0f, 20f)] private float moveSpeed = 10f;
    [SerializeField, Range(1f, 10f)] private float directionLerpSpeed = 5f;
    [SerializeField, Range(1f, 10f)] private float moveLerpSpeed = 2f;
    [SerializeField] private LayerMask groundMask;

    [Header("Sprint Settings")]
    [SerializeField, Range(0f, 20f)] private float sprintSpeedIncrement = 3f;

    [Header("Crouch Settings")]
    [SerializeField, Range(1f, 10f)] private float crouchSpeed = 4f;
    [SerializeField, Range(1f, 2f)] private float crouchHeight = 1f;
    [SerializeField, Range(1f, 4f)] private float standingHeight = 2f;
    [SerializeField, Range(1f, 10f)] private float crouchLerpSpeed = 8f;

    // --- NEW: Footstep Settings (Simple) ---
    [Header("Footstep Settings (Simple)")]
    [SerializeField] private PlayerFootsteps playerFootsteps; // Assign your PlayerFootsteps script here in the Inspector
    [SerializeField, Range(0.01f, 1.0f)] private float minMovementSpeedForFootsteps = 0.5f; // Minimum horizontal speed to trigger footsteps
    [SerializeField, Range(0.1f, 2.0f)] private float baseFootstepInterval = 0.5f; // Time in seconds between footsteps when walking normally (e.g., 0.5 for 2 steps/sec)
    [SerializeField, Range(0.1f, 1.0f)] private float sprintFootstepMultiplier = 0.7f; // Multiplier for interval when sprinting (e.g., 0.7 makes steps 30% faster)
    [SerializeField, Range(1.0f, 3.0f)] private float crouchFootstepMultiplier = 1.5f; // Multiplier for interval when crouching (e.g., 1.5 makes steps 50% slower)
    // --- END NEW ---

    //const
    private float gravity = -9.81f;
    private float groundCheckDistance = 0.4f;
    private float headCheckDistance = 0.4f;

    //intermediate
    [NonSerialized] public bool isMoving = false;
    [NonSerialized] public bool isCrouching = false;
    [NonSerialized] public bool isSprinting = false;
    [NonSerialized] public bool isGrounded = true;
    private Vector2 move;
    private Vector3 velocity;
    private float finalSpeed;
    private Vector3 currentDirection = Vector3.zero;
    Vector3 lastPos;
    private Vector2 rawInput;
    private Vector3 inputDirection;
    private float targetSpeed;
    private float signedSpeed;
    [NonSerialized] public float speedModifier = 1;

    // --- NEW: Footstep Timer Variable ---
    private float timeToNextFootstep; // Counts down to 0 to trigger a step
    // --- END NEW ---

    //references
    private CharacterController controller;
    private PlayerInput controls;
    private Transform playerCamera;

    private void Awake()
    {
        Debug.Log("PlayerMovement Awake: Initializing.");
        lastPos = transform.position;
        headCheckDistance = standingHeight - crouchHeight;
        finalSpeed = moveSpeed;
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>().transform;
        controller.height = standingHeight;
        playerCamera.position = new Vector3(playerCamera.transform.position.x, standingHeight, playerCamera.transform.position.z);

        // --- NEW: Initialize footstep timer to allow first step quickly ---
        timeToNextFootstep = 0f;
        // --- END NEW ---
    }
    private void OnEnable()
    {
        controls = new PlayerInput();
        controls.Enable();
    }
    private void OnDisable()
    {
        controls.Disable();
    }
    private void Update()
    {
        Crouch();
        Gravity();
        Move(); // This method calculates movement and updates velocity

        // --- NEW: Call the footstep handling logic ---
        HandleSimpleFootsteps();
        // --- END NEW ---
    }

    private void Move()
    {
        ReadInput();
        UpdateDirection();
        CalculateTargetSpeed();
        SmoothSpeedTransition();
        ApplyMovement();
    }

    // Read the raw Vector2 input and derive movement state
    private void ReadInput()
    {
        rawInput = controls.Player.Move.ReadValue<Vector2>();
        isMoving = rawInput.magnitude > 0.01f;
    }

    // Compute the desired direction vector (normalized)
    private void UpdateDirection()
    {
        Vector3 forwardComponent = rawInput.y * transform.forward;
        Vector3 rightComponent = rawInput.x * transform.right;
        Vector3 desired = forwardComponent + rightComponent;

        float lerpSpeed = isMoving ? directionLerpSpeed : moveLerpSpeed;
        Vector3 targetDir = isMoving ? desired.normalized : Vector3.zero;
        currentDirection = Vector3.Lerp(currentDirection, targetDir, Time.deltaTime * lerpSpeed);
    }

    // Determine what the target speed should be
    private void CalculateTargetSpeed()
    {
        // Base walk/crouch speed
        targetSpeed = isCrouching ? crouchSpeed : moveSpeed;
        signedSpeed = GetSignedMovementSpeed();

        if (signedSpeed < -0.1f)
        {
            // Backwards
            targetSpeed *= 0.5f;
            isSprinting = false;
        }
        else if (ShouldStartSprinting())
        {
            targetSpeed += sprintSpeedIncrement;
            isSprinting = true;
        }
        else if (!isMoving)
        {
            // No movement input → no speed
            targetSpeed = 0f;
            isSprinting = false;
        }

        targetSpeed *= speedModifier;
    }

    // Helper to decide sprint conditions
    private bool ShouldStartSprinting()
    {
        return controls.Player.Sprint.inProgress
            && isMoving
            && !isCrouching
            && signedSpeed > 0.1f;
    }

    // Smoothly lerp from current speed to targetSpeed
    private void SmoothSpeedTransition()
    {
        finalSpeed = Mathf.Lerp(finalSpeed, targetSpeed, Time.deltaTime * moveLerpSpeed);
    }

    // Finally apply movement to the character controller
    private void ApplyMovement()
    {
        Vector3 displacement = finalSpeed * Time.deltaTime * currentDirection;
        controller.Move(displacement);
    }

    private void Gravity()
    {
        isGrounded = Physics.CheckSphere(transform.position, groundCheckDistance, groundMask);
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    private bool CheckHeadBump()
    {
        Vector3 checkPoint = transform.position;
        checkPoint.y += controller.height;

        int playerLayerMask = 1 << LayerMask.NameToLayer("Player");
        int maskExcludingPlayer = ~playerLayerMask;

        Vector3 dir = checkPoint;
        dir.y += headCheckDistance;
        dir -= checkPoint;

        Ray ray = new Ray(checkPoint, dir);

        return Physics.Raycast(ray, headCheckDistance, maskExcludingPlayer, QueryTriggerInteraction.Ignore);
    }
    private void Crouch()
    {
        if (controls.Player.Crouch.triggered)
        {
            if (isCrouching && CheckHeadBump()) return; //dont uncrouch if head bumps

            isCrouching = !isCrouching;
            controller.height = isCrouching ? crouchHeight : standingHeight;
            Vector3 controllerCenter = controller.center;
            controllerCenter.y = controller.height * 0.5f;
            controller.center = controllerCenter;
        }
        SmoothCameraHeight();
    }
    private void SmoothCameraHeight()
    {
        Vector3 camPos = playerCamera.transform.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, isCrouching ? crouchHeight : standingHeight, Time.deltaTime * crouchLerpSpeed);
        playerCamera.transform.localPosition = camPos;
    }

    public float GetMovementSpeed()
    {
        Vector3 v = controller.velocity;
        v.y = 0f; // Ignore vertical speed for footsteps
        //Debug.Log(v.magnitude);
        return v.magnitude;
    }

    public float GetSignedMovementSpeed()
    {
        Vector3 delta = transform.position - lastPos;
        float signedSpeed = Vector3.Dot(delta / Time.deltaTime, transform.forward);
        lastPos = transform.position;
        return signedSpeed;
    }

    // --- NEW: Simple Footstep Logic Method (Refined) ---
    private void HandleSimpleFootsteps()
    {
        if (playerFootsteps == null)
        {
            //Debug.LogWarning("PlayerMovement HandleSimpleFootsteps: PlayerFootsteps script NOT assigned! Cannot play footsteps.");
            return;
        }

        float currentHorizontalSpeed = GetMovementSpeed();
        // Debug.Log($"PlayerMovement HandleSimpleFootsteps: isGrounded={isGrounded}, currentHorizontalSpeed={currentHorizontalSpeed}");

        // Condition for playing footsteps: Player must be grounded AND moving above a threshold
        bool shouldPlayFootsteps = isGrounded && currentHorizontalSpeed > minMovementSpeedForFootsteps;

        if (shouldPlayFootsteps)
        {
            // Calculate the desired time interval between footsteps based on current state
            float effectiveFootstepInterval = baseFootstepInterval;
            if (isSprinting)
            {
                effectiveFootstepInterval *= sprintFootstepMultiplier; // Shorter interval (faster steps)
            }
            else if (isCrouching)
            {
                effectiveFootstepInterval *= crouchFootstepMultiplier; // Longer interval (slower steps)
            }

            // Decrement the timer until the next footstep is due
            timeToNextFootstep -= Time.deltaTime;

            // If the timer has counted down to zero or below
            if (timeToNextFootstep <= 0f)
            {
                playerFootsteps.PlayFootstep(); // Trigger the footstep sound

                // Reset the timer for the *next* footstep
                // Add the interval. If it's still <= 0 (e.g., from high speed or low framerate),
                // add another interval until it becomes positive. This prevents playing multiple
                // sounds rapidly if a large amount of time passes in one frame.
                timeToNextFootstep += effectiveFootstepInterval;
                while (timeToNextFootstep <= 0f) // Handle cases where Time.deltaTime is very large
                {
                    timeToNextFootstep += effectiveFootstepInterval;
                }

                // Debug.Log($"PlayerMovement HandleSimpleFootsteps: Footstep triggered! Next step in {timeToNextFootstep:F2}s");
            }
        }
        else // Player is not grounded OR not moving fast enough
        {
            // Reset the timer. This ensures that when the player starts moving again or lands,
            // a footstep can play quickly without waiting for a long accumulated interval.
            timeToNextFootstep = 0f;
            // Debug.Log("PlayerMovement HandleSimpleFootsteps: Not moving or not grounded, resetting footstep timer.");
        }
    }
    // --- END NEW ---
}