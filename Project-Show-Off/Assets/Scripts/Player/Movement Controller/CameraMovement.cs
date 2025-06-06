using System;
using UnityEngine;
public class CameraMovement : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField, Range(0, 90)] private int verticalLookClamp = 45;
    [SerializeField, Range(0f, 1f)] public float mouseSensitivity = 0.5f;
    [SerializeField, Range(1f, 20f)] private float lookLerpSpeed = 10f;

    private float targetYaw;
    private float targetPitch;
    private float smoothYaw;
    private float smoothPitch;
    
    //references
    private PlayerInput controls;
    private Transform playerBody;

    void Awake()
    {
        playerBody = transform.parent;
        Cursor.lockState = CursorLockMode.Locked;
        
        targetYaw = smoothYaw = playerBody.eulerAngles.y;
        targetPitch = smoothPitch = transform.localEulerAngles.x;
    }
    private void OnEnable()
    {
        controls = new PlayerInput();
        controls.Enable();
    }

    void Update()
    {
        ReadValue();
        Look();
    }

    private void ReadValue()
    {
        // read raw input
        Vector2 raw = controls.Player.Look.ReadValue<Vector2>();
        float scaledX = raw.x * (mouseSensitivity / Screen.dpi * 100f);
        float scaledY = raw.y * (mouseSensitivity / Screen.dpi * 100f);

        // update target angles
        targetYaw += scaledX;
        targetPitch -= scaledY;
        targetPitch = Mathf.Clamp(targetPitch, -verticalLookClamp, verticalLookClamp);

        
        // smooth actual angles toward target
        smoothYaw = Mathf.LerpAngle(smoothYaw,   targetYaw,   Time.deltaTime * lookLerpSpeed);
        smoothPitch = Mathf.LerpAngle(smoothPitch, targetPitch, Time.deltaTime * lookLerpSpeed);
    }
    private void Look()
    {
        // apply
        transform.localRotation = Quaternion.Euler(smoothPitch, 0f, 0f);
        playerBody.rotation = Quaternion.Euler(0f, smoothYaw, 0f);
    }
    
    private void OnDisable()
    {
        controls.Disable();
    }
}