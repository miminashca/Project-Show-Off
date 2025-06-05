using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic; // Needed for Dictionary

public class PlayerFootsteps : MonoBehaviour
{
    public EventReference footstepsEvent;

    // FMOD Parameter names
    private const string PARAM_DIRT = "Dirt";
    private const string PARAM_MUD = "Mud";
    private const string PARAM_SHALLOW_WATER = "Shallow Water";
    private const string PARAM_DEEP_WATER = "Deep Water";
    private const string PARAM_WOOD = "Wood";
    private const string PARAM_GRASS = "Grass";
    private const string PARAM_MOVEMENT_STATE = "MovementState";

    [Header("Ground Detection Settings")]
    public float raycastDistance = 1.5f;
    public Vector3 raycastOriginOffset = new Vector3(0, 0.5f, 0); // You've set this to 0.5 in Y
    public LayerMask groundLayerMask;

    private float currentMovementState = 0.5f;

    private struct FootstepSoundBlend
    {
        public float dirt;
        public float mud;
        public float shallowWater;
        public float deepWater;
        public float wood;
        public float grass;

        public FootstepSoundBlend(float d, float m, float sw, float dw, float w, float g)
        {
            dirt = d; mud = m; shallowWater = sw; deepWater = dw; wood = w; grass = g;
        }
    }

    private Dictionary<string, FootstepSoundBlend> materialBlends = new Dictionary<string, FootstepSoundBlend>();
    private FootstepSoundBlend currentActiveBlend;
    private string lastDetectedMaterialKey = "Default";

    void Start()
    {
        if (footstepsEvent.IsNull)
        {
            Debug.LogError("PlayerFootsteps Start: FMOD Footsteps Event Reference is NOT assigned on " + gameObject.name);
        }

        // IMPORTANT: The KEYS here (e.g., "GrassyPeat") must now correspond to
        // part of the NAME of your TERRAIN LAYER assets in Unity.
        // For example, if you have a TerrainLayer asset named "TL_GrassyPeat" or "GrassyPeat_Texture",
        // the key "GrassyPeat" will match if it's contained within that name (case-insensitive).
        materialBlends.Add("GrassyPeat", new FootstepSoundBlend(d: 0.4f, m: 0.1f, sw: 0.0f, dw: 0.0f, w: 0.0f, g: 0.8f));
        materialBlends.Add("MossyPeat", new FootstepSoundBlend(d: 0.4f, m: 0.1f, sw: 0.0f, dw: 0.0f, w: 0.0f, g: 0.8f));
        materialBlends.Add("Pathway", new FootstepSoundBlend(d: 0.7f, m: 0.2f, sw: 0.1f, dw: 0.0f, w: 0.0f, g: 0.3f)); // Make sure your Terrain Layer for paths has "Pathway" in its name
        materialBlends.Add("Peat", new FootstepSoundBlend(d: 0.2f, m: 0.8f, sw: 0.1f, dw: 0.0f, w: 0.0f, g: 0.2f));
        materialBlends.Add("Default", new FootstepSoundBlend(d: 0.6f, m: 0.1f, sw: 0.0f, dw: 0.0f, w: 0.0f, g: 0.1f));

        currentActiveBlend = materialBlends["Default"];

        if (groundLayerMask == 0) // LayerMask not set
        {
            Debug.LogWarning("PlayerFootsteps: Ground Layer Mask is not set in the Inspector. Ground detection might not work correctly.");
        }
    }

    public void SetMovementState(float stateValue)
    {
        currentMovementState = stateValue;
    }

