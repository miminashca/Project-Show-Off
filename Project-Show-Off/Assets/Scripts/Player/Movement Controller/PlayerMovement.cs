using FMODUnity;
using FMOD.Studio;
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

    [Header("Visibility Point Control")]
    [Tooltip("The transform representing the player's head for AI visibility. Will be moved during crouch.")]
    [SerializeField] private Transform headTransform;
    [Tooltip("The transform representing the player's torso for AI visibility. Will be moved during crouch.")]
    [SerializeField] private Transform torsoTransform;
    [Tooltip("The Y-position of the head when crouching.")]
    [SerializeField] private float headCrouchY = 0.8f;
    [Tooltip("The Y-position of the torso when crouching.")]
    [SerializeField] private float torsoCrouchY = 0.6f;
    private Vector3 initialHeadLocalPos;
    private Vector3 initialTorsoLocalPos;

    [Header("Stamina Settings")]
    [SerializeField, Range(1f, 200f)] private float maxStamina = 100f;
    [SerializeField, Range(0.1f, 50f)] private float staminaDrainRate = 15f;
    [SerializeField, Range(0.1f, 50f)] private float staminaRegenRate = 10f;
    [SerializeField, Range(0f, 5f)] private float staminaRegenDelay = 2f;
    [SerializeField, Range(0f, 50f)] private float minStaminaToSprint = 5f;

    [Header("Footstep Settings (Simple)")]
    [SerializeField] private PlayerFootsteps playerFootsteps;
    [SerializeField, Range(0.01f, 1.0f)] private float minMovementSpeedForFootsteps = 0.5f;
    [SerializeField, Range(0.1f, 2.0f)] private float baseFootstepInterval = 0.5f;
    [SerializeField, Range(0.1f, 1.0f)] private float sprintFootstepMultiplier = 0.7f;
    [SerializeField, Range(1.0f, 3.0f)] private float crouchFootstepMultiplier = 1.5f;

    // NEW CHANGE
    [Header("FMOD Sprinting Sounds")]
    [Tooltip("FMOD Event for continuous breathing while sprinting. Should have a loop region.")]
    [SerializeField] private EventReference sprintingBreathEventPath;
    [Tooltip("FMOD Event for one-shot breath sound after sprinting stops.")]
    [SerializeField] private EventReference afterSprintingBreathEventPath;

    private EventInstance sprintingBreathInstance;
    private EventInstance afterSprintingBreathInstance; // Instance for the one-shot after-sprint breath
    private bool previousIsSprintingState = false;
    private bool staminaDroppedBelowHalfDuringThisSprint = false; // Flag to track if stamina dropped below half during the current sprint
    // END CHANGE

    //const
    private float gravity = -9.81f;
    private float groundCheckDistance = 0.4f;
    private float headCheckDistance;

    //intermediate
    [NonSerialized] public bool isMoving = false;
    [NonSerialized] public bool isCrouching = false;
    [NonSerialized] public bool isSprinting = false;
    [NonSerialized] public bool isGrounded = true;
    private Vector3 velocity;
    private float finalSpeed;
    private Vector3 currentDirection = Vector3.zero;
    private Vector3 lastPosForSignedSpeed;
    private Vector2 rawInput;
    private float targetSpeed;
    private float signedSpeedFromController;
    [NonSerialized] public float speedModifier = 1;

    private float timeToNextFootstep;

    private float currentStamina;
    private float timeSinceStoppedSprinting = 0f;
    private Vector3 previousFramePosition;

    private CharacterController controller;
    private PlayerInput controls;
    private Transform playerCamera;
    private PlayerStatus playerStatus;

    private void Awake()
    {
        lastPosForSignedSpeed = transform.position;
        previousFramePosition = transform.position;

        headCheckDistance = (standingHeight - crouchHeight) * 0.9f;
        if (headCheckDistance < 0.01f) headCheckDistance = 0.01f;

        playerStatus = GetComponent<PlayerStatus>();
        if (playerStatus == null)
        {
            Debug.LogError("PlayerMovement: PlayerStatus component not found on this GameObject!", this);
        }

        if (headTransform != null) initialHeadLocalPos = headTransform.localPosition;
        else Debug.LogError("PlayerMovement: Head Transform is not assigned!", this);

        if (torsoTransform != null) initialTorsoLocalPos = torsoTransform.localPosition;
        else Debug.LogError("PlayerMovement: Torso Transform is not assigned!", this);

        finalSpeed = moveSpeed;
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>().transform;
        controller.height = standingHeight;
        Vector3 camLocalPos = playerCamera.localPosition;
        camLocalPos.y = standingHeight;
        playerCamera.localPosition = camLocalPos;

        timeToNextFootstep = 0f;
        currentStamina = maxStamina;

        // NEW CHANGE
        if (!sprintingBreathEventPath.IsNull)
        {
            sprintingBreathInstance = RuntimeManager.CreateInstance(sprintingBreathEventPath);
            RuntimeManager.AttachInstanceToGameObject(sprintingBreathInstance, gameObject);
        }
        // Initialize FMOD event instance for after-sprinting breath
        if (!afterSprintingBreathEventPath.IsNull)
        {
            afterSprintingBreathInstance = RuntimeManager.CreateInstance(afterSprintingBreathEventPath);
            RuntimeManager.AttachInstanceToGameObject(afterSprintingBreathInstance, gameObject);
        }
        staminaDroppedBelowHalfDuringThisSprint = false; // Explicitly initialize, though default is false
        // END CHANGE
    }

    private void OnEnable()
    {
        controls = new PlayerInput();
        controls.Enable();
    }
    private void OnDisable()
    {
        controls.Disable();
        // NEW CHANGE
        // Stop sounds with fadeout when disabled
        if (sprintingBreathInstance.isValid())
        {
            sprintingBreathInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
        if (afterSprintingBreathInstance.isValid())
        {
            afterSprintingBreathInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
        isSprinting = false;
        previousIsSprintingState = false;
        staminaDroppedBelowHalfDuringThisSprint = false; // Reset flag on disable
        // END CHANGE
    }

    // NEW CHANGE
    private void OnDestroy()
    {
        // Release FMOD event instances
        if (sprintingBreathInstance.isValid())
        {
            sprintingBreathInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            sprintingBreathInstance.release();
        }
        if (afterSprintingBreathInstance.isValid())
        {
            afterSprintingBreathInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            afterSprintingBreathInstance.release();
        }
    }
    // END CHANGE

    private void Update()
    {
        ReadInput();
        Crouch();
        UpdateVisibilityPointPositions();
        HandleStamina();

        HandleSprintingAudioFMOD();

        Gravity();
        Move();
        HandleSimpleFootsteps();

        previousFramePosition = transform.position;
    }

    // NEW CHANGE
    // Helper method to start an FMOD EventInstance if it's not already playing
    private void StartEventInstanceFMOD(EventInstance eventInstance)
    {
        if (!eventInstance.isValid()) return;

        PLAYBACK_STATE playbackState;
        eventInstance.getPlaybackState(out playbackState);
        if (playbackState != PLAYBACK_STATE.PLAYING && playbackState != PLAYBACK_STATE.STARTING)
        {
            eventInstance.start();
        }
    }

    // Helper method to stop an FMOD EventInstance
    private void StopEventInstanceFMOD(EventInstance eventInstance, FMOD.Studio.STOP_MODE stopMode = FMOD.Studio.STOP_MODE.ALLOWFADEOUT)
    {
        if (!eventInstance.isValid()) return;
        eventInstance.stop(stopMode);
    }

    // Renamed from HandleSprintingAudio and updated with new logic
    private void HandleSprintingAudioFMOD()
    {
        bool justStartedSprinting = isSprinting && !previousIsSprintingState;
        bool justStoppedSprinting = !isSprinting && previousIsSprintingState;

        if (justStartedSprinting)
        {
            // Player started sprinting
            StartEventInstanceFMOD(sprintingBreathInstance);

            // Stop the "after sprinting" breath if it was playing (e.g. player quickly stops/starts)
            StopEventInstanceFMOD(afterSprintingBreathInstance, FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
        else if (justStoppedSprinting)
        {
            // Player stopped sprinting
            StopEventInstanceFMOD(sprintingBreathInstance, FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

            // NEW CHANGE
            // Play the one-shot "after sprinting" breath ONLY if stamina dropped below half during that sprint
            if (staminaDroppedBelowHalfDuringThisSprint)
            {
                StartEventInstanceFMOD(afterSprintingBreathInstance);
            }
            // staminaDroppedBelowHalfDuringThisSprint will be reset by HandleStamina when a new sprint starts.
            // END CHANGE
        }

        previousIsSprintingState = isSprinting;
    }
    // END CHANGE

    private void HandleStamina()
    {
        bool sprintInputActive = controls.Player.Sprint.inProgress;
        bool canPotentiallySprint = sprintInputActive && isMoving && !isCrouching;

        if (isSprinting) // Player is currently in a sprinting state
        {
            if (canPotentiallySprint && currentStamina > 0)
            {
                currentStamina -= staminaDrainRate * Time.deltaTime;
                currentStamina = Mathf.Max(0, currentStamina);
                timeSinceStoppedSprinting = 0f;

                // NEW CHANGE
                // Check if stamina has dropped below half during this sprint session.
                // Only set to true once per sprint if condition met.
                if (!staminaDroppedBelowHalfDuringThisSprint && currentStamina <= maxStamina / 2f)
                {
                    staminaDroppedBelowHalfDuringThisSprint = true;
                }
                // END CHANGE
            }
            else // Conditions to continue sprinting are no longer met (e.g., stamina depleted, stopped moving, crouched)
            {
                isSprinting = false; // Stop sprinting
            }
        }
        else // Player is not currently in a sprinting state
        {
            if (canPotentiallySprint && currentStamina > minStaminaToSprint)
            {
                isSprinting = true; // Start sprinting

                // NEW CHANGE
                // This marks the beginning of a new sprint session, so reset the flag.
                staminaDroppedBelowHalfDuringThisSprint = false;
                // END CHANGE

                // Drain stamina for the frame sprint starts
                currentStamina -= staminaDrainRate * Time.deltaTime;
                currentStamina = Mathf.Max(0, currentStamina);
                timeSinceStoppedSprinting = 0f;

                // NEW CHANGE
                // Check if stamina (after initial drain on this frame) is already below half.
                // This handles cases where starting to sprint immediately pushes stamina below threshold.
                if (!staminaDroppedBelowHalfDuringThisSprint && currentStamina <= maxStamina / 2f)
                {
                    staminaDroppedBelowHalfDuringThisSprint = true;
                }
                // END CHANGE
            }
        }

        // Stamina Regeneration
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
        CalculateTargetSpeed();
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

    private void CalculateTargetSpeed()
    {
        if (isSprinting)
        {
            targetSpeed = moveSpeed + sprintSpeedIncrement;
        }
        else
        {
            signedSpeedFromController = GetSignedMovementSpeedFromController();

            if (isCrouching)
            {
                targetSpeed = crouchSpeed;
            }
            else if (isMoving)
            {
                if (signedSpeedFromController < -0.05f)
                {
                    targetSpeed = moveSpeed * 0.5f;
                }
                else
                {
                    targetSpeed = moveSpeed;
                }
            }
            else
            {
                targetSpeed = 0f;
            }
        }

        targetSpeed *= speedModifier;
    }

    private void SmoothSpeedTransition()
    {
        finalSpeed = Mathf.Lerp(finalSpeed, targetSpeed, Time.deltaTime * moveLerpSpeed);
        //Debug.Log("final speed: " + finalSpeed);
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
        Vector3 rayStart = transform.position + controller.center + Vector3.up * (controller.height * 0.5f - controller.radius * 0.5f);
        float checkDist = standingHeight - controller.height;
        if (checkDist <= controller.skinWidth + 0.01f) return false;
        return Physics.SphereCast(rayStart, controller.radius * 0.9f, Vector3.up, out RaycastHit hit, checkDist, groundMask, QueryTriggerInteraction.Ignore);
    }
    private void Crouch()
    {
        if (controls.Player.Crouch.triggered)
        {
            if (isCrouching && CheckHeadBump())
            {
                return;
            }
            isCrouching = !isCrouching;
            if (playerStatus != null)
            {
                playerStatus.IsCrouching = isCrouching;
            }
            if (isCrouching) isSprinting = false;
        }

        float targetHeightCurrent = isCrouching ? crouchHeight : standingHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeightCurrent, Time.deltaTime * crouchLerpSpeed);

        Vector3 controllerCenter = controller.center;
        controllerCenter.y = controller.height * 0.5f;
        controller.center = controllerCenter;

        SmoothCameraHeight(targetHeightCurrent);
    }

    private void UpdateVisibilityPointPositions()
    {
        if (headTransform == null || torsoTransform == null) return;

        // Determine target local positions based on crouch state
        Vector3 targetHeadPos = isCrouching ? new Vector3(initialHeadLocalPos.x, headCrouchY, initialHeadLocalPos.z) : initialHeadLocalPos;
        Vector3 targetTorsoPos = isCrouching ? new Vector3(initialTorsoLocalPos.x, torsoCrouchY, initialTorsoLocalPos.z) : initialTorsoLocalPos;

        // Smoothly move the transforms to their target positions
        headTransform.localPosition = Vector3.Lerp(headTransform.localPosition, targetHeadPos, Time.deltaTime * crouchLerpSpeed);
        torsoTransform.localPosition = Vector3.Lerp(torsoTransform.localPosition, targetTorsoPos, Time.deltaTime * crouchLerpSpeed);
    }

    private void SmoothCameraHeight(float targetPlayerHeight)
    {
        Vector3 camPos = playerCamera.transform.localPosition;
        float targetCamY = targetPlayerHeight;

        camPos.y = Mathf.Lerp(camPos.y, targetCamY, Time.deltaTime * crouchLerpSpeed);
        playerCamera.transform.localPosition = camPos;
    }

    public float GetHorizontalMovementSpeed()
    {
        Vector3 horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0f;
        return horizontalVelocity.magnitude;
    }

    public float GetSignedMovementSpeedFromController()
    {
        if (Time.deltaTime == 0) return 0f;
        Vector3 delta = transform.position - lastPosForSignedSpeed;
        float speed = Vector3.Dot(delta / Time.deltaTime, transform.forward);
        lastPosForSignedSpeed = transform.position;
        return speed;
    }
    public float GetMovementSpeed()
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
                if (timeToNextFootstep < 0) timeToNextFootstep = effectiveFootstepInterval * 0.1f;
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