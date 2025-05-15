
using System;
using UnityEngine;
using UnityEngine.Serialization;

public class HemannekenStateMachine : StateMachine
{
    [SerializeField, Range(0f, 10f)] private float chaseDistanceRabbit = 3f;
    [SerializeField, Range(0f, 20f)] private float chaseDistanceTrue = 10f;
    [SerializeField, Range(0f, 100f)] private float investigateDistance = 50f;
    [SerializeField] private GameObject hemannekenTrueModel;
    [SerializeField] private GameObject hemannekenRabbitModel;

    [NonSerialized] public bool IsTrueForm;
    private Transform playerTransform;
    protected override State InitialState => new HemannekenRoamingState(this); // this is the initial state for Hemanneken SM
    
    protected override void Start()
    {
        playerTransform = FindFirstObjectByType<PlayerMovement>().transform;
        if(IsTrueForm)EnableTrueFormMesh();
        else EnableRabbitFormMesh();
        base.Start();
    }

    public float GetDistanceToPlayer()
    {
        return Vector3.Magnitude(gameObject.transform.position - playerTransform.position);
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

    public void EnableTrueFormMesh()
    {
        Instantiate(hemannekenTrueModel, this.gameObject.transform);
    }
    public void EnableRabbitFormMesh()
    {
        Instantiate(hemannekenRabbitModel, this.gameObject.transform);
    }

}
