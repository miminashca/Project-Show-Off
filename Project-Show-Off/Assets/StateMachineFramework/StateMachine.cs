using UnityEngine;

/// <summary>
/// A generic abstract state machine class. To be extended by subclasses.
/// </summary>
public abstract class StateMachine : MonoBehaviour
{
    private State currentState;

    /// <summary>
    /// Called when the object is first enabled. Determines the initial state.
    /// </summary>
    protected virtual void Start()
    {
        TransitToState(new IdleState(this));
    }

    /// <summary>
    /// Unity's Update loop calls the current state's Handle method each frame.
    /// </summary>
    protected virtual void Update()
    {
        if (currentState != null)
        {
            currentState.Handle();
        }
    }

    /// <summary>
    /// Transitions from the current state to a new one, calling the exit method 
    /// on the old state and the enter method on the new state.
    /// </summary>
    /// <param name="pState">The new state to transition to.</param>
    public virtual void TransitToState(State pState)
    {
        if (currentState != null)
        {
            currentState.OnExitState();
        }
        currentState = pState;
        currentState.OnEnterState();
    }

    /// <summary>
    /// When this object is destroyed, ensures the current state properly exits 
    /// to avoid leaving behind event subscriptions or other references.
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (currentState != null)
        {
            currentState.OnExitState();
        }
    }
}
