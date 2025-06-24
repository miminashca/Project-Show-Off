using System;
using UnityEngine;

/// <summary>
/// Defines the contract for a movement strategy. Each implementation
/// handles a specific way for the agent to move from one point to another.
/// </summary>
public interface IMovementStrategy
{
    /// <summary>
    /// Fired when the agent has successfully arrived at its final destination.
    /// </summary>
    event Action OnArrival;

    /// <summary>
    /// Initializes the strategy and sets the final destination for the agent.
    /// </summary>
    /// <param name="agentTransform">The transform of the agent to be moved.</param>
    /// <param name="destination">The final target position.</param>
    void SetDestination(AgentMovement context, Vector3 destination);

    /// <summary>
    /// Called every frame by AgentMovement to update the agent's position and rotation.
    /// </summary>
    void UpdateMovement(AgentMovement context);

    /// <summary>
    /// Immediately stops any current movement and resets the strategy's internal state.
    /// </summary>
    void Stop();

    /// <summary>
    /// Draws debug gizmos specific to this strategy in the editor.
    /// </summary>
    void DrawGizmos();
}