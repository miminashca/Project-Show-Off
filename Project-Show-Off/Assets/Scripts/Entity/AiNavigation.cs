using System;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class AiNavigation : MonoBehaviour
{
    [NonSerialized] public NavMeshAgent navAgent;
    [SerializeField, Range(1f, 20f)] private float speed = 5f;

    void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        navAgent.speed = speed;
    }

    public void SetDestination(Vector3 destination)
    {
        navAgent.SetDestination(destination);
    }
    
}
