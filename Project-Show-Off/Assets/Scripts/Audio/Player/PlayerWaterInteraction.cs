// PlayerWaterInteraction.cs
using UnityEngine;
using FMODUnity;
using FMOD.Studio; // Required for EventInstance and STOP_MODE

public class PlayerWaterInteraction : MonoBehaviour
{
    [Header("Detection Settings")]
    public Transform headTransform;
    public LayerMask waterSurfaceLayer;
    [Tooltip("How far above the headTransform's origin the water level needs to be for full submersion check.")]
    public float submergePointVerticalOffset = 0.2f;

    [Header("FMOD Events - Water")]
    public EventReference underwaterAmbienceEvent;
    public EventReference submergeSoundEvent;
    public EventReference emergeSoundEvent;

    private bool isUnderwater = false;
    private bool wasUnderwaterLastFrame = false;
    private EventInstance underwaterAmbienceInstance;

    [Header("FMOD Events - Nixie Lure")]
    [Tooltip("The 3D FMOD event for the child laughter sound.")]
    public EventReference childLaughterEvent; // Ensure this is a 3D event in FMOD
    [Tooltip("The layer your Nixie GameObjects are on.")]
    public LayerMask nixieLayer;
    [Tooltip("How close the player needs to be to a Nixie (while underwater) to hear the laughter.")]
    public float nixieProximityDistance = 30f;

    private EventInstance nixieLaughterInstance;
    private bool isLaughterPlaying = false;
    private Transform currentLuringNixie = null; // --- NEW: To track which Nixie is emitting sound ---

    void Start()
    {
        if (headTransform == null)
        {
            if (Camera.main != null) headTransform = Camera.main.transform;
            else
            {
                Debug.LogError("PlayerWaterInteraction: Head Transform not assigned and Main Camera not found! Disabling script.", this);
                enabled = false;
                return;
            }
        }

        // Initialize water sounds
        if (!underwaterAmbienceEvent.IsNull)
        {
            underwaterAmbienceInstance = RuntimeManager.CreateInstance(underwaterAmbienceEvent);
            RuntimeManager.AttachInstanceToGameObject(underwaterAmbienceInstance, headTransform.gameObject);
        }
        else Debug.LogWarning("PlayerWaterInteraction: 'Underwater Ambience Event' is not assigned!", this);

        if (submergeSoundEvent.IsNull) Debug.LogWarning("PlayerWaterInteraction: 'Submerge Sound Event' is not assigned!", this);
        if (emergeSoundEvent.IsNull) Debug.LogWarning("PlayerWaterInteraction: 'Emerge Sound Event' is not assigned!", this);

        // Initialize Nixie Laughter Sound
        if (!childLaughterEvent.IsNull)
        {
            nixieLaughterInstance = RuntimeManager.CreateInstance(childLaughterEvent);
            // We will attach it to a specific Nixie later, not the player's head.
            //Debug.Log($"PlayerWaterInteraction: Nixie Laughter Event '{childLaughterEvent.Path}' assigned. Instance IsValid: {nixieLaughterInstance.isValid()}", this);
        }
        else Debug.LogWarning("PlayerWaterInteraction: 'Child Laughter Event' is not assigned! Nixie lure will not function.", this);
    }

    void Update()
    {
        if (headTransform == null) return;

        wasUnderwaterLastFrame = isUnderwater;
        CheckIfUnderwater();

        if (isUnderwater && !wasUnderwaterLastFrame)
        {
            OnEnterWater();
        }
        else if (!isUnderwater && wasUnderwaterLastFrame)
        {
            OnExitWater();
        }

        HandleNixieLaughterLogic();
    }

    void CheckIfUnderwater()
    {
        Vector3 checkHalfExtents = new Vector3(0.01f, 0.01f, 0.01f);
        Vector3 checkPosition = headTransform.position + (headTransform.up * submergePointVerticalOffset);
        isUnderwater = Physics.CheckBox(checkPosition, checkHalfExtents, Quaternion.identity, waterSurfaceLayer, QueryTriggerInteraction.Collide);
    }

    void OnEnterWater()
    {
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
        if (!emergeSoundEvent.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(emergeSoundEvent, headTransform.gameObject);
        }
        if (underwaterAmbienceInstance.isValid())
        {
            underwaterAmbienceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
    }

    void HandleNixieLaughterLogic()
    {
        if (headTransform == null || childLaughterEvent.IsNull || !nixieLaughterInstance.isValid())
        {
            if (isLaughterPlaying && nixieLaughterInstance.isValid()) // Stop if it was playing and something went wrong
            {
                nixieLaughterInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                isLaughterPlaying = false;
                currentLuringNixie = null;
            }
            return;
        }

        GameObject closestNixieObject = null;
        float minDistanceSqr = nixieProximityDistance * nixieProximityDistance; // Only consider Nixies within this squared distance

        if (isUnderwater)
        {
            Collider[] nixieColliders = Physics.OverlapSphere(headTransform.position, nixieProximityDistance, nixieLayer, QueryTriggerInteraction.Collide);
            float currentClosestDistSqr = minDistanceSqr + 1f; // Initialize higher than max allowed

            foreach (Collider nixieCollider in nixieColliders)
            {
                // Ensure it's a valid Nixie GameObject (e.g., has the expected component or just use the collider's GameObject)
                // For simplicity, we'll just use nixieCollider.gameObject
                float distSqr = (headTransform.position - nixieCollider.transform.position).sqrMagnitude;
                if (distSqr < currentClosestDistSqr)
                {
                    currentClosestDistSqr = distSqr;
                    closestNixieObject = nixieCollider.gameObject;
                }
            }
        }
        // If not underwater, or if underwater but no Nixies found in range, closestNixieObject will remain null.

        bool conditionsMetForLaughter = isUnderwater && (closestNixieObject != null);

        if (conditionsMetForLaughter)
        {
            if (!isLaughterPlaying)
            {
                currentLuringNixie = closestNixieObject.transform;
                RuntimeManager.AttachInstanceToGameObject(nixieLaughterInstance, currentLuringNixie.gameObject, (Rigidbody)null); // Attach to the Nixie
                nixieLaughterInstance.start();
                isLaughterPlaying = true;
                // Debug.Log($"Nixie laughter started from: {currentLuringNixie.name}");
            }
            else if (isLaughterPlaying && closestNixieObject.transform != currentLuringNixie)
            {
                // The sound is already playing, but the closest Nixie has changed
                currentLuringNixie = closestNixieObject.transform;
                RuntimeManager.AttachInstanceToGameObject(nixieLaughterInstance, currentLuringNixie.gameObject, (Rigidbody)null); // Re-attach to the new Nixie
                // Debug.Log($"Nixie laughter re-attached to: {currentLuringNixie.name}");
                // No need to call start() again if it's already playing and continuous.
                // If the sound needs to "re-trigger" from the new source, you might stop and start it.
            }
        }
        else // Conditions are NOT met
        {
            if (isLaughterPlaying)
            {
                nixieLaughterInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                isLaughterPlaying = false;
                currentLuringNixie = null; // Clear the reference
                // Debug.Log("Nixie laughter stopped: Conditions no longer met.");
            }
        }
    }

    void OnDestroy()
    {
        if (underwaterAmbienceInstance.isValid())
        {
            underwaterAmbienceInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            underwaterAmbienceInstance.release();
        }

        if (nixieLaughterInstance.isValid())
        {
            nixieLaughterInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            nixieLaughterInstance.release();
        }
    }
}