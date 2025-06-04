// PlayerWaterInteraction.cs (Modified for Physics.CheckBox)
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class PlayerWaterInteraction : MonoBehaviour
{
    [Header("Detection Settings")]
    public Transform headTransform;
    public LayerMask waterSurfaceLayer;
    // public float raycastDistance = 1.0f; // No longer directly used by CheckBox for "am I inside"

    [Header("FMOD Events")]
    public EventReference underwaterAmbienceEvent;
    public EventReference submergeSoundEvent;
    public EventReference emergeSoundEvent;

    private bool isUnderwater = false;
    private bool wasUnderwaterLastFrame = false;
    private EventInstance underwaterAmbienceInstance;

    void Start()
    {
        if (headTransform == null)
        {
            if (Camera.main != null) headTransform = Camera.main.transform;
            else { Debug.LogError("PlayerWaterInteraction: Head Transform not found!"); enabled = false; return; }
        }

        if (!underwaterAmbienceEvent.IsNull)
        {
            underwaterAmbienceInstance = RuntimeManager.CreateInstance(underwaterAmbienceEvent);
            RuntimeManager.AttachInstanceToGameObject(underwaterAmbienceInstance, headTransform.gameObject);
        }
        else Debug.LogError("PlayerWaterInteraction: 'Underwater Ambience Event' is not assigned!");

        if (submergeSoundEvent.IsNull) Debug.LogError("PlayerWaterInteraction: 'Submerge Sound Event' is not assigned!");
        if (emergeSoundEvent.IsNull) Debug.LogError("PlayerWaterInteraction: 'Emerge Sound Event' is not assigned!");
    }

    void Update()
    {
        if (headTransform == null) return;

        wasUnderwaterLastFrame = isUnderwater;
        CheckIfUnderwater(); // Now uses Physics.CheckBox

        if (isUnderwater && !wasUnderwaterLastFrame)
        {
            OnEnterWater();
        }
        else if (!isUnderwater && wasUnderwaterLastFrame)
        {
            OnExitWater();
        }
    }

    void CheckIfUnderwater()
    {
        // Define the size of the small box/point to check.
        Vector3 checkHalfExtents = new Vector3(0.01f, 0.01f, 0.01f);

        // --- NEW CHANGE: Offset the check position ---
        // You need to decide how high above the 'headTransform.position' (camera's position)
        // the water level needs to be for the player to be considered "fully submerged".
        // This 'submergePointOffset' is in local space relative to the headTransform's UP direction.
        // Adjust this value based on your player model's head height or desired submersion point.
        // For example, 0.1f to 0.3f might be a good starting range if headTransform is at eye level.
        float submergePointVerticalOffset = 0.6f; // Example: 20cm above the camera's origin

        // Calculate the actual world-space position for the CheckBox
        // We take the headTransform's position and add an offset along its local UP vector.
        // Using headTransform.up ensures the offset is always "above the head" regardless of player orientation.
        Vector3 checkPosition = headTransform.position + (headTransform.up * submergePointVerticalOffset);
        // --- END CHANGE ---

        isUnderwater = Physics.CheckBox(checkPosition, checkHalfExtents, Quaternion.identity, waterSurfaceLayer, QueryTriggerInteraction.Collide);

#if UNITY_EDITOR
        // Visualize the actual checkPosition
        Color debugColor = isUnderwater ? Color.blue : Color.yellow;
        Debug.DrawRay(checkPosition, Vector3.up * 0.1f, debugColor, 0.1f); // Draw a small ray at the checkPosition
        if (isUnderwater)
        {
            // Debug.Log($"PlayerWaterInteraction: CheckBox at {checkPosition} - IS UNDERWATER.");
        }
        else
        {
            // Debug.Log($"PlayerWaterInteraction: CheckBox at {checkPosition} - NOT underwater.");
        }
#endif
    }

    void OnEnterWater()
    {
        Debug.Log("Player SUBMERGED (CheckBox Detection)");
        if (!submergeSoundEvent.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(submergeSoundEvent, headTransform.gameObject);
        }
        if (underwaterAmbienceInstance.isValid())
        {
            underwaterAmbienceInstance.start();
        }
    }

    void OnExitWater()
    {
        Debug.Log("Player EMERGED (CheckBox Detection)");
        if (!emergeSoundEvent.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(emergeSoundEvent, headTransform.gameObject);
        }
        if (underwaterAmbienceInstance.isValid())
        {
            underwaterAmbienceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
    }

    void OnDestroy()
    {
        if (underwaterAmbienceInstance.isValid())
        {
            underwaterAmbienceInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            underwaterAmbienceInstance.release();
        }
    }
}