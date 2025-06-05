using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;

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
    public Vector3 raycastOriginOffset = new Vector3(0, 0.5f, 0);
    public LayerMask groundLayerMask;

    [Header("Water Detection Settings")]
    public Transform waterSurfaceTransform; // Assign your water surface GameObject here
    public float playerFeetYOffset = 0f;    // Adjust if player pivot isn't at feet level
    [Tooltip("Submersion depth at which shallow water effects start.")]
    public float minDepthForShallowEffect = 0.05f;
    [Tooltip("Submersion depth for full shallow water effect / deep water starts.")]
    public float fullShallowDepth = 0.3f;
    [Tooltip("Submersion depth for full deep water effect.")]
    public float fullDeepDepth = 0.7f;
    [Tooltip("How much to reduce ground material sounds when in shallow water (0-1)")]
    [Range(0f, 1f)]
    public float groundSoundReductionInShallow = 0.5f;
    [Tooltip("How much to reduce ground material sounds when in deep water (0-1)")]
    [Range(0f, 1f)]
    public float groundSoundReductionInDeep = 0.9f;


    private float currentMovementState = 0.5f;

    // This struct now primarily focuses on ground materials.
    // Water parameters will be calculated separately and applied.
    private struct FootstepSoundBlend
    {
        public float dirt;
        public float mud;
        public float wood;
        public float grass;
        // Removed shallowWater and deepWater as they are now dynamic

        public FootstepSoundBlend(float d, float m, float w, float g)
        {
            dirt = d; mud = m; wood = w; grass = g;
        }
    }

    private Dictionary<string, FootstepSoundBlend> materialBlends = new Dictionary<string, FootstepSoundBlend>();
    private FootstepSoundBlend currentGroundBlend; // Renamed for clarity
    private string lastDetectedMaterialKey = "Default";

    // Variables to store calculated water levels
    private float currentShallowWaterLevel = 0f;
    private float currentDeepWaterLevel = 0f;

    void Start()
    {
        if (footstepsEvent.IsNull)
        {
            Debug.LogError("PlayerFootsteps Start: FMOD Footsteps Event Reference is NOT assigned on " + gameObject.name);
        }

        // Blends for ground materials
        materialBlends.Add("GrassyPeat", new FootstepSoundBlend(d: 0.2f, m: 0.1f, w: 0.0f, g: 0.7f));
        materialBlends.Add("MossyPeat", new FootstepSoundBlend(d: 0.1f, m: 0.4f, w: 0.0f, g: 0.5f));
        materialBlends.Add("Pathway", new FootstepSoundBlend(d: 0.8f, m: 0.0f, w: 0.1f, g: 0.1f));
        materialBlends.Add("Peat", new FootstepSoundBlend(d: 0.2f, m: 0.7f, w: 0.0f, g: 0.1f));
        materialBlends.Add("Default", new FootstepSoundBlend(d: 0.6f, m: 0.1f, w: 0.0f, g: 0.1f));

        currentGroundBlend = materialBlends["Default"];

        if (groundLayerMask == 0)
        {
            Debug.LogWarning("PlayerFootsteps: Ground Layer Mask is not set. Ground detection might not work.");
        }
        if (waterSurfaceTransform == null)
        {
            Debug.LogWarning("PlayerFootsteps: Water Surface Transform is not assigned. Water detection will be disabled.");
        }
        // Validate depth settings to prevent illogical configurations
        if (fullShallowDepth < minDepthForShallowEffect)
        {
            Debug.LogWarning("Water Detection Settings: 'Full Shallow Depth' should be greater than or equal to 'Min Depth For Shallow Effect'.");
            fullShallowDepth = minDepthForShallowEffect; // Auto-correct
        }
        if (fullDeepDepth < fullShallowDepth)
        {
            Debug.LogWarning("Water Detection Settings: 'Full Deep Depth' should be greater than or equal to 'Full Shallow Depth'.");
            fullDeepDepth = fullShallowDepth; // Auto-correct
        }
    }

    void Update()
    {
        // Calculate water levels every frame
        UpdateWaterLevels();
    }

    /// <summary>
    /// Calculates the current shallow and deep water FMOD parameter levels based on player's submersion.
    /// </summary>
    void UpdateWaterLevels()
    {
        if (waterSurfaceTransform == null)
        {
            currentShallowWaterLevel = 0f;
            currentDeepWaterLevel = 0f;
            return;
        }

        float waterSurfaceY = waterSurfaceTransform.position.y;
        float playerFeetY = transform.position.y + playerFeetYOffset;
        float submersionDepth = waterSurfaceY - playerFeetY;

        if (submersionDepth <= 0) // Player is above or exactly at water level
        {
            currentShallowWaterLevel = 0f;
            currentDeepWaterLevel = 0f;
            return;
        }

        // Calculate Shallow Water parameter
        // It scales from 0 to 1 between minDepthForShallowEffect and fullShallowDepth
        currentShallowWaterLevel = Mathf.Clamp01(Mathf.InverseLerp(minDepthForShallowEffect, fullShallowDepth, submersionDepth));

        // Calculate Deep Water parameter
        // It scales from 0 to 1 between fullShallowDepth (acting as min for deep) and fullDeepDepth
        currentDeepWaterLevel = Mathf.Clamp01(Mathf.InverseLerp(fullShallowDepth, fullDeepDepth, submersionDepth));

        // Optional: If fully deep, shallow might be reduced or FMOD event handles the mix.
        // For now, shallow can stay at 1 even if deep is 1. FMOD can blend this.
        // If you want shallow to go down as deep goes up after fullShallowDepth:
        // if (currentDeepWaterLevel > 0) {
        //    currentShallowWaterLevel = Mathf.Clamp01(1f - currentDeepWaterLevel); // Example of crossfade
        // }
    }


    void DetectGroundMaterial()
    {
        Vector3 rayOrigin = transform.position + raycastOriginOffset;
        RaycastHit hit;
        string determinedKey = "Default";

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, raycastDistance, groundLayerMask))
        {
            Terrain terrain = hit.collider.GetComponent<Terrain>();
            if (terrain != null)
            {
                string terrainLayerName = GetDominantTerrainLayerName(terrain, hit.point);
                if (!string.IsNullOrEmpty(terrainLayerName))
                {
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
            else
            {
                Renderer renderer = hit.collider.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    string materialName = renderer.sharedMaterial.name;
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

        if (materialBlends.TryGetValue(determinedKey, out FootstepSoundBlend newBlend))
        {
            currentGroundBlend = newBlend;
            if (lastDetectedMaterialKey != determinedKey)
            {
                lastDetectedMaterialKey = determinedKey;
            }
        }
        else
        {
            currentGroundBlend = materialBlends["Default"];
            if (lastDetectedMaterialKey != "Default")
            {
                lastDetectedMaterialKey = "Default";
            }
        }
    }

    private string GetDominantTerrainLayerName(Terrain terrain, Vector3 worldPos)
    {
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;
        int mapX = (int)(((worldPos.x - terrainPos.x) / terrainData.size.x) * (terrainData.alphamapWidth - 1));
        int mapZ = (int)(((worldPos.z - terrainPos.z) / terrainData.size.z) * (terrainData.alphamapHeight - 1));
        mapX = Mathf.Clamp(mapX, 0, terrainData.alphamapWidth - 1);
        mapZ = Mathf.Clamp(mapZ, 0, terrainData.alphamapHeight - 1);
        float[,,] splatmapData = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);
        if (splatmapData == null || terrainData.alphamapLayers == 0) return null;
        float maxMix = 0;
        int maxIndex = 0;
        for (int n = 0; n < terrainData.alphamapLayers; n++)
        {
            if (splatmapData[0, 0, n] > maxMix)
            {
                maxIndex = n;
                maxMix = splatmapData[0, 0, n];
            }
        }
        if (maxIndex < terrainData.terrainLayers.Length && terrainData.terrainLayers[maxIndex] != null)
        {
            return terrainData.terrainLayers[maxIndex].name;
        }
        return null;
    }

    public void PlayFootstep()
    {
        if (footstepsEvent.IsNull) return;

        DetectGroundMaterial(); // Keep detecting ground for underlying material sounds

        EventInstance currentFootstepInstance = RuntimeManager.CreateInstance(footstepsEvent);
        currentFootstepInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));

        SetEnvironmentParametersForInstance(currentFootstepInstance);
        SetMovementStateParameterForInstance(currentFootstepInstance);

        currentFootstepInstance.start();
        currentFootstepInstance.release();
    }

    private void SetEnvironmentParametersForInstance(EventInstance instance)
    {
        if (!instance.isValid())
        {
            Debug.LogWarning("SetEnvironmentParametersForInstance(): FMOD instance not valid.");
            return;
        }

        // Calculate ground sound attenuation based on water level
        float groundAttenuation = 1f;
        if (currentDeepWaterLevel > 0.01f) // If significantly in deep water
        {
            groundAttenuation = 1f - groundSoundReductionInDeep;
        }
        else if (currentShallowWaterLevel > 0.01f) // If significantly in shallow water
        {
            groundAttenuation = 1f - groundSoundReductionInShallow;
        }


        // Apply ground material sounds (potentially attenuated by water)
        instance.setParameterByName(PARAM_DIRT, currentGroundBlend.dirt * groundAttenuation);
        instance.setParameterByName(PARAM_MUD, currentGroundBlend.mud * groundAttenuation);
        instance.setParameterByName(PARAM_WOOD, currentGroundBlend.wood * groundAttenuation);
        instance.setParameterByName(PARAM_GRASS, currentGroundBlend.grass * groundAttenuation);

        // Apply calculated water levels directly
        instance.setParameterByName(PARAM_SHALLOW_WATER, currentShallowWaterLevel);
        instance.setParameterByName(PARAM_DEEP_WATER, currentDeepWaterLevel);

        // Debug.Log($"FMOD Params: GroundKey='{lastDetectedMaterialKey}', Dirt:{currentGroundBlend.dirt * groundAttenuation:F2}, Mud:{currentGroundBlend.mud * groundAttenuation:F2}, Grass:{currentGroundBlend.grass * groundAttenuation:F2}, ShallowW:{currentShallowWaterLevel:F2}, DeepW:{currentDeepWaterLevel:F2}");
    }

    private void SetMovementStateParameterForInstance(EventInstance instance)
    {
        if (instance.isValid())
        {
            instance.setParameterByName(PARAM_MOVEMENT_STATE, currentMovementState);
        }
    }

    public void SetMovementState(float stateValue)
    {
        currentMovementState = stateValue;
    }

    void OnDrawGizmosSelected()
    {
        // Ground Raycast Gizmo
        Vector3 rayOrigin = transform.position + raycastOriginOffset;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * raycastDistance);

        // Water Level Gizmos (if water surface is assigned)
        if (waterSurfaceTransform != null)
        {
            float waterY = waterSurfaceTransform.position.y;
            float playerFeetY = transform.position.y + playerFeetYOffset;
            Vector3 playerFeetPos = new Vector3(transform.position.x, playerFeetY, transform.position.z);

            // Line from feet to water surface
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(playerFeetPos, new Vector3(playerFeetPos.x, waterY, playerFeetPos.z));

            // Indicate depth thresholds relative to player feet (visualized as if water is at player feet level)
            // This helps visualize the ranges if player IS at that depth.
            // Min Shallow depth
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(playerFeetPos + Vector3.down * minDepthForShallowEffect, playerFeetPos + Vector3.down * minDepthForShallowEffect + Vector3.right * 0.2f);
            // Full Shallow depth
            Gizmos.color = Color.green;
            Gizmos.DrawLine(playerFeetPos + Vector3.down * fullShallowDepth, playerFeetPos + Vector3.down * fullShallowDepth + Vector3.right * 0.3f);
            // Full Deep depth
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(playerFeetPos + Vector3.down * fullDeepDepth, playerFeetPos + Vector3.down * fullDeepDepth + Vector3.right * 0.4f);
        }
    }
}