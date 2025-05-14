using UnityEngine;

public class HeadbobController : MonoBehaviour
{
    [Header("Configuration")]
    
    [SerializeField] private bool enable = true; // Toggle to enable/disable headbobbing

    [SerializeField, Range(0f, 0.1f)] private float amplitude = 0.015f; // Amplitude of headbobbing motion
    [SerializeField, Range(0f, 20f)] private float frequency = 10.0f; // Frequency of headbobbing motion
    [SerializeField, Range(0f, 5f)] private float resetCamSpeed = 1f; // Frequency of headbobbing motion
    
    //references
    private Transform playerBody;
    private Camera camera;
    private PlayerMovement controller; // Reference to the PlayerMovement script

    //const
    private float toggleSpeed = 1.0f; // Speed threshold to trigger headbobbing
    private Vector3 startPos; // Starting position of the camera

    void Awake()
    {
        playerBody = transform.parent;
        controller = playerBody.GetComponent<PlayerMovement>();
        camera = GetComponent<Camera>();
    }
    private void Start()
    {
        startPos = transform.localPosition; // Store the initial local position of the camera
    }

    void Update()
    {
        if (!enable) return; // If headbobbing is disabled, exit Update
        
        CheckMotion(); // Check if the player's motion should trigger headbobbing
        ResetPosition(); // Reset the camera position smoothly back to the start position
    }

    // Calculate the headbobbing motion based on footstep-like movement
    private Vector3 FootStepMotion()
    {
        float finalFrequency = frequency;
        
        if (controller.isCrouching) finalFrequency *= 0.5f;
        else if(controller.isSprinting) finalFrequency *= 2f;
        
        Vector3 pos = Vector3.zero;
        pos.y += Mathf.Sin(Time.time * finalFrequency) * amplitude; // Vertical motion (bobbing up and down)
        pos.x += Mathf.Cos(Time.time * finalFrequency / 2) * amplitude * 2; // Horizontal motion (swaying side to side)
        
        return pos;
    }

    // Check if player's motion is sufficient to trigger headbobbing
    private void CheckMotion()
    {
        float speed = controller.GetMovementSpeed(); // Get player's movement speed directly
        if (speed < toggleSpeed) return; // If speed is below threshold, do not trigger headbobbing
        if (!controller.isGrounded) return; // If player is not grounded, do not trigger headbobbing

        PlayMotion(FootStepMotion()); // Trigger headbobbing based on footstep motion
    }
    
    // Apply the calculated motion to the camera, relative to player's orientation
    private void PlayMotion(Vector3 motion)
    {
        camera.transform.localPosition += motion; // Apply the motion to the camera's local position
    }
    
    
    // Smoothly reset the camera's position back to the starting position
    private void ResetPosition()
    {
        if (camera.transform.localPosition == startPos) return; // If camera is already at the start position, do nothing

        camera.transform.localPosition = Vector3.Lerp(camera.transform.localPosition, startPos, resetCamSpeed * Time.deltaTime); // Smoothly move camera towards the start position
    }
}