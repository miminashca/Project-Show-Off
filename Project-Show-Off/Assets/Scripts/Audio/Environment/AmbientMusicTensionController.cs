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

    [Header("Spirit Tree Tension Settings")]
    [Tooltip("The maximum distance (in meters) at which a Spirit Tree starts influencing tension.")]
    public float treeMaxInfluenceDistance = 75f;

    // ----- NEW: MONSTER TENSION SETTINGS -----
    [Header("Monster Tension Settings")]
    [Tooltip("Layers that contain monsters which should trigger tension (e.g., Hunter, Nixie).")]
    public LayerMask monsterLayers;
    [Tooltip("The maximum distance (in meters) at which a monster starts influencing tension.")]
    public float monsterMaxDetectionRange = 50f;
    [Tooltip("The distance (in meters) at which a monster causes maximum tension (1.0).")]
    public float monsterMaxTensionRange = 10f;
    // -------------------------------------------

    [Header("Tension Control Settings")]
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
            ambientMusicInstance.start();
        }
        else
        {
            Debug.LogError("AmbientMusicTensionController: Ambient Music EventReference is not set. Disabling script.");
            enabled = false;
            return;
        }

        // --- Find Spirit Trees ---
        spiritTrees = GameObject.FindGameObjectsWithTag("SpiritTree");
        if (spiritTrees.Length == 0)
        {
            Debug.LogWarning("AmbientMusicTensionController: No GameObjects found with the tag 'SpiritTree'.");
        }
    }

    void Update()
    {
        if (!ambientMusicInstance.isValid() || playerTransform == null) return;

        // --- Calculate tension from different sources ---
        float treeTension = CalculateTreeTension();
        float monsterTension = CalculateMonsterTension();

        // --- Determine the final target tension ---
        // We use the highest tension value from all sources.
        // This means if a monster is close, its high tension will override the lower tree tension.
        float targetTension = Mathf.Max(treeTension, monsterTension);

        SmoothlyUpdateTension(targetTension);
    }

    private float CalculateTreeTension()
    {
        if (spiritTrees == null || spiritTrees.Length == 0) return 0f;

        float closestDistanceSqr = Mathf.Infinity;
        foreach (GameObject tree in spiritTrees)
        {
            if (tree == null || !tree.activeInHierarchy) continue;

            float distanceSqr = (tree.transform.position - playerTransform.position).sqrMagnitude;
            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
            }
        }

        if (closestDistanceSqr <= (treeMaxInfluenceDistance * treeMaxInfluenceDistance))
        {
            float actualDistance = Mathf.Sqrt(closestDistanceSqr);
            // Inversely scale tension from 0.4 (at 0 distance) to 0 (at max distance)
            float tension = 0.4f * (1.0f - (actualDistance / treeMaxInfluenceDistance));
            return Mathf.Clamp(tension, 0f, 0.4f);
        }

        return 0f;
    }

    private float CalculateMonsterTension()
    {
        // Use an overlap sphere to find all monster colliders within the max detection range.
        // This is more performant than finding all monsters in the scene every frame.
        Collider[] monstersInRange = Physics.OverlapSphere(playerTransform.position, monsterMaxDetectionRange, monsterLayers);

        if (monstersInRange.Length == 0) return 0f; // No monsters nearby

        float closestMonsterDistSqr = Mathf.Infinity;
        foreach (var monsterCollider in monstersInRange)
        {
            // We find the closest point on the collider to the player for more accurate distance
            Vector3 closestPoint = monsterCollider.ClosestPoint(playerTransform.position);
            float distSqr = (closestPoint - playerTransform.position).sqrMagnitude;
            if (distSqr < closestMonsterDistSqr)
            {
                closestMonsterDistSqr = distSqr;
            }
        }

        float closestDistance = Mathf.Sqrt(closestMonsterDistSqr);

        // If the player is within the maximum tension range, tension is 1.0
        if (closestDistance <= monsterMaxTensionRange)
        {
            return 1.0f;
        }

        // If the player is between the max tension and max detection range, scale the tension
        // We calculate how far the player is into the "detection zone" as a percentage
        // and invert it, so closer means higher tension.
        float tension = 1.0f - ((closestDistance - monsterMaxTensionRange) / (monsterMaxDetectionRange - monsterMaxTensionRange));
        return Mathf.Clamp01(tension);
    }

    void SmoothlyUpdateTension(float targetTension)
    {
        currentTensionValue = Mathf.Lerp(currentTensionValue, targetTension, Time.deltaTime * tensionSmoothingSpeed);

        if (ambientMusicInstance.isValid())
        {
            ambientMusicInstance.setParameterByName(TENSION_PARAMETER_NAME, currentTensionValue);
        }
    }

    void OnDestroy()
    {
        if (ambientMusicInstance.isValid())
        {
            ambientMusicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            ambientMusicInstance.release();
        }
    }

    // This function ensures your ranges make sense in the editor.
    private void OnValidate()
    {
        if (monsterMaxTensionRange < 0) monsterMaxTensionRange = 0;
        if (monsterMaxDetectionRange < monsterMaxTensionRange)
        {
            monsterMaxDetectionRange = monsterMaxTensionRange + 1.0f;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;

        // Gizmo for Spirit Tree tension
        Gizmos.color = new Color(0, 1, 1, 0.25f); // Cyan
        Gizmos.DrawWireSphere(playerTransform.position, treeMaxInfluenceDistance);

        // ----- NEW: GIZMOS FOR MONSTER TENSION -----
        // Gizmo for outer monster detection range
        Gizmos.color = new Color(1, 1, 0, 0.25f); // Yellow
        Gizmos.DrawWireSphere(playerTransform.position, monsterMaxDetectionRange);

        // Gizmo for inner (max) monster tension range
        Gizmos.color = new Color(1, 0, 0, 0.4f); // Red
        Gizmos.DrawWireSphere(playerTransform.position, monsterMaxTensionRange);
        // ------------------------------------------
    }
}