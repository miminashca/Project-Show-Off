using UnityEngine;

[RequireComponent(typeof(ThimbleHunterAI))]
public class ThimbleHunterStateMachine : StateMachine // Inherits from your abstract StateMachine
{
    // Public property to allow states to access the ThimbleHunterAI component
    public ThimbleHunterAI HunterAI { get; private set; }

    // --- State Instances ---
    // Make these public if you want to access them from outside for specific reasons,
    // otherwise private is fine.
    public ThimbleHunterRoamingState RoamingState { get; private set; }
    public ThimbleHunterInvestigatingState InvestigatingState { get; private set; }
    public ThimbleHunterChasingState ChasingState { get; private set; }
    public ThimbleHunterAimingState AimingState { get; private set; }
    public ThimbleHunterShootingState ShootingState { get; private set; }
    // public ThimbleHunterCloseKillingState CloseKillingState { get; private set; }

    // Awake is called before Start
    protected virtual void Awake()
    {
        HunterAI = GetComponent<ThimbleHunterAI>();
        if (HunterAI == null)
        {
            Debug.LogError("ThimbleHunterStateMachine requires a ThimbleHunterAI component on the same GameObject!", this);
            enabled = false; // Disable if AI component is missing
            return;
        }

        // Initialize all states, passing 'this' (the StateMachine)
        RoamingState = new ThimbleHunterRoamingState(this);
        InvestigatingState = new ThimbleHunterInvestigatingState(this);
        ChasingState = new ThimbleHunterChasingState(this);
        AimingState = new ThimbleHunterAimingState(this);
        ShootingState = new ThimbleHunterShootingState(this);
        // CloseKillingState = new ThimbleHunterCloseKillingState(this);

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