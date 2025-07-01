using System;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField, Range(0, 90)] private int verticalLookClamp = 45;
    [SerializeField, Range(0f, 1f)] public float mouseSensitivity = 0.5f;
    [SerializeField, Range(1f, 20f)] private float lookLerpSpeed = 10f;

    public bool IsGazePullActive { get; set; } = false;
    
    private Vector3 _gazePullDirection;
    private float _gazePullLerpFactor;

    private float targetYaw, targetPitch;
    private float smoothYaw, smoothPitch;
    
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

    void LateUpdate()
    {
        // This is now a single, unified function.
        ProcessAndApplyLook();
    }

    private void ProcessAndApplyLook()
    {
        // 1. Read raw player input
        Vector2 raw = controls.Player.Look.ReadValue<Vector2>();
        float scaledX = raw.x * (mouseSensitivity / Screen.dpi * 100f);
        float scaledY = raw.y * (mouseSensitivity / Screen.dpi * 100f);

        // 2. Apply player's input to the RAW target angles
        targetYaw += scaledX;
        targetPitch -= scaledY;

        // 3. --- THE CRITICAL FIX IS HERE ---
        // If the pull is active, we LERP the RAW target angles, NOT the smoothed ones.
        if (IsGazePullActive)
        {
            Quaternion targetLookRotation = Quaternion.LookRotation(_gazePullDirection);
            float gazeTargetYaw = targetLookRotation.eulerAngles.y;
            float gazeTargetPitch = targetLookRotation.eulerAngles.x;
            if (gazeTargetPitch > 180) gazeTargetPitch -= 360;

            // Blend the player's raw target with the lady's target
            targetYaw = Mathf.LerpAngle(targetYaw, gazeTargetYaw, _gazePullLerpFactor);
            targetPitch = Mathf.LerpAngle(targetPitch, gazeTargetPitch, _gazePullLerpFactor);
        }

        // 4. Clamp the final target pitch
        targetPitch = Mathf.Clamp(targetPitch, -verticalLookClamp, verticalLookClamp);

        // 5. NOW, we apply the final smoothing to the (potentially modified) target angles.
        smoothYaw = Mathf.LerpAngle(smoothYaw, targetYaw, Time.deltaTime * lookLerpSpeed);
        smoothPitch = Mathf.LerpAngle(smoothPitch, targetPitch, Time.deltaTime * lookLerpSpeed);

        // 6. Apply the final smoothed rotation to the transforms
        transform.localRotation = Quaternion.Euler(smoothPitch, 0f, 0f);
        playerBody.rotation = Quaternion.Euler(0f, smoothYaw, 0f);
    }

    /// <summary>
    /// Public method for the FeedbackController to provide pull data.
    /// </summary>
    public void UpdateGazeData(Vector3 direction, float lerpFactor)
    {
        _gazePullDirection = direction;
        _gazePullLerpFactor = lerpFactor;
    }
    
    private void OnDisable()
    {
        controls.Disable();
    }
}