using UnityEngine;

[RequireComponent(typeof(HunterAI))]
public class HunterStateMachine : StateMachine
{
    public HunterAI HunterAI { get; private set; }

    public HunterRoamingState RoamingState { get; private set; }
    public HunterInvestigatingState InvestigatingState { get; private set; }
    public HunterChasingState ChasingState { get; private set; }
    public HunterAimingState AimingState { get; private set; }
    public HunterSuppressingState SuppressingState { get; private set; }
    public HunterShootingState ShootingState { get; private set; }

    protected virtual void Awake()
    {
        HunterAI = GetComponent<HunterAI>();
        if (HunterAI == null)
        {
            Debug.LogError("HunterStateMachine requires a ThimbleHunterAI component on the same GameObject!", this);
            enabled = false;
            return;
        }

        // Initialize all states for Phase II: Active Hunter

        RoamingState = new HunterRoamingState(this);
        InvestigatingState = new HunterInvestigatingState(this);
        ChasingState = new HunterChasingState(this);
        AimingState = new HunterAimingState(this);
        SuppressingState = new HunterSuppressingState(this);
        ShootingState = new HunterShootingState(this);
    }

    // The InitialState is now ALWAYS the RoamingState.
    // When this GameObject is enabled, it's hostile.
    protected override State InitialState
    {
        get { return RoamingState; }
    }
}