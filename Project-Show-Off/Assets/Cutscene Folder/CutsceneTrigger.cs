using UnityEngine;
using UnityEngine.Playables; // Required for accessing the PlayableDirector
using System.Collections;
using UnityEngine.Events;   // Required for Coroutines

/// <summary>
/// This script triggers a cutscene (controlled by a PlayableDirector)
/// when a GameObject with a specific tag enters a trigger collider.
/// It creates a seamless transition by smoothly animating the player camera 
/// to the position and rotation of the first shot of the cutscene.
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
    [Tooltip("The main camera used during gameplay (often a child of the player).")]
    public Camera playerCamera;

    [Tooltip("The camera that the timeline will use for the cutscene. The transition will move the player camera to match this camera's initial state.")]
    public Camera cutsceneCamera;

    [Header("Transition Settings")]
    [Tooltip("How long the blend from player camera to cutscene camera should take.")]
    public float transitionDuration = 1.5f;

    // NEW: An AnimationCurve provides much more control over the transition's feel.
    [Tooltip("The curve that defines the blend's speed over time. A nice S-curve (ease-in-out) is recommended.")]
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Player Control")]
    [Tooltip("(Optional) The player's movement script to disable during the cutscene.")]
    public MonoBehaviour playerController;


    // --- Private Variables ---
    private bool hasTriggered = false; // Ensures the cutscene only plays once.
    private Coroutine endCutsceneCoroutine; // NEW: To handle the transition back

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
        hasTriggered = true;

        // --- Start the Transition ---
        StartCoroutine(StefanBullshit());
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

        // The cutscene camera should be disabled at the start of the transition
        cutsceneCamera.gameObject.SetActive(false);

        float elapsedTime = 0f;
        Vector3 startPosition = playerCamera.transform.position;
        Quaternion startRotation = playerCamera.transform.rotation;

        // The target is the transform of the cutscene camera in the scene
        Transform targetTransform = cutsceneCamera.transform;

        // Loop over the duration of the transition
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            // CHANGED: Use the AnimationCurve to evaluate the interpolation factor.
            float t = transitionCurve.Evaluate(elapsedTime / transitionDuration);

            // Interpolate position and rotation of the PLAYER camera
            playerCamera.transform.position = Vector3.LerpUnclamped(startPosition, targetTransform.position, t);
            playerCamera.transform.rotation = Quaternion.SlerpUnclamped(startRotation, targetTransform.rotation, t);

            yield return null; // Wait for the next frame
        }

        // --- Finalize Transition ---
        // Ensure the player camera is exactly at the target transform
        playerCamera.transform.position = targetTransform.position;
        playerCamera.transform.rotation = targetTransform.rotation;

        // --- Hand Over to the Cutscene ---
        // Now, switch the active cameras. The player camera is already in the perfect spot.
        cutsceneCamera.gameObject.SetActive(true);
        playerCamera.gameObject.SetActive(false);

        Debug.Log("Camera blend complete. Playing cutscene.");
        cutsceneDirector.Play();
    }
    IEnumerator StefanBullshit()
    {
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        cutsceneCamera.transform.position = playerCamera.transform.position;
        cutsceneCamera.gameObject.SetActive(true);
        playerCamera.gameObject.SetActive(false);
        cutsceneDirector.Play();

        //this doesn't need to be a coroutine so I just break at the end
        yield break;
    }

    /// <summary>
    /// This public method should be called at the end of the cutscene via a Signal Emitter.
    /// It transitions smoothly back to the player's perspective and re-enables control.
    /// </summary>
    public void EndCutscene()
    {
        // NEW: We now start a coroutine for a smooth transition back to the player
        if (endCutsceneCoroutine == null)
        {
            endCutsceneCoroutine = StartCoroutine(EndCutsceneTransition());
        }
    }

    /// <summary>
    /// NEW: Coroutine to handle the transition back to the player camera.
    /// This provides a smooth return to gameplay, avoiding a jarring cut.
    /// </summary>
    private IEnumerator EndCutsceneTransition()
    {
        Debug.Log("Cutscene finished. Transitioning back to player camera.");

        // First, re-activate the player camera and deactivate the cutscene camera.
        // At this exact frame, both cameras are in the same spot, so there is no visual pop.
        playerCamera.gameObject.SetActive(true);
        cutsceneCamera.gameObject.SetActive(false);

        // We can re-use the same transition settings for the return trip.
        float elapsedTime = 0f;

        // The starting point is now the cutscene's end position
        Vector3 startPosition = playerCamera.transform.position;
        Quaternion startRotation = playerCamera.transform.rotation;

        // The target is a reference to the player object's transform, which may have moved
        // if the player character was animated during the cutscene.
        // We will assume the player camera needs to return to a default state relative to the player.
        // For simplicity, we'll let the player's camera controller script handle repositioning.
        // If your camera doesn't auto-correct, you would Lerp it back to a target position here.

        // Re-enable player controls first so the camera can snap back to its gameplay position.
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        // This coroutine currently just handles the state switching. 
        // You could add another Lerp loop here if you wanted to animate the camera back
        // to a specific offset from the player, but often just re-enabling the player
        // controller and its camera script is enough.

        yield return null; // Wait a frame to ensure scripts are enabled.

        Debug.Log("Player control has been restored.");
        endCutsceneCoroutine = null;
    }


    /// <summary>
    /// This is a Unity Editor-only method to draw a visual aid in the Scene view.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (cutsceneDirector != null)
        {
            Gizmos.color = new Color(0f, 0.7f, 0.9f, 0.5f); // Cyan
            Gizmos.DrawLine(transform.position, cutsceneDirector.transform.position);
        }
        if (cutsceneCamera != null && playerCamera != null)
        {
            Gizmos.color = new Color(0.9f, 0.7f, 0f, 0.5f); // Yellow
            Gizmos.DrawLine(playerCamera.transform.position, cutsceneCamera.transform.position);
        }
    }
}