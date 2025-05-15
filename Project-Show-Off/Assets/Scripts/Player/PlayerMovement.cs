using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Move Settings")]
    [SerializeField, Range(0f, 20f)] private float moveSpeed = 10f;
    [SerializeField, Range(1f, 10f)] private float directionLerpSpeed = 5f;
    [SerializeField] private LayerMask groundMask;
    
    [Header("Sprint Settings")]
    [SerializeField, Range(0f, 20f)] private float sprintSpeedIncrement = 3f;
    [SerializeField, Range(1f, 10f)] private float moveToSprintLerpSpeed = 2f;
    
    [Header("Crouch Settings")]
    [SerializeField, Range(1f, 10f)] private float crouchSpeed = 4f;
    [SerializeField, Range(1f, 2f)] private float crouchHeight = 1f;
    [SerializeField, Range(1f, 4f)] private float standingHeight = 2f;
    [SerializeField, Range(1f, 10f)] private float crouchLerpSpeed = 8f;
    
    //const
    private float gravity = -9.81f;
    private float groundCheckDistance = 0.4f;
    private float headCheckDistance = 0.4f;
    
    //intermediate
    [NonSerialized] public bool isCrouching = false;
    [NonSerialized] public bool isSprinting = false;
    [NonSerialized] public bool isGrounded = true;
    private Vector2 move;
    private Vector3 velocity;
    private float finalSpeed;
    private Vector3 currentDirection = Vector3.zero;
    Vector3 lastPos;

    //references
    private CharacterController controller;
    private PlayerInput controls;
    private Camera playerCamera;

    private void Awake()
    {
        lastPos = transform.position;
        headCheckDistance = standingHeight - crouchHeight;
        finalSpeed = moveSpeed;
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        controller.height = standingHeight;
        playerCamera.transform.position = new Vector3(playerCamera.transform.position.x, standingHeight, playerCamera.transform.position.z);
    }
    private void OnEnable()
    {
        controls = new PlayerInput();
        controls.Enable();
    }
    
    private void Update()
    {
        Crouch();
    }
    private void FixedUpdate()
    {
       Gravity();
       Move();
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

    private void Move()
    {
        move = controls.Movement.Move.ReadValue<Vector2>();
        Vector3 inputDirection = (move.y * transform.forward) + (move.x * transform.right);
        bool isMoving = move.magnitude > 0.01f;
        
        // Smoothly update currentDirection towards input or zero
        if (isMoving)
        {
            currentDirection = Vector3.Lerp(currentDirection, inputDirection.normalized, Time.fixedDeltaTime * directionLerpSpeed);
        }
        else
        {
            currentDirection = Vector3.Lerp(currentDirection, Vector3.zero, Time.fixedDeltaTime * moveToSprintLerpSpeed); 
        }

        // Sprinting speed logic
        float targetSpeed = isCrouching ? crouchSpeed : moveSpeed;
        float signedSpeed = GetSignedMovementSpeed();
        if (signedSpeed < -0.1f) targetSpeed *= 0.8f;
        else if (controls.Movement.Sprint.inProgress && isMoving && !isCrouching && signedSpeed > 0.1f)
        {
            isSprinting = true;
            targetSpeed += sprintSpeedIncrement;
        }
        else if (!isMoving)
        {
            targetSpeed = 0f;
        }

        if (!controls.Movement.Sprint.inProgress) isSprinting = false;

        finalSpeed = Mathf.Lerp(finalSpeed, targetSpeed, Time.fixedDeltaTime * moveToSprintLerpSpeed); // smooth speed transition

        Vector3 finalMove = finalSpeed * Time.fixedDeltaTime * currentDirection;
        controller.Move(finalMove);
    }

    private void Crouch()
    {
        if (controls.Movement.Crouch.triggered)
        {
            if(isCrouching && CheckHeadBump()) return; //dont uncrouch if head bumps
            
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
        v.y = 0f;
        //Debug.Log(v.magnitude);
        return v.magnitude;
    }
    /// <summary>
    /// Returns positive if moving forward, negative if moving backward,
    /// zero if stationary or purely strafing.
    /// Magnitude of the return value is the horizontal speed.
    /// </summary>
    public float GetSignedMovementSpeed()
    {
        Vector3 delta = transform.position - lastPos;
        float signedSpeed = Vector3.Dot(delta / Time.deltaTime, transform.forward);
        //Debug.Log("Signed speed: " + signedSpeed);
        lastPos = transform.position;
        return signedSpeed;
    }
    
    private void OnDisable()
    {
        controls.Disable();
    }
}
