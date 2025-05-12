using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField, Range(0f, 10f)] private float moveSpeed = 10f;
    [SerializeField, Range(1f, 3f)] private float moveToSprintSpeed = 2f;
    [SerializeField, Range(0f, 100f)] private float sprintSpeedPlus = 3f;
    
    [SerializeField] private LayerMask groundMask;
    private float finalSpeed;
    private Vector3 currentDirection = Vector3.zero;
    private CharacterController controller;
    private Vector3 velocity;
    private float gravity = -9.81f;
    private Vector2 move;
    private PlayerInput controls;
    private float groundCheckDistance = 0.4f;
    private bool isGrounded;

    private void Awake()
    {
        finalSpeed = moveSpeed;
        controls = new PlayerInput();
        controller = GetComponent<CharacterController>();
    }
    private void OnEnable()
    {
        controls.Enable();
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

    private void Move()
    {
        move = controls.Movement.Move.ReadValue<Vector2>();
        Vector3 inputDirection = (move.y * transform.forward) + (move.x * transform.right);
        bool isMoving = move.magnitude != 0f;

        // Smoothly update currentDirection towards input or zero
        if (isMoving)
        {
            currentDirection = inputDirection.normalized;
        }
        else
        {
            currentDirection = Vector3.Lerp(currentDirection, Vector3.zero, Time.fixedDeltaTime * moveToSprintSpeed); 
        }

        // Sprinting speed logic
        float targetSpeed = moveSpeed;
        if (controls.Movement.Sprint.inProgress && isMoving)
        {
            targetSpeed += sprintSpeedPlus;
        }
        else if (!isMoving)
        {
            targetSpeed = 0f;
        }

        finalSpeed = Mathf.Lerp(finalSpeed, targetSpeed, Time.fixedDeltaTime * moveToSprintSpeed); // smooth speed transition

        Vector3 finalMove = finalSpeed * Time.fixedDeltaTime * currentDirection;
        controller.Move(finalMove);
    }
    
    private void OnDisable()
    {
        controls.Disable();
    }
}
