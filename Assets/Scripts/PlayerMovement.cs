using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField, Range(0f, 10f)] private float moveSpeed = 10f;
    [SerializeField, Range(0f, 100f)] private float sprintSpeedPlus = 3f;
    
    [SerializeField] private LayerMask groundMask;
    private float finalSpeed;
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
        Vector3 movement = (move.y * transform. forward) + (move.x * transform.right);
        
        if (controls.Movement.Sprint.inProgress)
        {
            if(finalSpeed != moveSpeed + sprintSpeedPlus) finalSpeed = Mathf.Lerp(finalSpeed, moveSpeed + sprintSpeedPlus, Time.fixedDeltaTime * 2f);
        }

        if (!controls.Movement.Sprint.inProgress)
        {
            if(finalSpeed != moveSpeed) finalSpeed = Mathf.Lerp(finalSpeed, moveSpeed, Time.fixedDeltaTime * 2f);
        }
        
        controller.Move ( finalSpeed * Time.fixedDeltaTime * movement);
    }
    
    private void OnDisable()
    {
        controls.Disable();
    }
}
