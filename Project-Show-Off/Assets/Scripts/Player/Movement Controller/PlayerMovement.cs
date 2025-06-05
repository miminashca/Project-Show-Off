// PlayerMovement.cs
// NEW CHANGE
using FMODUnity;
using FMOD.Studio;
// END CHANGE
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

    // NEW CHANGE
    [Header("FMOD Sprinting Sounds")]
    [Tooltip("FMOD Event for continuous breathing while sprinting. Should have a loop region.")]
    [SerializeField] private EventReference sprintingBreathEventPath;
    [Tooltip("FMOD Event for one-shot breath sound after sprinting stops.")]
    [SerializeField] private EventReference afterSprintingBreathEventPath;

    private EventInstance sprintingBreathInstance;
    private EventInstance afterSprintingBreathInstance; // Instance for the one-shot after-sprint breath
    private bool previousIsSprintingState = false;
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
        HandleStamina();

        // NEW CHANGE
        HandleSprintingAudio();
        // END CHANGE

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

        // Optional: Check if it's playing before stopping, though stopping a stopped instance is often safe.
        // PLAYBACK_STATE playbackState;
        // eventInstance.getPlaybackState(out playbackState);
        // if (playbackState == PLAYBACK_STATE.PLAYING || playbackState == PLAYBACK_STATE.STARTING)
        // {
        eventInstance.stop(stopMode);
        // }
    }

    private void HandleSprintingAudio()
    {
        bool justStartedSprinting = isSprinting && !previousIsSprintingState;
        bool justStoppedSprinting = !isSprinting && previousIsSprintingState;

        if (justStartedSprinting)
        {
            // Player started sprinting
            StartEventInstanceFMOD(sprintingBreathInstance);

            // Stop the "after sprinting" breath if it was playing
            StopEventInstanceFMOD(afterSprintingBreathInstance, FMOD.Studio.STOP_MODE.ALLOWFADEOUT); // Or IMMEDIATE if a hard cut is preferred
        }
        else if (justStoppedSprinting)
        {
            // Player stopped sprinting
            StopEventInstanceFMOD(sprintingBreathInstance, FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

            // Play the one-shot "after sprinting" breath
            // Ensure the "afterSprintingBreathEventPath" FMOD event is a one-shot (no loop region)
            StartEventInstanceFMOD(afterSprintingBreathInstance);
        }

        previousIsSprintingState = isSprinting;
    }
    // END CHANGE

    private void HandleStamina()
    {
        bool sprintInputActive = controls.Player.Sprint.inProgress;
        bool canPotentiallySprint = sprintInputActive && isMoving && !isCrouching;

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
            }
        }
        else
        {
            if (canPotentiallySprint && currentStamina > minStaminaToSprint)
            {
                isSprinting = true;
                currentStamina -= staminaDrainRate * Time.deltaTime;
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