/// <summary>
/// The abstract base class for all states within the StateMachine. 
/// Derived classes must implement OnEnterState, Handle, and OnExitState to define 
/// specific behaviors when entering, actively handling logic, and exiting a state.
/// </summary>
public abstract class State
{
    /// <summary>
    /// A reference to the StateMachine that controls the transitions between states.
    /// </summary>
    protected StateMachine SM;

    /// <summary>
    /// Initializes the state with a reference to the controlling StateMachine.
    /// </summary>
    /// <param name="pSM">The StateMachine overseeing state transitions.</param>
    public State(StateMachine pSM)
    {
        SM = pSM;
    }

    /// <summary>
    /// Called once when the state is first entered. 
    /// Implementations should handle any setup or subscription logic here.
    /// </summary>
    public abstract void OnEnterState();

    /// <summary>
    /// Called every frame while the state is active. 
    /// Implementations should contain the state's ongoing logic.
    /// </summary>
    public abstract void Handle();

    /// <summary>
    /// Called once when the state is exited. 
    /// Implementations should handle any cleanup or unsubscription logic here.
    /// </summary>
    public abstract void OnExitState();
}