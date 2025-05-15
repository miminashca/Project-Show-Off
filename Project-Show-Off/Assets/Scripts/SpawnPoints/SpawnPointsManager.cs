using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpawnPointsManager : MonoBehaviour
{
    public List<SpawnPoint> SpawnPoints { get; private set; }
    public event Action SpawnPointsInitialized;
    
    private void Awake()
    {
        SpawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).ToList();
        Debug.Log("Number of spawn points: " + SpawnPoints.Count);
    }

    private void Start()
    {
        SpawnPointsInitialized?.Invoke();
    }
}
