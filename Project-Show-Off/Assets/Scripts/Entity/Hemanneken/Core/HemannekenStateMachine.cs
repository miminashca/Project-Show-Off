
using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class HemannekenStateMachine : StateMachine
{
    [SerializeField, Range(0f, 10f)] private float chaseDistanceRabbit = 3f;
    [SerializeField, Range(0f, 20f)] private float chaseDistanceTrue = 10f;
    [SerializeField, Range(0f, 100f)] private float investigateDistance = 50f;
    [SerializeField, Range(0f, 100f)] private float attachDistance = 1f;
    [SerializeField] private GameObject hemannekenTrueModel;
    [SerializeField] private GameObject hemannekenRabbitModel;
   
    [NonSerialized] public AiNavigation aiNav;
    [NonSerialized] public Navigation nav;
    [NonSerialized] public bool IsTrueForm;
    private Transform playerTransform;
    protected override State InitialState => new HemannekenRoamingState(this); // this is the initial state for Hemanneken SM
    
    protected override void Start()
    {
        aiNav = GetComponent<AiNavigation>();
        nav = GetComponent<Navigation>();
        
        playerTransform = FindFirstObjectByType<PlayerMovement>().transform;
        if (hemannekenRabbitModel && hemannekenTrueModel)
        {
            if(IsTrueForm)EnableTrueFormMesh();
            else EnableRabbitFormMesh();
        }
        base.Start();
    }

    public float GetDistanceToPlayer()
    {
        Vector3 posA = gameObject.transform.position;
        posA.y = 0;
        Vector3 posB = playerTransform.position;
        posB.y = 0;
        
        return Vector3.Magnitude(posA - posB);
    }
    public Vector3 GetPlayerPosition()
    {
        return playerTransform.position;
    }
    public bool PlayerIsInRabbitChaseDistance()
    {
        return GetDistanceToPlayer() <= chaseDistanceRabbit;
    }
    public bool PlayerIsInTrueChaseDistance()
    {
        return GetDistanceToPlayer() <= chaseDistanceTrue;
    }
    public bool PlayerIsInInvestigateDistance()
    {
        return GetDistanceToPlayer() <= investigateDistance;
    }
    public bool PlayerIsInAttachingDistance()
    {
        return GetDistanceToPlayer() <= attachDistance;
    }

    public void EnableTrueFormMesh()
    {
        Instantiate(hemannekenTrueModel, this.gameObject.transform);
    }
    public void EnableRabbitFormMesh()
    {
        Instantiate(hemannekenRabbitModel, this.gameObject.transform);
    }
    public void LockNavMeshAgent(bool Lock)
    {
        if(!aiNav) return;
        
        if (Lock)
        {
            // aiNav.navAgent.updatePosition = false;
            // aiNav.navAgent.updateRotation = false;
            aiNav.navAgent.enabled = false;
        }
        else
        {
            // aiNav.navAgent.updatePosition = true;
            // aiNav.navAgent.updateRotation = true;
            aiNav.navAgent.enabled = true;
        }
        
    }

}
