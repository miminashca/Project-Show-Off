using UnityEngine;
using UnityEngine.Playables; // Required for accessing the PlayableDirector
using System.Collections; // Required for Coroutines

/// <summary>
/// This script triggers a cutscene (controlled by a PlayableDirector)
/// when a GameObject with a specific tag enters a trigger collider.
/// It also handles switching between the player and cutscene cameras,
/// with a smooth blend from the player camera to the cutscene camera's starting position.
/// </summary>
public class CutsceneTrigger : MonoBehaviour
{
    // --- Public Variables ---
    [Header("Timeline Settings")]
    [Tooltip("The PlayableDirector component that controls the cutscene timeline.")]
    public PlayableDirector cutsceneDirector;

    [Tooltip("The tag of the object that can trigger the cutscene (e.g., 'Player').")]
    public string triggerTag = "Player";

    [Header("Camera Settings")]
    [Tooltip("The main camera attached to the player.")]
    public Camera playerCamera;

    [Tooltip("The camera that will be used for the cutscene.")]
    public Camera cutsceneCamera;

    [Tooltip("How long the blend from player camera to cutscene camera should take.")]
    public float transitionDuration = 0.5f;

    [Header("Player Control")]
    [Tooltip("(Optional) The player's movement script to disable during the cutscene.")]
    public MonoBehaviour playerController;


    // --- Private Variables ---
    private bool hasTriggered = false; // Ensures the cutscene only plays once.

    /// <summary>
    /// This method is called by Unity when another collider enters the trigger.
    /// </summary>
    /// <param name="other">The Collider of the object that entered the trigger zone.</param>
    private void OnTriggerEnter(Collider other)
    {
        // --- Pre-Trigger Checks ---
        if (hasTriggered || cutsceneDirector == null || playerCamera == null || cutsceneCamera == null || !other.CompareTag(triggerTag))
        {
            if (cutsceneDirector == null || playerCamera == null || cutsceneCamera == null)
            {
                Debug.LogWarning("Cutscene Trigger is missing a reference to the Director or one of the cameras.", this);
            }
            return; // Exit the method if any condition is not met.
        }

        // --- Mark as Triggered ---
        // We set this early to prevent the coroutine from being started multiple times.
        hasTriggered = true;

        // --- Start the Transition ---
        StartCoroutine(StartCutsceneTransition());
    }

    /// <summary>
    /// A coroutine to smoothly blend the player camera to the cutscene camera's transform
    /// before playing the timeline.
    /// </summary>
    private IEnumerator StartCutsceneTransition()
    {
        // Disable player controls if a controller script is assigned
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        float elapsedTime = 0f;
        Vector3 startPosition = playerCamera.transform.position;
        Quaternion startRotation = playerCamera.transform.rotation;

        // The target is the transform of the cutscene camera
        Vector3 targetPosition = cutsceneCamera.transform.position;
        Quaternion targetRotation = cutsceneCamera.transform.rotation;

        // Loop over the duration of the transition
        while (elapsedTime < transitionDuration)
        {
            // Calculate the current interpolation factor
            float t = elapsedTime / transitionDuration;
            // This creates a smooth ease-in and ease-out effect
            t = t * t * (3f - 2f * t);

            // Interpolate position and rotation
            playerCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            playerCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            // Wait for the next frame
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // --- Finalize Transition ---
        // Ensure the player camera is exactly at the target transform
        playerCamera.transform.position = targetPosition;
        playerCamera.transform.rotation = targetRotation;

        // Now, switch the active cameras
        playerCamera.gameObject.SetActive(false);
        cutsceneCamera.gameObject.SetActive(true);

        // --- Play the Cutscene ---
        Debug.Log("Camera blend complete. Playing cutscene.");
        cutsceneDirector.Play();
    }


    /// <summary>
    /// This public method should be called at the end of the cutscene via a Signal Emitter in the Timeline.
    /// It switches the cameras back to the player's perspective and re-enables control.
    /// </summary>
    public void EndCutscene()
    {
        Debug.Log("Cutscene finished. Switching back to player camera.");
        cutsceneCamera.gameObject.SetActive(false);
        playerCamera.gameObject.SetActive(true);

        // Re-enable player controls if a controller script is assigned
        if (playerController != null)
        {
            playerController.enabled = true;
        }
    }


    /// <summary>
    /// This is a Unity Editor-only method to draw a visual aid in the Scene view.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (cutsceneDirector != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, cutsceneDirector.transform.position);
        }
        if (cutsceneCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, cutsceneCamera.transform.position);
        }
    }
}
