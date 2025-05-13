// using UnityEngine;
//
// public class HeadbobController : MonoBehaviour
// {
//     [Header("Configuration")]
//     [SerializeField] private bool enable = true; // Toggle to enable/disable headbobbing
//
//     [SerializeField, Range(0f, 0.1f)] private float amplitude = 0.015f; // Amplitude of headbobbing motion
//     [SerializeField, Range(0f, 20f)] private float frequency = 10.0f; // Frequency of headbobbing motion
//     [SerializeField, Range(0f, 5f)] private float resetCamSpeed = 1f; // Frequency of headbobbing motion
//     
//     //references
//     private Transform playerBody;
//     private Camera camera;
//
//     private float toggleSpeed = 2.0f; // Speed threshold to trigger headbobbing
//     private Vector3 startPos; // Starting position of the camera
//     private PlayerMovement controller; // Reference to the PlayerMovement script
//
//     void Awake()
//     {
//         playerBody = transform.parent;
//         camera = GetComponent<Camera>();
//     }
//     private void Start()
//     {
//         controller = playerBody.GetComponent<PlayerMovement>();
//         startPos = transform.localPosition; // Store the initial local position of the camera
//     }
//
//     void Update()
//     {
//         if (!enable) return; // If headbobbing is disabled, exit Update
//
//         CheckMotion(); // Check if the player's motion should trigger headbobbing
//         ResetPosition(); // Reset the camera position smoothly back to the start position
//         LookAt(FocusTarget()); // Make the camera look at a specific target point
//     }
//
//     // Calculate the headbobbing motion based on footstep-like movement
//     private Vector3 FootStepMotion()
//     {
//         Vector3 pos = Vector3.zero;
//         pos.y += Mathf.Sin(Time.time * frequency) * amplitude; // Vertical motion (bobbing up and down)
//         pos.x += Mathf.Cos(Time.time * frequency / 2) * amplitude * 2; // Horizontal motion (swaying side to side)
//         return pos;
//     }
//
//     // Check if player's motion is sufficient to trigger headbobbing
//     private void CheckMotion()
//     {
//         float speed = controller.GetMovementSpeed(); // Get player's movement speed directly
//         if (speed < toggleSpeed) return; // If speed is below threshold, do not trigger headbobbing
//         if (!controller.isGrounded) return; // If player is not grounded, do not trigger headbobbing
//
//         PlayMotion(FootStepMotion()); // Trigger headbobbing based on footstep motion
//     }
//
//
//     // Apply the calculated motion to the camera, relative to player's orientation
//     private void PlayMotion(Vector3 motion)
//     {
//         Vector3 localMotion = orientation.TransformDirection(motion); // Transform motion to be relative to player's orientation
//         camera.localPosition += localMotion; // Apply the motion to the camera's local position
//     }
//     
//     // Calculate the target position for the camera to look at
//     private Vector3 FocusTarget()
//     {
//         Vector3 pos = new Vector3(transform.position.x, transform.position.y + _cameraHolder.localPosition.y, transform.position.z); // Calculate position slightly above player's position
//         pos += _cameraHolder.forward * 7.0f; // Offset forward by a fixed distance
//         return pos; // Return the calculated target position
//     }
//
//     private void LookAt(Vector3 target)
//     {
//         transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles + target);
//     }
//     // Smoothly reset the camera's position back to the starting position
//     private void ResetPosition()
//     {
//         if (_camera.localPosition == _startPos) return; // If camera is already at the start position, do nothing
//
//         _camera.localPosition = Vector3.Lerp(_camera.localPosition, _startPos, _resetCamSpeed * Time.deltaTime); // Smoothly move camera towards the start position
//     }
// }