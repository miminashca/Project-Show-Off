using System;
using UnityEngine;
using UnityEngine.Serialization;

public class CameraMovement : MonoBehaviour
{
    [SerializeField, Range(0, 90)] private int verticalLookClamp = 45;
    [SerializeField, Range(0f, 1f)] private float mouseSensitivity = 0.5f;
    
    private PlayerInput controls;
    private Vector2 mouseLook;
    private float xRotation = 0f;
    private Transform playerBody;

    void Awake()
    {
        playerBody = transform.parent;
        controls = new PlayerInput();
        Cursor.lockState = CursorLockMode.Locked;
    }
    private void OnEnable()
    {
        controls.Enable();
    }

    void Update()
    {
        Look();
    }

    private void Look()
    {
        mouseLook =  (mouseSensitivity / Screen.dpi * 100f) * controls.Movement.Look.ReadValue<Vector2>();
        xRotation = Mathf.Clamp(xRotation-mouseLook.y, -verticalLookClamp, verticalLookClamp);
        
        transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        playerBody.Rotate(Vector3.up * mouseLook.x);
        
    }
    private void OnDisable()
    {
        controls.Disable();
    }
}
