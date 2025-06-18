using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;
using System.Linq;

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
    private const string PARAM_HEAVYNESS = "Heavyness";
    private const string PARAM_MOVEMENT_STATE = "MovementState";

    [Header("Ground Detection Settings")]
    public float raycastDistance = 1.5f;
    public Vector3 raycastOriginOffset = new Vector3(0, 0.5f, 0); // From player pivot
    public LayerMask groundLayerMask;

    [Header("Material Sound Definitions")]
    [Tooltip("List of material names that should use the 'Wood' footstep sound blend. Checks if the detected material name *starts with* any of these strings.")]
    public List<string> woodMaterialNames = new List<string> { "WoodFloor_Shed 2", "New Material", "BridgeVer_1" };


    [Header("Water Detection Settings")]
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

    private struct FootstepSoundBlend
    {
        public float dirt; public float mud; public float wood; public float grass; public float heavyness;
        public FootstepSoundBlend(float d, float m, float w, float g, float h) { dirt = d; mud = m; wood = w; grass = g; heavyness = h; }
    }

    private Dictionary<string, FootstepSoundBlend> materialBlends = new Dictionary<string, FootstepSoundBlend>();
    private FootstepSoundBlend currentGroundBlend;
    private string lastDetectedMaterialKey = "Default";

    private float currentShallowWaterLevel = 0f;
    private float currentDeepWaterLevel = 0f;

    private List<WaterZone> activeWaterZones = new List<WaterZone>();
    private Transform currentActiveWaterSurface = null;
    private WaterZone currentActiveTypedWaterZone = null;

    private PlayerStatus playerStatus;

    void Start()
    {
        playerStatus = GetComponent<PlayerStatus>();
        if (playerStatus == null)
        {
            Debug.LogError("PlayerFootsteps: PlayerStatus component not found on the player!", this);
        }

        if (footstepsEvent.IsNull) { Debug.LogError("PlayerFootsteps: Footsteps Event is not assigned.", this); }

        materialBlends.Add("Wood", new FootstepSoundBlend(d: 0.0f, m: 0.0f, w: 1.0f, g: 0.0f, h: 0.25f));
        materialBlends.Add("GrassyPeat", new FootstepSoundBlend(d: 0.1f, m: 0.3f, w: 0.0f, g: 0.7f, h: 0.25f));
        materialBlends.Add("MossyPeat", new FootstepSoundBlend(d: 0.1f, m: 0.4f, w: 0.0f, g: 0.6f, h: 0.25f));
        materialBlends.Add("Pathway", new FootstepSoundBlend(d: 0.8f, m: 0.2f, w: 0.0f, g: 0.1f, h: 0.25f));
        materialBlends.Add("Peat", new FootstepSoundBlend(d: 0.2f, m: 0.7f, w: 0.0f, g: 0.2f, h: 0.25f));
        materialBlends.Add("Default", new FootstepSoundBlend(d: 0.6f, m: 0.1f, w: 0.0f, g: 0.1f, h: 0.25f));

        currentGroundBlend = materialBlends["Default"];

        if (groundLayerMask == 0) { Debug.LogWarning("PlayerFootsteps: Ground Layer Mask is not set."); }

        if (fullShallowDepth < minDepthForShallowEffect)
        {
            Debug.LogWarning("PlayerFootsteps: Full Shallow Depth should be >= Min Depth for Shallow Effect. Adjusting."); fullShallowDepth = minDepthForShallowEffect;

        }
        if (fullDeepDepth < fullShallowDepth)
        {
            Debug.LogWarning("PlayerFootsteps: Full Deep Depth should be >= Full Shallow Depth. Adjusting."); fullDeepDepth = fullShallowDepth;
        }

        activeWaterZones = new List<WaterZone>();
    }

    void Update()
    {
        RecalculateActiveWaterSurfaceAndZone();
        UpdateWaterLevels();
    }

    void OnTriggerEnter(Collider other)
    {
        WaterZone zone = other.GetComponent<WaterZone>();
        if (zone != null && zone.waterSurfacePlane != null)
        {
            if (!activeWaterZones.Contains(zone))
            {
                activeWaterZones.Add(zone);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        WaterZone zone = other.GetComponent<WaterZone>();
        if (zone != null)
        {
            if (activeWaterZones.Contains(zone))
            {
                activeWaterZones.Remove(zone);
            }
        }
    }

    void RecalculateActiveWaterSurfaceAndZone()
    {
        Transform newActiveSurface = null;
        WaterZone newTypedActiveZone = null;

        float highestSubmergedSurfaceY = float.MinValue;
        float playerFeetY = transform.position.y + playerFeetYOffset;

        activeWaterZones.RemoveAll(zone => zone == null || zone.waterSurfacePlane == null);

        foreach (WaterZone zone in activeWaterZones)
        {
            float candidateSurfaceY = zone.SurfaceYLevel;

            if (playerFeetY < candidateSurfaceY)
            {
                if (candidateSurfaceY > highestSubmergedSurfaceY)
                {
                    highestSubmergedSurfaceY = candidateSurfaceY;
                    newActiveSurface = zone.waterSurfacePlane;
                    newTypedActiveZone = zone;
                }
            }
        }

        currentActiveWaterSurface = newActiveSurface;
        currentActiveTypedWaterZone = newTypedActiveZone;

        if (playerStatus != null)
        {
            if (playerStatus.CurrentWaterZone != currentActiveTypedWaterZone)
            {
                playerStatus.CurrentWaterZone = currentActiveTypedWaterZone;
            }
        }
    }

    void UpdateWaterLevels()
    {
        if (currentActiveWaterSurface == null)
        {
            currentShallowWaterLevel = 0f;
            currentDeepWaterLevel = 0f;
            return;
        }

        float waterSurfaceY = currentActiveWaterSurface.position.y;
        float playerFeetY = transform.position.y + playerFeetYOffset;
        float submersionDepth = waterSurfaceY - playerFeetY;

        if (submersionDepth <= 0)
        {
            currentShallowWaterLevel = 0f;
            currentDeepWaterLevel = 0f;
            return;
        }

        currentShallowWaterLevel = Mathf.Clamp01(Mathf.InverseLerp(minDepthForShallowEffect, fullShallowDepth, submersionDepth));
        currentDeepWaterLevel = Mathf.Clamp01(Mathf.InverseLerp(fullShallowDepth, fullDeepDepth, submersionDepth));
    }

    // --- MODIFIED --- This is the new, ultra-robust detection method.
    // --- MODIFIED --- With a debug line to solve the naming issue.
    void DetectGroundMaterial()
    {
        Vector3 rayOrigin = transform.position + raycastOriginOffset;
        RaycastHit hit;
        string determinedKey = "Default";

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, raycastDistance, groundLayerMask))
        {
            // --- LOGIC FOR TERRAIN ---
            Terrain terrain = hit.collider.GetComponent<Terrain>();
            if (terrain != null)
            {
                // (Terrain logic is unchanged)
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
            // --- LOGIC FOR STANDARD GAMEOBJECTS (like your wood) ---
            else
            {
                Renderer renderer = hit.collider.GetComponent<Renderer>();
                if (renderer == null) { renderer = hit.collider.GetComponentInParent<Renderer>(); }
                if (renderer == null) { renderer = hit.collider.GetComponentInChildren<Renderer>(); }

                if (renderer != null && renderer.sharedMaterial != null)
                {
                    string materialName = renderer.sharedMaterial.name;

                    // --- THIS IS THE DEBUG LINE! ---
                    // It will tell you the exact material name the script is seeing.
                    Debug.Log($"Footsteps checking ground... Raycast hit: {hit.collider.name}, Detected Material Name: '{materialName}'");

                    bool specificMaterialFound = false;

                    foreach (string woodName in woodMaterialNames)
                    {
                        if (!string.IsNullOrEmpty(woodName) && materialName.StartsWith(woodName))
                        {
                            determinedKey = "Wood";
                            specificMaterialFound = true;
                            break;
                        }
                    }

                    if (!specificMaterialFound)
                    {
                        string materialNameLower = materialName.ToLower();
                        foreach (var blendEntry in materialBlends)
                        {
                            if (blendEntry.Key == "Wood" || blendEntry.Key == "Default") continue;
                            if (materialNameLower.Contains(blendEntry.Key.ToLower()))
                            {
                                determinedKey = blendEntry.Key;
                                break;
                            }
                        }
                    }
                }
            }
        }

        // Apply the determined blend
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
        DetectGroundMaterial();
        EventInstance currentFootstepInstance = RuntimeManager.CreateInstance(footstepsEvent);
        currentFootstepInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
        SetEnvironmentParametersForInstance(currentFootstepInstance);
        SetMovementStateParameterForInstance(currentFootstepInstance);
        currentFootstepInstance.start();
        currentFootstepInstance.release();
    }
    private void SetEnvironmentParametersForInstance(EventInstance instance)
    {
        if (!instance.isValid()) { Debug.LogWarning("..."); return; }
        float groundAttenuation = 1f;
        if (currentDeepWaterLevel > 0.01f) { groundAttenuation = 1f - groundSoundReductionInDeep; }
        else if (currentShallowWaterLevel > 0.01f) { groundAttenuation = 1f - groundSoundReductionInShallow; }

        instance.setParameterByName(PARAM_DIRT, currentGroundBlend.dirt * groundAttenuation);
        instance.setParameterByName(PARAM_MUD, currentGroundBlend.mud * groundAttenuation);
        instance.setParameterByName(PARAM_WOOD, currentGroundBlend.wood * groundAttenuation);
        instance.setParameterByName(PARAM_GRASS, currentGroundBlend.grass * groundAttenuation);
        instance.setParameterByName(PARAM_HEAVYNESS, currentGroundBlend.heavyness * groundAttenuation);
        instance.setParameterByName(PARAM_SHALLOW_WATER, currentShallowWaterLevel);
        instance.setParameterByName(PARAM_DEEP_WATER, currentDeepWaterLevel);
    }
    private void SetMovementStateParameterForInstance(EventInstance instance)
    {
        if (instance.isValid()) { instance.setParameterByName(PARAM_MOVEMENT_STATE, currentMovementState); }
    }
    public void SetMovementState(float stateValue) { currentMovementState = stateValue; }

    void OnDrawGizmosSelected()
    {
        Vector3 rayOrigin = transform.position + raycastOriginOffset;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * raycastDistance);

        if (currentActiveWaterSurface != null)
        {
            float waterY = currentActiveWaterSurface.position.y;
            float playerFeetY = transform.position.y + playerFeetYOffset;
            Vector3 playerFeetPos = new Vector3(transform.position.x, playerFeetY, transform.position.z);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(playerFeetPos, new Vector3(playerFeetPos.x, waterY, playerFeetPos.z));

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(playerFeetPos + Vector3.down * minDepthForShallowEffect, playerFeetPos + Vector3.down * minDepthForShallowEffect + Vector3.right * 0.2f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(playerFeetPos + Vector3.down * fullShallowDepth, playerFeetPos + Vector3.down * fullShallowDepth + Vector3.right * 0.3f);
            Gizmos.color = Color.blue; // Darker blue for deep
            Gizmos.DrawLine(playerFeetPos + Vector3.down * fullDeepDepth, playerFeetPos + Vector3.down * fullDeepDepth + Vector3.right * 0.4f);
        }
    }
}