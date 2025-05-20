// HeadbobController.cs
using UnityEngine;

public class HeadbobController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private bool enable = true; // Toggle to enable/disable headbobbing
    [SerializeField, Range(0f, 0.1f)] private float amplitude = 0.015f; // Amplitude of headbobbing motion
    [SerializeField, Range(0f, 20f)] private float frequency = 10.0f; // Frequency of headbobbing motion
    [SerializeField, Range(0f, 20f)] private float bobLerpSpeed = 10f; // Speed to interpolate headbob effect

    //references
    private Transform playerBody;
    private Camera playerCamera;
    private PlayerMovement controller; // Reference to the PlayerMovement script

    //const
    private float toggleSpeed = 0.3f; // Speed threshold to trigger headbobbing
    private Vector3 startPos; // Starting local position of the playerCamera (captures initial X, Y, Z)
    private Vector3 currentBobOffset = Vector3.zero; // The current offset applied by headbob

    void Awake()
    {
        playerBody = transform.parent;
        if (playerBody == null)
        {
            Debug.LogError("HeadbobController: PlayerBody (parent) not found!", this);
            enable = false;
            return;
        }
        controller = playerBody.GetComponent<PlayerMovement>();
        if (controller == null)
        {
            Debug.LogError("HeadbobController: PlayerMovement script not found on PlayerBody!", this);
            enable = false;
            return;
        }
        playerCamera = GetComponent<Camera>();
        if (playerCamera == null)
        {
            Debug.LogError("HeadbobController: Camera component not found on this GameObject!", this);
            enable = false;
            return;
        }
    }

    private void Start()
    {
        if (!enable) return;
        startPos = playerCamera.transform.localPosition;
    }

    void LateUpdate()
    {
        if (!enable)
        {
            currentBobOffset = Vector3.Lerp(currentBobOffset, Vector3.zero, bobLerpSpeed * Time.deltaTime);
            ApplyBobOffset();
            return;
        }

        Vector3 targetBobOffset = Vector3.zero;
        float speed = controller.GetMovementSpeed();

        if (speed >= toggleSpeed && controller.isGrounded)
        {
            targetBobOffset = CalculateFootStepMotion();
        }

        currentBobOffset = Vector3.Lerp(currentBobOffset, targetBobOffset, bobLerpSpeed * Time.deltaTime);

        ApplyBobOffset();
    }

    // Calculate the headbobbing motion offset
    private Vector3 CalculateFootStepMotion()
    {
        float finalFrequency = frequency;
        float finalAmplitude = amplitude;

        if (controller.isCrouching)
        {
            finalFrequency *= 0.75f;
            finalAmplitude *= 0.75f;
        }
        else if (controller.isSprinting)
        {
            finalFrequency *= 1.5f;
            finalAmplitude *= 1.25f;
        }

        Vector3 bobOffset = Vector3.zero;
        bobOffset.y = Mathf.Sin(Time.time * finalFrequency) * finalAmplitude;
        bobOffset.x = Mathf.Cos(Time.time * finalFrequency / 2f) * finalAmplitude * 2f;

        return bobOffset;
    }

    private void ApplyBobOffset()
    {
        if (playerCamera == null) return;

        Vector3 currentBaseLocalPosition = playerCamera.transform.localPosition;

        playerCamera.transform.localPosition = new Vector3(
            startPos.x + currentBobOffset.x,
            currentBaseLocalPosition.y + currentBobOffset.y,
            startPos.z
        );
    }
}