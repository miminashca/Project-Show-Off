using UnityEngine;
public class CameraMovement : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField, Range(0, 90)] private int verticalLookClamp = 45;
    [SerializeField, Range(0f, 1f)] private float mouseSensitivity = 0.5f;
    [SerializeField, Range(1f, 20f)] private float lookLerpSpeed = 10f;

    private float targetYaw;
    private float targetPitch;
    private float smoothYaw;
    private float smoothPitch;

    //references
    private PlayerInput controls;
    private Transform playerBody;

    //intermediate
    private Vector2 mouseLook;

    void Awake()
    {
        playerBody = transform.parent;
        controls = new PlayerInput(); // Assuming PlayerInput is set up correctly
        Cursor.lockState = CursorLockMode.Locked;

        targetYaw = smoothYaw = playerBody.eulerAngles.y;
        // Ensure pitch initialization handles negative angles correctly from Euler
        float initialPitch = transform.localEulerAngles.x;
        if (initialPitch > 180) initialPitch -= 360;
        targetPitch = smoothPitch = initialPitch;
    }
    private void OnEnable()
    {
        controls.Enable();
        // It's good practice to re-sync yaw/pitch on enable if the object could have been rotated while disabled
        targetYaw = smoothYaw = playerBody.eulerAngles.y;
        float initialPitch = transform.localEulerAngles.x;
        if (initialPitch > 180) initialPitch -= 360;
        targetPitch = smoothPitch = initialPitch;
    }

    void Update()
    {
        Look();
    }

    private void Look()
    {
        // read raw input
        Vector2 raw = controls.Movement.Look.ReadValue<Vector2>();
        float scaledX = raw.x * (mouseSensitivity / Screen.dpi * 100f);
        float scaledY = raw.y * (mouseSensitivity / Screen.dpi * 100f);

        // update target angles
        targetYaw += scaledX;
        targetPitch -= scaledY;
        targetPitch = Mathf.Clamp(targetPitch, -verticalLookClamp, verticalLookClamp);

        // smooth actual angles toward target
        smoothYaw = Mathf.LerpAngle(smoothYaw, targetYaw, Time.deltaTime * lookLerpSpeed);
        smoothPitch = Mathf.LerpAngle(smoothPitch, targetPitch, Time.deltaTime * lookLerpSpeed);

        // apply
        transform.localRotation = Quaternion.Euler(smoothPitch, 0f, 0f);
        playerBody.rotation = Quaternion.Euler(0f, smoothYaw, 0f);
    }

    private void OnDisable()
    {
        controls.Disable();
    }
}
