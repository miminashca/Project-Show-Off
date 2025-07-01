using UnityEngine;
using FMODUnity; // Required for EventReference and RuntimeManager
using FMOD.Studio; // Required for EventInstance
using System.Collections; // Required for IEnumerator if you were to use coroutines

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

    [Header("Monster Tension Settings")]
    [Tooltip("Layers that contain monsters which should trigger tension (e.g., Hunter, Nixie).")]
    public LayerMask monsterLayers;
    [Tooltip("The maximum distance (in meters) at which a monster starts influencing tension.")]
    public float monsterMaxDetectionRange = 50f;
    [Tooltip("The distance (in meters) at which a monster causes maximum tension (1.0).")]
    public float monsterMaxTensionRange = 10f;

    // ----- NEW: SHOT TENSION SETTINGS -----
    [Header("Shot Tension Settings")]
    [Tooltip("The value the tension parameter will spike to when the player is shot.")]
    public float shotTensionAmount = 1.0f;
    [Tooltip("How long (in seconds) the high tension will linger after being shot.")]
    public float shotLingerDuration = 10.0f;
    [Tooltip("How quickly the tension ramps up after being shot. Should be faster than the normal smoothing speed.")]
    public float shotTensionRampUpSpeed = 10.0f;
    // ------------------------------------

    [Header("Tension Control Settings")]
    [Tooltip("How quickly the tension parameter smooths to its target value.")]
    public float tensionSmoothingSpeed = 2.0f;

    private float currentTensionValue = 0f;
    private GameObject[] spiritTrees; // To cache found trees

    // ----- NEW -----
    private float shotLingerTimer = 0.0f;
    // ---------------

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

        // ----- NEW: Update shot linger timer -----
        if (shotLingerTimer > 0)
        {
            shotLingerTimer -= Time.deltaTime;
        }
        // ----------------------------------------

        // --- Calculate tension from different sources ---
        float treeTension = CalculateTreeTension();
        float monsterTension = CalculateMonsterTension();

        // ----- NEW: Calculate tension from being shot -----
        float shotTension = CalculateShotTension();
        // --------------------------------------------------

        // --- Determine the final target tension ---
        // ----- MODIFIED: We use the highest tension value from ALL sources. -----
        // This ensures the shot tension overrides proximity, but if proximity is higher, it will be used instead.
        float targetTension = Mathf.Max(treeTension, monsterTension, shotTension);

        SmoothlyUpdateTension(targetTension);
    }

    // ----- NEW: PUBLIC METHOD TO BE CALLED FROM OTHER SCRIPTS -----
    /// <summary>
    /// Call this method from your player's health script when they take damage from a shot.
    /// It will trigger a period of high musical tension that lingers for a set duration.
    /// </summary>
    public void TriggerShotTension()
    {
        Debug.Log("Shot tension triggered!");
        shotLingerTimer = shotLingerDuration;
    }
    // --------------------------------------------------------------

    // ----- NEW: CALCULATES THE SHOT TENSION VALUE -----
    private float CalculateShotTension()
    {
        if (shotLingerTimer <= 0) return 0f;

        // Calculate tension based on the remaining timer.
        // This makes the tension fade out over the linger duration.
        // It's a linear fade-out from shotTensionAmount to 0.
        float tension = (shotLingerTimer / shotLingerDuration) * shotTensionAmount;
        return Mathf.Clamp01(tension);
    }
    // ----------------------------------------------------

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
            float tension = 0.4f * (1.0f - (actualDistance / treeMaxInfluenceDistance));
            return Mathf.Clamp(tension, 0f, 0.4f);
        }

        return 0f;
    }

    private float CalculateMonsterTension()
    {
        Collider[] monstersInRange = Physics.OverlapSphere(playerTransform.position, monsterMaxDetectionRange, monsterLayers);
        if (monstersInRange.Length == 0) return 0f;

        float closestMonsterDistSqr = Mathf.Infinity;
        foreach (var monsterCollider in monstersInRange)
        {
            Vector3 closestPoint = monsterCollider.ClosestPoint(playerTransform.position);
            float distSqr = (closestPoint - playerTransform.position).sqrMagnitude;
            if (distSqr < closestMonsterDistSqr)
            {
                closestMonsterDistSqr = distSqr;
            }
        }

        float closestDistance = Mathf.Sqrt(closestMonsterDistSqr);
        if (closestDistance <= monsterMaxTensionRange) return 1.0f;

        float tension = 1.0f - ((closestDistance - monsterMaxTensionRange) / (monsterMaxDetectionRange - monsterMaxTensionRange));
        return Mathf.Clamp01(tension);
    }

    // ----- MODIFIED: To allow for a faster ramp-up after being shot -----
    void SmoothlyUpdateTension(float targetTension)
    {
        // Determine which smoothing speed to use
        float currentSmoothingSpeed = tensionSmoothingSpeed;

        // If the shot linger is active AND we are trying to increase the tension, use the faster ramp-up speed.
        if (shotLingerTimer > 0 && targetTension > currentTensionValue)
        {
            currentSmoothingSpeed = shotTensionRampUpSpeed;
        }

        currentTensionValue = Mathf.Lerp(currentTensionValue, targetTension, Time.deltaTime * currentSmoothingSpeed);

        if (ambientMusicInstance.isValid())
        {
            ambientMusicInstance.setParameterByName(TENSION_PARAMETER_NAME, currentTensionValue);
        }
    }
    // ------------------------------------------------------------------------

    void OnDestroy()
    {
        if (ambientMusicInstance.isValid())
        {
            ambientMusicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            ambientMusicInstance.release();
        }
    }

    private void OnValidate()
    {
        if (monsterMaxTensionRange < 0) monsterMaxTensionRange = 0;
        if (monsterMaxDetectionRange < monsterMaxTensionRange)
        {
            monsterMaxDetectionRange = monsterMaxTensionRange + 1.0f;
        }
        // ----- NEW: Validation for shot linger settings -----
        if (shotLingerDuration < 0) shotLingerDuration = 0;
        if (shotTensionAmount < 0) shotTensionAmount = 0;
        if (shotTensionRampUpSpeed < 0) shotTensionRampUpSpeed = 0;
    }

    void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;
        Gizmos.color = new Color(0, 1, 1, 0.25f);
        Gizmos.DrawWireSphere(playerTransform.position, treeMaxInfluenceDistance);
        Gizmos.color = new Color(1, 1, 0, 0.25f);
        Gizmos.DrawWireSphere(playerTransform.position, monsterMaxDetectionRange);
        Gizmos.color = new Color(1, 0, 0, 0.4f);
        Gizmos.DrawWireSphere(playerTransform.position, monsterMaxTensionRange);
    }
}