using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class Navigation : MonoBehaviour
{
    [NonSerialized] public SpawnPointsManager spManager;
    private int currentPatrolIndex = 0;
    [SerializeField] private float speed = 5f;
    private float stoppingDistance = 0.1f;

    private Vector3 destination;
    private bool isMoving = false;
    private List<Vector3> navPointsPositions;


    void Awake()
    {
        destination = transform.position;
        spManager = GetComponentInChildren<SpawnPointsManager>();
        spManager.SpawnPointsInitialized += InitNavPoints;
        SetNextPatrolPoint();
    }

    private void InitNavPoints()
    {
        navPointsPositions = new List<Vector3>();
        foreach (SpawnPoint p in spManager.SpawnPoints)
        {
            navPointsPositions.Add(p.transform.position);
        }
    }

    public void SetNextPatrolPoint()
    {
        if (navPointsPositions == null || navPointsPositions.Count == 0) return;

        int oldIndex = currentPatrolIndex;
        int newIndex = oldIndex;

        while (newIndex == oldIndex)
        {
            newIndex = Random.Range(0, navPointsPositions.Count);
        }

        currentPatrolIndex = newIndex;
        SetDestination(navPointsPositions[newIndex]);
    }

    public void Handle()
    { 
        // Move towards the destination
        transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);

        // Check if we've reached the destination
        if (Vector3.Distance(transform.position, destination) <= stoppingDistance)
        {
            isMoving = false;
            OnDestinationReached();
        }  
    }
    
    /// <summary>
    /// Call this method to start moving the entity towards the given destination.
    /// </summary>
    public void SetDestination(Vector3 destination)
    {
        this.destination = destination;
        isMoving = true;
    }
    
    /// <summary>
    /// Override this method to handle logic when the destination is reached.
    /// </summary>
    protected virtual void OnDestinationReached()
    {
        SetNextPatrolPoint();
    }

    private void OnDisable()
    {
        spManager.SpawnPointsInitialized -= InitNavPoints;
    }
}
