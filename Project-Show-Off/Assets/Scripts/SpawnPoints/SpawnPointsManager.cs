using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpawnPointsManager : MonoBehaviour
{
    public List<SpawnPoint> SpawnPoints { get; private set; }
    public List<SpawnPoint> SecondarySpawnPoints { get; private set; }
    public event Action SpawnPointsInitialized;
    
    private void Awake()
    {
        SpawnPoints = GetComponentsInChildren<SpawnPoint>().ToList();
        SecondarySpawnPoints = new List<SpawnPoint>();
        
        foreach (SpawnPoint p in SpawnPoints)
        {
            if (p.GetComponentsInParent<SpawnPointsManager>().Length > 1) SecondarySpawnPoints.Add(p);
        }

        foreach (SpawnPoint p in SecondarySpawnPoints)
        {
            SpawnPoints.Remove(p);
        }
        
        //Debug.Log("Number of spawn points: " + SpawnPoints.Count);
    }

    private void Start()
    {
        SpawnPointsInitialized?.Invoke();
    }
}
