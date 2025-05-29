using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class PlayerFootsteps : MonoBehaviour
{
    public EventReference footstepsEvent;

    // FMOD Parameter names (make sure these match exactly in FMOD Studio)
    private const string PARAM_DIRT = "Dirt";
    private const string PARAM_SHALLOW_WATER = "Shallow Water";
    private const string PARAM_DEEP_WATER = "Deep Water";
    private const string PARAM_WOOD = "Wood";
    private const string PARAM_GRASS = "Grass";
    private const string PARAM_MOVEMENT_STATE = "MovementState"; // <--- NEW PARAMETER NAME

    // Trigger flags for environment detection
    private bool isInShallowTrigger = false;
    private bool isInDeepTrigger = false;

    // <--- NEW: Variable to store the current movement state --->
    private float currentMovementState = 0.5f; // Default to walk

    void Start()
    {
        if (footstepsEvent.IsNull)
        {
            Debug.LogError("PlayerFootsteps Start: FMOD Footsteps Event Reference is NOT assigned on " + gameObject.name);
        }
    }

    /// <summary>
    /// Sets the MovementState parameter value for future footstep events.
    /// </summary>
    /// <param name="stateValue">0.0 for Crouch, 0.5 for Walk, 1.0 for Sprint.</param>
    public void SetMovementState(float stateValue)
    {
        currentMovementState = stateValue;
        // Debug.Log($"PlayerFootsteps: MovementState set to {currentMovementState}");
    }

    /// <summary>
    /// Plays a single footstep sound at the current player's location.
    /// </summary>
    public void PlayFootstep()
    {
        if (footstepsEvent.IsNull)
        {
            Debug.LogWarning("PlayFootstep(): FMOD footsteps event is not assigned. Cannot play sound.");
            return;
        }

        EventInstance currentFootstepInstance = RuntimeManager.CreateInstance(footstepsEvent);
        currentFootstepInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));

        // 3. Set the FMOD parameters based on current environment and movement state
        SetEnvironmentParametersForInstance(currentFootstepInstance);
        SetMovementStateParameterForInstance(currentFootstepInstance); // <--- NEW CALL

        currentFootstepInstance.start();
        currentFootstepInstance.release();
    }

    /// <summary>
    /// Sets the environment parameters on a given FMOD event instance.
    /// </summary>
    /// <param name="instance">The FMOD EventInstance to set parameters on.</param>
    private void SetEnvironmentParametersForInstance(EventInstance instance)
    {
        float dirt = 0.8f;
        float shallowWater = 0.15f;
        float deepWater = 0.0f;
        float wood = 0.0f;
        float grass = 0.4f;
        string currentEnvironment = "Base"; // For logging

        if (isInDeepTrigger)
        {
            currentEnvironment = "Deep Water";
            dirt = 0.0f; shallowWater = 0.20f; deepWater = 0.80f; wood = 0.0f;
        }
        else if (isInShallowTrigger)
        {
            currentEnvironment = "Shallow Water";
            dirt = 0.0f; shallowWater = 0.75f; deepWater = 0.25f; wood = 0.0f;
        }
        // Add more environment checks here if needed (e.g., ground texture raycast)

        if (instance.isValid())
        {
            instance.setParameterByName(PARAM_DIRT, dirt);
            instance.setParameterByName(PARAM_SHALLOW_WATER, shallowWater);
            instance.setParameterByName(PARAM_DEEP_WATER, deepWater);
            instance.setParameterByName(PARAM_WOOD, wood);
            instance.setParameterByName(PARAM_GRASS, grass);
            // Debug.Log($"PlayerFootsteps: Environment set to {currentEnvironment}. Dirt: {dirt}, Shallow: {shallowWater}, Deep: {deepWater}");
        }
        else
        {
            Debug.LogWarning("SetEnvironmentParametersForInstance(): FMOD instance not valid, cannot set parameters.");
        }
    }

    /// <summary>
    /// Sets the MovementState parameter on a given FMOD event instance.
    /// </summary>
    /// <param name="instance">The FMOD EventInstance to set parameters on.</param>
    private void SetMovementStateParameterForInstance(EventInstance instance)
    {
        if (instance.isValid())
        {
            instance.setParameterByName(PARAM_MOVEMENT_STATE, currentMovementState);
            // Debug.Log($"PlayerFootsteps: Set {PARAM_MOVEMENT_STATE} to {currentMovementState:F1}");
        }
    }

    // OnTriggerEnter and OnTriggerExit methods remain unchanged
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ShallowCollider")) { isInShallowTrigger = true; }
        else if (other.CompareTag("DeepCollider")) { isInDeepTrigger = true; }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("ShallowCollider")) { isInShallowTrigger = false; }
        else if (other.CompareTag("DeepCollider")) { isInDeepTrigger = false; }
    }
}