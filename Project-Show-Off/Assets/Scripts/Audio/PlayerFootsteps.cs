using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class PlayerFootsteps : MonoBehaviour
{
    // Removed [EventRef] - EventReference struct provides inspector functionality automatically
    public EventReference footstepsEvent;

    // FMOD Parameter names (make sure these match exactly in FMOD Studio)
    private const string PARAM_DIRT = "Dirt";
    private const string PARAM_SHALLOW_WATER = "Shallow Water";
    private const string PARAM_DEEP_WATER = "Deep Water";
    private const string PARAM_WOOD = "Wood";

    // Trigger flags for environment detection
    private bool isInShallowTrigger = false;
    private bool isInDeepTrigger = false;

    void Start()
    {
        // Debug.Log("PlayerFootsteps Start: Initializing.");
        if (footstepsEvent.IsNull)
        {
            Debug.LogError("PlayerFootsteps Start: FMOD Footsteps Event Reference is NOT assigned on " + gameObject.name);
        }
    }

    /// <summary>
    /// Plays a single footstep sound at the current player's location.
    /// </summary>
    public void PlayFootstep()
    {
        // Debug.Log("PlayFootstep() called!");

        if (footstepsEvent.IsNull)
        {
            Debug.LogWarning("PlayFootstep(): FMOD footsteps event is not assigned. Cannot play sound.");
            return;
        }

        // 1. Create a NEW instance for each footstep sound
        EventInstance currentFootstepInstance = RuntimeManager.CreateInstance(footstepsEvent);

        // 2. Set 3D attributes immediately after creation
        //    Use 'transform' directly to pass position, velocity, and orientation for better 3D spatialization.
        currentFootstepInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));

        // 3. Set the FMOD parameters based on current environment
        SetEnvironmentParametersForInstance(currentFootstepInstance);

        // 4. Start the event instance
        currentFootstepInstance.start();

        // 5. Release the instance immediately. FMOD will manage playing and cleaning up.
        //    This is vital for one-shot sounds to prevent "too many instances" issues.
        currentFootstepInstance.release();

        // Debug.Log("PlayFootstep(): Footstep sound attempted to play and instance released.");
    }

    /// <summary>
    /// Sets the environment parameters on a given FMOD event instance.
    /// </summary>
    /// <param name="instance">The FMOD EventInstance to set parameters on.</param>
    private void SetEnvironmentParametersForInstance(EventInstance instance)
    {
        float dirt = 0.9f;
        float shallowWater = 0.1f;
        float deepWater = 0.0f;
        float wood = 0.0f;
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

        // Always check if the instance is valid before setting parameters
        if (instance.isValid())
        {
            instance.setParameterByName(PARAM_DIRT, dirt);
            instance.setParameterByName(PARAM_SHALLOW_WATER, shallowWater);
            instance.setParameterByName(PARAM_DEEP_WATER, deepWater);
            instance.setParameterByName(PARAM_WOOD, wood);

            // Debug.Log($"PlayerFootsteps: Environment set to {currentEnvironment}. Dirt: {dirt}, Shallow: {shallowWater}, Deep: {deepWater}");
        }
        else
        {
            Debug.LogWarning("SetEnvironmentParametersForInstance(): FMOD instance not valid, cannot set parameters.");
        }
    }

    // OnTriggerEnter and OnTriggerExit methods remain unchanged
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ShallowCollider")) { isInShallowTrigger = true; /* Debug.Log("PlayerFootsteps: Entered ShallowCollider."); */ }
        else if (other.CompareTag("DeepCollider")) { isInDeepTrigger = true; /* Debug.Log("PlayerFootsteps: Entered DeepCollider."); */ }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("ShallowCollider")) { isInShallowTrigger = false; /* Debug.Log("PlayerFootsteps: Exited ShallowCollider."); */ }
        else if (other.CompareTag("DeepCollider")) { isInDeepTrigger = false; /* Debug.Log("PlayerFootsteps: Exited DeepCollider."); */ }
    }
}