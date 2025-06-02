using UnityEngine;
using FMODUnity; // Required for FMOD integration

[RequireComponent(typeof(Collider))] // Ensures a Collider is present
public class SoundTrigger : MonoBehaviour
{
    [Header("FMOD Event Settings")]
    [Tooltip("Drag your FMOD event here. This is the primary event that will be played.")]
    public EventReference fmodEvent; // This is now the primary way to select the event

    [Tooltip("Informational: Path of the selected FMOD event. Automatically updated if FMOD Event is set.")]
    public string fmodEventPath; // Kept for informational purposes or if you need the path string

    [Header("Trigger Settings")]
    [Tooltip("The tag of the GameObject that should trigger the sound (e.g., 'Player').")]
    public string triggerTag = "Player";

    [Tooltip("Should the sound only play once? If false, it will play every time the trigger is entered.")]
    public bool playOnce = true;

    [Tooltip("If true, the sound will play attached to this trigger object. If false, it will play at the trigger object's position but not be attached (useful for very short sounds).")]
    public bool attachToGameObject = true;

    [Header("Debugging")]
    [SerializeField] // Show private field in inspector for debugging
    private bool hasBeenTriggered = false;

    private Collider _collider;

    void Awake()
    {
        _collider = GetComponent<Collider>();

        // Ensure the collider is set to be a trigger
        if (_collider != null)
        {
            if (!_collider.isTrigger)
            {
                Debug.LogWarning($"Collider on {gameObject.name} was not set to 'Is Trigger'. SoundTrigger automatically set it to true.", this);
                _collider.isTrigger = true;
            }
        }
        else
        {
            Debug.LogError($"SoundTrigger on {gameObject.name} requires a Collider component, but none was found.", this);
            enabled = false; // Disable script if no collider
            return; // Early exit
        }

        // Validate the FMOD EventReference
        if (fmodEvent.IsNull) // EventReference.IsNull checks if the GUID is zero (i.e., not set)
        {
            Debug.LogError($"FMOD EventReference is not set or is invalid on SoundTrigger: {gameObject.name}. " +
                           "Please assign an FMOD Event to the 'Fmod Event' slot in the Inspector.", this);
            enabled = false; // Disable script if no valid event reference
            return;
        }

        // If fmodEvent is valid, try to update fmodEventPath for display/debug purposes.
        // This might not always resolve immediately in Awake if FMOD isn't fully initialized,
        // but it's good practice.
        if (!fmodEvent.IsNull)
        {
            FMOD.Studio.EventDescription eventDescription;
            if (RuntimeManager.StudioSystem.getEventByID(fmodEvent.Guid, out eventDescription) == FMOD.RESULT.OK)
            {
                eventDescription.getPath(out fmodEventPath);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Do nothing if the script is disabled (e.g., due to missing event in Awake)
        if (!enabled) return;

        // Check if the colliding object has the specified tag
        if (other.CompareTag(triggerTag))
        {
            // If playOnce is true, check if it has already been triggered
            if (playOnce && hasBeenTriggered)
            {
                return; // Sound has already played and should only play once
            }

            // Play the sound
            PlayFMODEvent();

            // Mark as triggered if playOnce is true
            if (playOnce)
            {
                hasBeenTriggered = true;
            }
        }
    }

    void PlayFMODEvent()
    {
        // The fmodEvent.IsNull check is primarily done in Awake,
        // but a redundant check here ensures safety if Awake was skipped or state changed.
        if (fmodEvent.IsNull)
        {
            Debug.LogWarning($"Attempted to play sound on {gameObject.name}, but FMOD EventReference is invalid. " +
                             "This should have been caught in Awake.", this);
            return;
        }

        // For logging, use the fmodEventPath if available, otherwise the GUID.
        string eventIdentifier = string.IsNullOrEmpty(fmodEventPath) ? $"GUID: {fmodEvent.Guid}" : $"'{fmodEventPath}'";

        if (attachToGameObject)
        {
            // Plays the sound attached to this GameObject using the EventReference.
            RuntimeManager.PlayOneShotAttached(fmodEvent, gameObject);
            Debug.Log($"FMOD Event {eventIdentifier} played, attached to {gameObject.name}.", this);
        }
        else
        {
            // Plays the sound at the position of this GameObject using the EventReference.
            RuntimeManager.PlayOneShot(fmodEvent, transform.position);
            Debug.Log($"FMOD Event {eventIdentifier} played at position of {gameObject.name}.", this);
        }
    }

    // Optional: For visualizing the trigger area in the editor
    void OnDrawGizmos()
    {
        if (_collider == null) _collider = GetComponent<Collider>(); // Try to get it if not set in Awake (e.g. editor time)

        if (_collider != null)
        {
            Gizmos.color = hasBeenTriggered && playOnce ? Color.gray : (Color.green * 0.7f); // Green if active, gray if used

            // Draw different shapes based on collider type
            if (_collider is BoxCollider boxCollider)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.TransformPoint(boxCollider.center), transform.rotation, transform.lossyScale);
                Gizmos.DrawWireCube(Vector3.zero, boxCollider.size);
            }
            else if (_collider is SphereCollider sphereCollider)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.TransformPoint(sphereCollider.center), transform.rotation, transform.lossyScale);
                float maxScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
                Gizmos.DrawWireSphere(Vector3.zero, sphereCollider.radius * maxScale);
            }
            else if (_collider is CapsuleCollider capsuleCollider)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.TransformPoint(capsuleCollider.center), transform.rotation, transform.lossyScale);
                float radiusScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
                float heightScale = transform.lossyScale.y;
                // Simplified gizmo for capsule - drawing a proper scaled capsule is more involved
                Gizmos.DrawWireSphere(Vector3.zero, Mathf.Max(capsuleCollider.radius * radiusScale, (capsuleCollider.height * heightScale) / 2f));
            }
            else
            {
                Gizmos.DrawIcon(transform.position, "FMODAudioSource.png", true);
            }
            Gizmos.matrix = Matrix4x4.identity; // Reset matrix
        }
    }

    // This function can be useful if you want to manually update the fmodEventPath in the inspector
    // when the fmodEvent is changed, as OnValidate is called in the editor when a property changes.
    void OnValidate()
    {
        if (!fmodEvent.IsNull)
        {
            // This ensures the fmodEventPath string in the inspector updates
            // if you change the EventReference via the drag-and-drop.
            // FMOD.Studio.EventDescription eventDescription;
            // if (RuntimeManager.StudioSystem.getEventByID(fmodEvent.Guid, out eventDescription) == FMOD.RESULT.OK) {
            //    eventDescription.getPath(out fmodEventPath);
            // }
            // Note: Calling RuntimeManager.StudioSystem in OnValidate can sometimes be problematic
            // if FMOD isn't fully initialized or if you're not in play mode.
            // A simpler approach for OnValidate might be to just clear fmodEventPath if fmodEvent is null,
            // or rely on Awake to populate it. For robust path fetching, Awake/Start is better.
            // For now, I'll leave the more robust path fetching in Awake.
        }
        else
        {
            fmodEventPath = string.Empty;
        }
    }
}