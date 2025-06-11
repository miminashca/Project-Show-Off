using UnityEngine;

/// <summary>
/// A generic abstract state machine class.
/// </summary>
public abstract class StateMachine : MonoBehaviour
{
    private State _currentState;

    // --- NEW: Public properties to know the current and previous state ---
    public State CurrentState => _currentState;
    public State PreviousState { get; private set; }

    // Subclasses must override this to supply the Initial State
    protected abstract State InitialState { get; }

    /// <summary>
    /// Called when the object is first enabled. Determines the initial state.
    /// </summary>
    protected virtual void Start()
    {
        // Set the initial previous state to null and transition to the starting state.
        PreviousState = null;
        _currentState = InitialState;
        if (_currentState != null)
        {
            _currentState.OnEnterState();
        }
        else
        {
            Debug.LogError("InitialState is null. State machine cannot start.", this);
        }
    }

    /// <summary>
    /// Unity's Update loop calls the current state's Handle method each frame.
    /// </summary>
    protected virtual void Update()
    {
        // We can safely access _currentState directly because it's set in Start/TransitToState
        _currentState?.Handle();
    }

    /// <summary>
    /// Transitions from the current state to a new one, calling the exit method
    /// on the old state and the enter method on the new state.
    /// </summary>
    /// <param name="newState">The new state to transition to.</param>
    public virtual void TransitToState(State newState)
    {
        if (newState == null || newState == _currentState)
        {
            // Do not transition to a null state or to the same state.
            return;
        }

        _currentState?.OnExitState();

        // --- CORE CHANGE: Update PreviousState before changing CurrentState ---
        PreviousState = _currentState;
        _currentState = newState;

        _currentState.OnEnterState();
    }

    /// <summary>
    /// When this object is destroyed, ensures the current state properly exits
    /// to avoid leaving behind event subscriptions or other references.
    /// </summary>
    protected virtual void OnDestroy()
    {
        _currentState?.OnExitState();
    }
}