    /// <summary>
    /// Detects the ground material beneath the player and updates the currentActiveBlend.
    /// Handles both Unity Terrains (by texture) and regular meshes (by material).
    /// </summary>
    void DetectGroundMaterial()
    {
        Vector3 rayOrigin = transform.position + raycastOriginOffset;
        RaycastHit hit;
        string determinedKey = "Default"; // Start with default, change if a match is found

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, raycastDistance, groundLayerMask))
        {
            Terrain terrain = hit.collider.GetComponent<Terrain>();

            if (terrain != null) // Hit a Unity Terrain
            {
                string terrainLayerName = GetDominantTerrainLayerName(terrain, hit.point);
                if (!string.IsNullOrEmpty(terrainLayerName))
                {
                    // Debug.Log($"Hit Terrain. Dominant Layer: {terrainLayerName}");
                    string terrainLayerNameLower = terrainLayerName.ToLower();
                    foreach (var blendEntry in materialBlends)
                    {
                        if (terrainLayerNameLower.Contains(blendEntry.Key.ToLower()))
                        {
                            determinedKey = blendEntry.Key;
                            break;
                        }
                    }
                }
            }
            else // Hit a regular mesh collider (not a Terrain)
            {
                Renderer renderer = hit.collider.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    string materialName = renderer.sharedMaterial.name;
                    // Debug.Log($"Hit Regular Mesh. Material: {materialName}");
                    string materialNameLower = materialName.ToLower();
                    foreach (var blendEntry in materialBlends)
                    {
                        if (materialNameLower.Contains(blendEntry.Key.ToLower()))
                        {
                            determinedKey = blendEntry.Key;
                            break;
                        }
                    }
                }
            }
        }
        // else: Raycast didn't hit anything on the ground layer, will use 'determinedKey' which is "Default".

        // Update the active blend
        if (materialBlends.TryGetValue(determinedKey, out FootstepSoundBlend newBlend))
        {
            currentActiveBlend = newBlend;
            if (lastDetectedMaterialKey != determinedKey)
            {
                // Debug.Log($"PlayerFootsteps: Switched to blend '{determinedKey}'.");
                lastDetectedMaterialKey = determinedKey;
            }
        }
        else
        {
            // This case should ideally not be reached if "Default" is always a valid key
            // and determinedKey defaults to "Default".
            currentActiveBlend = materialBlends["Default"];
            if (lastDetectedMaterialKey != "Default")
            {
                Debug.LogWarning($"PlayerFootsteps: Could not find blend for key '{determinedKey}'. Using Default.");
                lastDetectedMaterialKey = "Default";
            }
        }
    }

    /// <summary>
    /// Gets the name of the dominant terrain layer at a given world position on a terrain.
    /// </summary>
    private string GetDominantTerrainLayerName(Terrain terrain, Vector3 worldPos)
    {
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;

        // Convert world coords to terrain local coords
        int mapX = (int)(((worldPos.x - terrainPos.x) / terrainData.size.x) * (terrainData.alphamapWidth - 1));
        int mapZ = (int)(((worldPos.z - terrainPos.z) / terrainData.size.z) * (terrainData.alphamapHeight - 1));

        // Ensure coordinates are within alphamap bounds
        mapX = Mathf.Clamp(mapX, 0, terrainData.alphamapWidth - 1);
        mapZ = Mathf.Clamp(mapZ, 0, terrainData.alphamapHeight - 1);

        // Get the splat data for this point
        float[,,] splatmapData = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

        if (splatmapData == null || terrainData.alphamapLayers == 0) return null;

        float maxMix = 0;
        int maxIndex = 0;

        // Loop through each terrain layer
        for (int n = 0; n < terrainData.alphamapLayers; n++)
        {
            if (splatmapData[0, 0, n] > maxMix)
            {
                maxIndex = n;
                maxMix = splatmapData[0, 0, n];
            }
        }

        // Get the TerrainLayer asset
        if (maxIndex < terrainData.terrainLayers.Length && terrainData.terrainLayers[maxIndex] != null)
        {
            return terrainData.terrainLayers[maxIndex].name;
        }
        return null; // No dominant layer found or TerrainLayers not set up correctly
    }


    public void PlayFootstep()
    {
        if (footstepsEvent.IsNull)
        {
            Debug.LogWarning("PlayFootstep(): FMOD footsteps event is not assigned. Cannot play sound.");
            return;
        }

        DetectGroundMaterial(); // Detect ground material before playing

        EventInstance currentFootstepInstance = RuntimeManager.CreateInstance(footstepsEvent);
        currentFootstepInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));

        SetEnvironmentParametersForInstance(currentFootstepInstance);
        SetMovementStateParameterForInstance(currentFootstepInstance);

        currentFootstepInstance.start();
        currentFootstepInstance.release();
    }

    private void SetEnvironmentParametersForInstance(EventInstance instance)
    {
        if (instance.isValid())
        {
            instance.setParameterByName(PARAM_DIRT, currentActiveBlend.dirt);
            instance.setParameterByName(PARAM_MUD, currentActiveBlend.mud);
            instance.setParameterByName(PARAM_SHALLOW_WATER, currentActiveBlend.shallowWater);
            instance.setParameterByName(PARAM_DEEP_WATER, currentActiveBlend.deepWater);
            instance.setParameterByName(PARAM_WOOD, currentActiveBlend.wood);
            instance.setParameterByName(PARAM_GRASS, currentActiveBlend.grass);
            // Debug.Log($"FMOD Params Set for '{lastDetectedMaterialKey}': D:{currentActiveBlend.dirt:F1} M:{currentActiveBlend.mud:F1} G:{currentActiveBlend.grass:F1}");
        }
        else
        {
            Debug.LogWarning("SetEnvironmentParametersForInstance(): FMOD instance not valid, cannot set parameters.");
        }
    }

    private void SetMovementStateParameterForInstance(EventInstance instance)
    {
        if (instance.isValid())
        {
            instance.setParameterByName(PARAM_MOVEMENT_STATE, currentMovementState);
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 rayOrigin = transform.position + raycastOriginOffset;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * raycastDistance);
    }
}