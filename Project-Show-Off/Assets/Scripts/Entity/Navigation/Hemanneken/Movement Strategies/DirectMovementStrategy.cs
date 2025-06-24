using System;
using UnityEngine;

public class DirectMovementStrategy : IMovementStrategy
{
    public event Action OnArrival;

    // Internal state variables, not parameters.
    private Transform _agentTransform;
    private Vector3 _targetPosition;
    private bool _isMoving;

    // 1. The constructor is now parameterless. It doesn't store any config data.
    public DirectMovementStrategy() { }

    public void SetDestination(AgentMovement context, Vector3 destination)
    {
        _agentTransform = context.transform;
        _targetPosition = destination;

        // Use the live stopping distance from the context's current parameters.
        _isMoving = Vector3.Distance(_agentTransform.position, _targetPosition) > context.CurrentParameters.stoppingDistance;
    }

    public void UpdateMovement(AgentMovement context)
    {
        if (!_isMoving || _agentTransform == null) return;
        
        // 2. Get the LIVE parameters from the context every frame.
        MovementParameters p = context.CurrentParameters;

        // 3. Use the live parameters for movement calculations.
        if (MoveTowards(_targetPosition, p.speed, p.stoppingDistance))
        {
            Arrived();
            return;
        }
        
        RotateTowards(_targetPosition, p.rotationSpeed, context.IsGroundRestricted);
    }

    private bool MoveTowards(Vector3 target, float speed, float stoppingDistance)
    {
        Vector3 direction = target - _agentTransform.position;
        if (direction.magnitude <= stoppingDistance) return true;
        
        Vector3 movement = direction.normalized * speed * Time.deltaTime;
        if (movement.magnitude >= direction.magnitude)
        {
            _agentTransform.position = target;
            return true;
        }
        
        _agentTransform.position += movement;
        return false;
    }

    private void RotateTowards(Vector3 target, float rotationSpeed, bool isGroundRestricted)
    {
        Vector3 direction = target - _agentTransform.position;
        if (isGroundRestricted)
        {
            direction.y = 0;
        }

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            _agentTransform.rotation = Quaternion.RotateTowards(_agentTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void Arrived()
    {
        if (!_isMoving) return;
        _isMoving = false;
        if (_agentTransform != null)
        {
            _agentTransform.position = _targetPosition;
        }
        OnArrival?.Invoke();
    }

    public void Stop()
    {
        _isMoving = false;
        _agentTransform = null;
    }

    public void DrawGizmos()
    {
        if (_agentTransform != null && _isMoving)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(_agentTransform.position, _targetPosition);
            Gizmos.DrawWireSphere(_targetPosition, 0.2f);
        }
    }
}