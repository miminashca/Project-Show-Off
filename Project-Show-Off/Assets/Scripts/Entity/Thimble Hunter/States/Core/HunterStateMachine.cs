using UnityEngine;

[RequireComponent(typeof(HunterAI))]
public class HunterStateMachine : StateMachine
{
    public HunterAI HunterAI { get; private set; }

    public HunterRoamingState RoamingState { get; private set; }
    public HunterInvestigatingState InvestigatingState { get; private set; }
    public HunterChasingState ChasingState { get; private set; }
    public HunterAimingState AimingState { get; private set; }
    public HunterShootingState ShootingState { get; private set; }
    public HunterCloseKillingState CloseKillingState { get; private set; }

    protected virtual void Awake()
    {
        HunterAI = GetComponent<HunterAI>();
        if (HunterAI == null)
        {
            Debug.LogError("ThimbleHunterStateMachine requires a ThimbleHunterAI component on the same GameObject!", this);
            enabled = false;
            return;
        }

        // Initialize all states, passing 'this' (the StateMachine)
        RoamingState = new HunterRoamingState(this);
        InvestigatingState = new HunterInvestigatingState(this);
        ChasingState = new HunterChasingState(this);
        AimingState = new HunterAimingState(this);
        ShootingState = new HunterShootingState(this);
        CloseKillingState = new HunterCloseKillingState(this);
    }

    // Implementation of the abstract property from your base StateMachine
    protected override State InitialState
    {
        get
        {
            if (RoamingState == null)
            {
                Debug.LogError("RoamingState not initialized when InitialState was accessed!", this);
                // Fallback or force re-initialization if critical
                Awake();
            }
            return RoamingState;
        }
    }
}