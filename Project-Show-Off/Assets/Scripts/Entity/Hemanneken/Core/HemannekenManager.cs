using UnityEngine;

public class HemannekenManager : MonoBehaviour
{
    private SpawnPointsManager spManager;
    [SerializeField] private HemannekenStateMachine hemannekenPrefab; // Prefab should have PlayerSensor, AgentMovement, HemannekenVisuals components

    private void Awake()
    {
        spManager = GetComponentInChildren<SpawnPointsManager>();
        if (spManager) 
        {
            // If already initialized (e.g. if manager starts after spawn points)
            if (spManager.SpawnPoints != null && spManager.SpawnPoints.Count > 0)
            {
                SpawnHemanneken();
            }
        }
        else
        {
            Debug.LogError("SpawnPointsManager not found in children of HemannekenManager.", this);
        }
    }

    private void SpawnHemanneken()
    {
        if (hemannekenPrefab == null)
        {
            Debug.LogError("HemannekenPrefab is not assigned in HemannekenManager.", this);
            return;
        }
        if (spManager == null || spManager.SpawnPoints == null)
        {
            Debug.LogError("Cannot spawn Hemanneken, SpawnPointsManager or its points are null.", this);
            return;
        }

        foreach (SpawnPoint p in spManager.SpawnPoints)
        {
            HemannekenStateMachine hemanneken = Instantiate(hemannekenPrefab, p.transform.position, Quaternion.identity);
            hemanneken.IsInitiallyTrueForm = p.isOverWater;
            // The HemannekenStateMachine's Awake will handle setting the form via its Visuals component
        }
    }

    
}