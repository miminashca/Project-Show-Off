using UnityEngine;

[RequireComponent(typeof(NixieAI), typeof(NixieNavigation))]
public class NixieStateMachine : StateMachine
{
    // --- Public properties to be accessed by the states ---
    public NixieAI NixieAI { get; private set; }
    public NixieNavigation NixieNav { get; private set; }

    // --- State Instances ---
    public NixieRoamingState RoamingState { get; private set; }
    public NixieStaringState StaringState { get; private set; }
    public NixieChasingState ChasingState { get; private set; }
    public NixieHurtingState HurtingState { get; private set; }
    public NixieStuntedState StuntedState { get; private set; }

    // Use Awake for initialization, similar to your HunterStateMachine
    protected virtual void Awake()
    {
        NixieAI = GetComponent<NixieAI>();
        NixieNav = GetComponent<NixieNavigation>();

        // Initialize all states, passing 'this' (the StateMachine)
        RoamingState = new NixieRoamingState(this);
        StaringState = new NixieStaringState(this);
        ChasingState = new NixieChasingState(this);
        HurtingState = new NixieHurtingState(this);
        StuntedState = new NixieStuntedState(this);
    }

    // Implementation of the abstract property from your base StateMachine
    protected override State InitialState => RoamingState;
}