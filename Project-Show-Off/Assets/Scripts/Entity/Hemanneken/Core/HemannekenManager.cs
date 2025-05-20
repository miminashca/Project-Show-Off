using System;
using UnityEngine;

public class HemannekenManager : MonoBehaviour
{
    private SpawnPointsManager spManager;
    [SerializeField] private HemannekenStateMachine hemannekenPrefab;

    private void Awake()
    {
        spManager = GetComponentInChildren<SpawnPointsManager>();
        if (spManager) spManager.SpawnPointsInitialized += SpawnHemanneken;
    }

    private void SpawnHemanneken()
    {
        foreach (SpawnPoint p in spManager.SpawnPoints)
        {
            HemannekenStateMachine hemanneken = Instantiate(hemannekenPrefab, p.transform.position, Quaternion.identity);
            hemanneken.IsTrueForm = p.isOverWater;
        }
    }

    private void OnDestroy()
    {
        if (spManager) spManager.SpawnPointsInitialized -= SpawnHemanneken;
    }
}
