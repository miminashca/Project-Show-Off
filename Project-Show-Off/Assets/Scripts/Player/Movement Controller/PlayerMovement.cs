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

    [Header("Footstep Settings (Simple)")]
    [SerializeField] private PlayerFootsteps playerFootsteps;
    [SerializeField, Range(0.01f, 1.0f)] private float minMovementSpeedForFootsteps = 0.5f;
    [SerializeField, Range(0.1f, 2.0f)] private float baseFootstepInterval = 0.5f;
    [SerializeField, Range(0.1f, 1.0f)] private float sprintFootstepMultiplier = 0.7f;
    [SerializeField, Range(1.0f, 3.0f)] private float crouchFootstepMultiplier = 1.5f;

    [Header("Stamina Settings")]
    [SerializeField, Range(1f, 200f)] private float maxStamina = 100f;
    [SerializeField, Range(0.1f, 50f)] private float staminaDrainRate = 15f;
    [SerializeField, Range(0.1f, 50f)] private float staminaRegenRate = 10f;
    [SerializeField, Range(0f, 5f)] private float staminaRegenDelay = 2f;
    [SerializeField, Range(0f, 50f)] private float minStaminaToSprint = 5f;

    //const
    private float gravity = -9.81f;
    private float groundCheckDistance = 0.4f;
    private float headCheckDistance; // Initialized in Awake based on heights

    //intermediate
    [NonSerialized] public bool isMoving = false;
    [NonSerialized] public bool isCrouching = false;
    [NonSerialized] public bool isSprinting = false;
    [NonSerialized] public bool isGrounded = true;
    private Vector3 velocity;
    private float finalSpeed;
    private Vector3 currentDirection = Vector3.zero;
    private Vector3 lastPosForSignedSpeed; // Renamed for clarity vs previousFramePosition
    private Vector2 rawInput;
    private float targetSpeed;
    private float signedSpeedFromController; // Renamed for clarity
    [NonSerialized] public float speedModifier = 1;

    private float timeToNextFootstep;

    private float currentStamina;
    private float timeSinceStoppedSprinting = 0f;
    private Vector3 previousFramePosition;

    //references
    private CharacterController controller;
    private PlayerInput controls;
    private Transform playerCamera;

    private void Awake()
    {
        lastPosForSignedSpeed = transform.position;
        previousFramePosition = transform.position;

        // Calculate headCheckDistance based on configured heights
        headCheckDistance = (standingHeight - crouchHeight) * 0.9f; // A bit less to avoid issues
        if (headCheckDistance < 0.01f) headCheckDistance = 0.01f; // Ensure it's a small positive value


        finalSpeed = moveSpeed;
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>().transform;
        controller.height = standingHeight;
        Vector3 camLocalPos = playerCamera.localPosition;
        camLocalPos.y = standingHeight;
        playerCamera.localPosition = camLocalPos;

        timeToNextFootstep = 0f;
        currentStamina = maxStamina;
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
        ReadInput();

        Crouch();
        
        HandleStamina();
        // Debug.Log($"isSprinting after HandleStamina: {isSprinting}, Stamina: {currentStamina}");

        Gravity();
        Move();
        HandleSimpleFootsteps();

        previousFramePosition = transform.position;
    }

    private void HandleStamina()
    {
        bool sprintInputActive = controls.Player.Sprint.inProgress;
        bool canPotentiallySprint = sprintInputActive && isMoving && !isCrouching;

        //Debug.Log($"HandleStamina - Input: {sprintInputActive}, isMoving: {isMoving}, !isCrouching: {!isCrouching}, CanPotentiallySprint: {canPotentiallySprint}");

        if (isSprinting)
        {
            if (canPotentiallySprint && currentStamina > 0)
            {
                currentStamina -= staminaDrainRate * Time.deltaTime;
                currentStamina = Mathf.Max(0, currentStamina);
                timeSinceStoppedSprinting = 0f;
            }
            else
            {
                isSprinting = false;
                // Debug.Log("Stopping sprint: conditions no longer met or stamina depleted.");
            }
        }
        else // Not currently sprinting
        {
            if (canPotentiallySprint && currentStamina > minStaminaToSprint)
            {
                isSprinting = true;
                // Debug.Log("Starting sprint.");
                currentStamina -= staminaDrainRate * Time.deltaTime; // Initial drain for this frame
                currentStamina = Mathf.Max(0, currentStamina);
                timeSinceStoppedSprinting = 0f;
            }
        }

        if (!isSprinting && currentStamina < maxStamina)
        {
            timeSinceStoppedSprinting += Time.deltaTime;
            if (timeSinceStoppedSprinting >= staminaRegenDelay)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);
            }
        }
    }

    private void Move()
    {
        UpdateDirection();
        CalculateTargetSpeed(); // Uses 'isSprinting' state set by HandleStamina
        SmoothSpeedTransition();
        ApplyMovement();
    }

    private void ReadInput()
    {
        rawInput = controls.Player.Move.ReadValue<Vector2>();
        isMoving = rawInput.magnitude > 0.01f;
    }

    private void UpdateDirection()
    {
        Vector3 forwardComponent = rawInput.y * transform.forward;
        Vector3 rightComponent = rawInput.x * transform.right;
        Vector3 desired = forwardComponent + rightComponent;

        float lerpSpeed = isMoving ? directionLerpSpeed : moveLerpSpeed;
        Vector3 targetDir = isMoving ? desired.normalized : Vector3.zero;
        currentDirection = Vector3.Lerp(currentDirection, targetDir, Time.deltaTime * lerpSpeed);
    }

    // --- MODIFIED CalculateTargetSpeed ---
    private void CalculateTargetSpeed()
    {
        // 'isSprinting' is now definitively set by HandleStamina()
        // 'isCrouching' is determined by Crouch()
        // 'isMoving' is determined by ReadInput()

        if (isSprinting)
        {
            // If HandleStamina says we are sprinting, we apply sprint speed.
            // HandleStamina already verified forward movement and stamina.
            targetSpeed = moveSpeed + sprintSpeedIncrement;
        }
        else // Not sprinting
        {
            signedSpeedFromController = GetSignedMovementSpeedFromController(); // Calculate for backward check etc.

            if (isCrouching)
            {
                targetSpeed = crouchSpeed;
            }
            else if (isMoving)
            {
                if (signedSpeedFromController < -0.05f) // Moving backward (threshold can be adjusted)
                {
                    targetSpeed = moveSpeed * 0.5f; // Penalty for backward movement
                }
                else // Moving forward or sideways (walking)
                {
                    targetSpeed = moveSpeed;
                }
            }
            else // Not moving
            {
                targetSpeed = 0f;
            }
        }

        targetSpeed *= speedModifier;
        // Debug.Log($"CalculateTargetSpeed - isSprinting: {isSprinting}, TargetSpeed: {targetSpeed}, SignedSpeedCtrl: {signedSpeedFromController}");
    }
    // --- END MODIFIED CalculateTargetSpeed ---

    private void SmoothSpeedTransition()
    {
        finalSpeed = Mathf.Lerp(finalSpeed, targetSpeed, Time.deltaTime * moveLerpSpeed);
    }

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
        // Start ray from slightly inside the current top of the controller to avoid self-collision
        Vector3 rayStart = transform.position + controller.center + Vector3.up * (controller.height * 0.5f - controller.radius * 0.5f) ;

        // Distance to check upwards is the difference to standing height
        float checkDist = standingHeight - controller.height;
        if (checkDist <= controller.skinWidth + 0.01f) return false; // Already standing or very close, or checkDist is too small

        // Raycast upwards
        // Debug.DrawRay(rayStart, Vector3.up * checkDist, Color.red, 2f);
        return Physics.SphereCast(rayStart, controller.radius * 0.9f, Vector3.up, out RaycastHit hit, checkDist, groundMask, QueryTriggerInteraction.Ignore);
    }
    private void Crouch()
    {
        if (controls.Player.Crouch.triggered)
        {
            if (isCrouching && CheckHeadBump())
            {
                // Debug.Log("Head bump detected, cannot stand.");
                return;
            }
            isCrouching = !isCrouching;
            if (isCrouching) isSprinting = false; // Cannot sprint while crouching, ensure isSprinting is false if we start crouching
        }

        float targetHeightCurrent = isCrouching ? crouchHeight : standingHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeightCurrent, Time.deltaTime * crouchLerpSpeed);

        Vector3 controllerCenter = controller.center;
        controllerCenter.y = controller.height * 0.5f;
        controller.center = controllerCenter;

        SmoothCameraHeight(targetHeightCurrent);
    }

    private void SmoothCameraHeight(float targetPlayerHeight)
    {
        Vector3 camPos = playerCamera.transform.localPosition;
        float targetCamY = targetPlayerHeight; // Adjust if camera pivot is different
        
        camPos.y = Mathf.Lerp(camPos.y, targetCamY, Time.deltaTime * crouchLerpSpeed);
        playerCamera.transform.localPosition = camPos;
    }

    public float GetHorizontalMovementSpeed() // Renamed for clarity
    {
        Vector3 horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0f;
        return horizontalVelocity.magnitude;
    }

    public float GetSignedMovementSpeedFromController() // Renamed for clarity
    {
        if (Time.deltaTime == 0) return 0f;
        // This measures speed based on displacement since the last call to this specific function
        Vector3 delta = transform.position - lastPosForSignedSpeed;
        float speed = Vector3.Dot(delta / Time.deltaTime, transform.forward);
        lastPosForSignedSpeed = transform.position;
        return speed;
    }
    public float GetMovementSpeed() // This is based on CharacterController.velocity magnitude
    {
        Vector3 horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0f;
        return horizontalVelocity.magnitude;
    }

    private void HandleSimpleFootsteps()
    {
        if (playerFootsteps == null) return;

        float currentHorizontalSpeed = GetHorizontalMovementSpeed();
        bool shouldPlayFootsteps = isGrounded && currentHorizontalSpeed > minMovementSpeedForFootsteps;

        float movementStateValue = 0.5f;
        if (isSprinting) movementStateValue = 1.0f;
        else if (isCrouching) movementStateValue = 0.0f;
        playerFootsteps.SetMovementState(movementStateValue);

        if (shouldPlayFootsteps)
        {
            float effectiveFootstepInterval = baseFootstepInterval;
            if (isSprinting) effectiveFootstepInterval *= sprintFootstepMultiplier;
            else if (isCrouching) effectiveFootstepInterval *= crouchFootstepMultiplier;

            timeToNextFootstep -= Time.deltaTime;

            if (timeToNextFootstep <= 0f)
            {
                playerFootsteps.PlayFootstep();
                timeToNextFootstep += effectiveFootstepInterval;
                if(timeToNextFootstep < 0) timeToNextFootstep = effectiveFootstepInterval * 0.1f;
            }
        }
        else
        {
            timeToNextFootstep = baseFootstepInterval * 0.1f;
        }
    }

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
}