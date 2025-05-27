using UnityEngine;

[RequireComponent(typeof(HunterAI))]
public class HunterStateMachine : StateMachine // Inherits from your abstract StateMachine
{
    // Public property to allow states to access the ThimbleHunterAI component
    public HunterAI HunterAI { get; private set; }

    // --- State Instances ---
    // Make these public if you want to access them from outside for specific reasons,
    // otherwise private is fine.
    public HunterRoamingState RoamingState { get; private set; }
    public HunterInvestigatingState InvestigatingState { get; private set; }
    public HunterChasingState ChasingState { get; private set; }
    public HunterAimingState AimingState { get; private set; }
    public HunterShootingState ShootingState { get; private set; }
    // public HunterCloseKillingState CloseKillingState { get; private set; }

    // Awake is called before Start
    protected virtual void Awake()
    {
        HunterAI = GetComponent<HunterAI>();
        if (HunterAI == null)
        {
            Debug.LogError("ThimbleHunterStateMachine requires a ThimbleHunterAI component on the same GameObject!", this);
            enabled = false; // Disable if AI component is missing
            return;
        }

        // Initialize all states, passing 'this' (the StateMachine)
        RoamingState = new HunterRoamingState(this);
        InvestigatingState = new HunterInvestigatingState(this);
        ChasingState = new HunterChasingState(this);
        AimingState = new HunterAimingState(this);
        ShootingState = new HunterShootingState(this);
        // CloseKillingState = new HunterCloseKillingState(this);

        // Note: The base StateMachine's Start() method will call InitStartState()
        // and then TransitToState(initialState).
    }

    // Implementation of the abstract property from your base StateMachine
    protected override State InitialState
    {
        get
        {
            // Ensure states are initialized before accessing
            if (RoamingState == null)
            {
                // This case should ideally not happen if Awake runs correctly.
                // Consider re-initializing states here if necessary, or log an error.
                Debug.LogError("RoamingState not initialized when InitialState was accessed!", this);
                // Fallback or force re-initialization if critical
                Awake(); // Re-run Awake to ensure states are created (use with caution)
            }
            return RoamingState;
        }
    }

    // The base StateMachine's Start() and Update() will handle the core loop.
    // Start() calls InitStartState() then TransitToState(initialState)
    // Update() calls currentState.Handle()
}