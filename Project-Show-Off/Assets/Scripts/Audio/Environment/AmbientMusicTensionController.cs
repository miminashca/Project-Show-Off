using UnityEngine;
using FMODUnity; // Required for EventReference and RuntimeManager
using FMOD.Studio; // Required for EventInstance

public class AmbientMusicTensionController : MonoBehaviour
{
    [Header("FMOD Settings")]
    [Tooltip("Drag your FMOD Ambient Music Event here.")]
    public EventReference ambientMusicEventReference;
    private EventInstance ambientMusicInstance;
    private const string TENSION_PARAMETER_NAME = "Tension"; // Make sure this matches your FMOD parameter name

    [Header("Player Settings")]
    [Tooltip("Assign the player's Transform. If null, will try to find GameObject with 'Player' tag.")]
    public Transform playerTransform;

    [Header("Tension Control Settings")]
    [Tooltip("The maximum distance (in meters) at which a Spirit Tree starts influencing tension.")]
    public float maxInfluenceDistance = 75f;
    [Tooltip("How quickly the tension parameter smooths to its target value.")]
    public float tensionSmoothingSpeed = 2.0f;

    private float currentTensionValue = 0f;
    private GameObject[] spiritTrees; // To cache found trees

    void Start()
    {
        // --- Player Setup ---
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                Debug.LogError("AmbientMusicTensionController: Player Transform not assigned and GameObject with tag 'Player' not found. Disabling script.");
                enabled = false;
                return;
            }
        }

        // --- FMOD Event Setup ---
        if (!ambientMusicEventReference.IsNull)
        {
            ambientMusicInstance = RuntimeManager.CreateInstance(ambientMusicEventReference);
            // Optional: If your ambient music event is 3D, you might want to attach it.
            // For general ambient music, it's often 2D and doesn't need attaching.
            // RuntimeManager.AttachInstanceToGameObject(ambientMusicInstance, transform, GetComponent<Rigidbody>());
            ambientMusicInstance.start();
            Debug.Log("Ambient Music event started.");
        }
        else
        {
            Debug.LogError("AmbientMusicTensionController: Ambient Music EventReference is not set. Disabling script.");
            enabled = false;
            return;
        }

        // --- Find Spirit Trees ---
        // It's good practice to find them once if they don't change often.
        // If trees can be added/removed dynamically, you might need to update this list.
        spiritTrees = GameObject.FindGameObjectsWithTag("SpiritTree");
        if (spiritTrees.Length == 0)
        {
            Debug.LogWarning("AmbientMusicTensionController: No GameObjects found with the tag 'SpiritTree'. Tension will remain at 0 unless trees are added and found later.");
        }
    }

    void Update()
    {
        if (!ambientMusicInstance.isValid() || playerTransform == null)
        {
            return; // Exit if FMOD instance is not valid or player is missing
        }

        // If no trees were found at start, or if you want to dynamically find them:
        // You could uncomment this if trees are frequently added/removed,
        // but it's less performant than finding them once.
        // spiritTrees = GameObject.FindGameObjectsWithTag("SpiritTree");

        if (spiritTrees.Length == 0)
        {
            // No trees, target tension should be 0
            SmoothlyUpdateTension(0f);
            return;
        }

        float closestDistanceSqr = Mathf.Infinity;
        // Transform closestTree = null; // Not strictly needed for this logic

        foreach (GameObject tree in spiritTrees)
        {
            if (tree == null || !tree.activeInHierarchy) continue; // Skip inactive or destroyed trees

            float distanceSqr = (tree.transform.position - playerTransform.position).sqrMagnitude;
            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                // closestTree = tree.transform;
            }
        }

        float targetTension = 0f;
        if (closestDistanceSqr <= (maxInfluenceDistance * maxInfluenceDistance)) // Compare squared distances to avoid Sqrt
        {
            float actualDistance = Mathf.Sqrt(closestDistanceSqr);
            // Linear interpolation: 1.0 when distance is 0, 0.0 when distance is maxInfluenceDistance
            targetTension = 1.0f - (actualDistance / maxInfluenceDistance);
            targetTension = Mathf.Clamp01(targetTension); // Ensure value is between 0 and 1
        }
        // If closestDistanceSqr is greater than maxInfluenceDistance^2, targetTension remains 0

        SmoothlyUpdateTension(targetTension);
    }

    void SmoothlyUpdateTension(float targetTension)
    {
        // Lerp the current tension value towards the target value for a smoother transition
        currentTensionValue = Mathf.Lerp(currentTensionValue, targetTension, Time.deltaTime * tensionSmoothingSpeed);

        // Apply the smoothed tension value to the FMOD parameter
        if (ambientMusicInstance.isValid())
        {
            FMOD.RESULT result = ambientMusicInstance.setParameterByName(TENSION_PARAMETER_NAME, currentTensionValue);
            if (result != FMOD.RESULT.OK)
            {
                Debug.LogWarning($"FMOD: Could not set parameter '{TENSION_PARAMETER_NAME}'. Error: {result}");
            }
        }
    }

    void OnDestroy()
    {
        // Important: Stop and release the FMOD event instance when this GameObject is destroyed
        if (ambientMusicInstance.isValid())
        {
            ambientMusicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT); // Allow graceful fade out
            ambientMusicInstance.release();
            Debug.Log("Ambient Music event stopped and released.");
        }
    }

    // Optional: Gizmo to visualize the influence radius in the editor
    void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            Gizmos.color = new Color(1, 0.5f, 0, 0.3f); // Orange, semi-transparent
            Gizmos.DrawWireSphere(playerTransform.position, maxInfluenceDistance);
        }
    }
}