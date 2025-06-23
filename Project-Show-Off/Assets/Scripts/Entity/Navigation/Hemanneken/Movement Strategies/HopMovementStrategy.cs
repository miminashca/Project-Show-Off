using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class HopMovementStrategy : IMovementStrategy
{
    public event Action OnArrival;

    // Configuration
    private readonly float _hopSpeed;
    private readonly float _hopDistance;
    private readonly float _hopWaitDuration;
    private readonly float _rotationSpeed;
    private readonly float _stoppingDistance;
    private readonly GroundingModule _groundingModule;
    private readonly HemannekenEventBus _eventBus;

    // Internal State
    private Transform _agentTransform;
    private Vector3 _seriesTargetWaypoint;
    private Vector3 _singleHopTargetPosition;
    private float _hopWaitTimer;
    private bool _isCurrentlyMidHop;

    public HopMovementStrategy(HemannekenAIConfig config, GroundingModule grounding, HemannekenEventBus eventBus)
    {
        _hopSpeed = config.hopSpeed;
        _hopDistance = config.hopDistance;
        _hopWaitDuration = config.hopWaitDuration;
        _rotationSpeed = config.rotationSpeed;
        _stoppingDistance = config.stoppingDistance;
        _groundingModule = grounding;
        _eventBus = eventBus;
    }
    public void SetDestination(AgentMovement context, Vector3 destination)
    {
        // Get the transform from the context object.
        _agentTransform = context.transform;
        
        _seriesTargetWaypoint = destination;
        _isCurrentlyMidHop = false;
        _hopWaitTimer = 0.01f; // Start with a tiny wait to kick off the first hop planning.
    }

    public void UpdateMovement(AgentMovement context)
    {
        if (_agentTransform == null) return;
        
        // --- Phase 1: Waiting Between Hops ---
        if (_hopWaitTimer > 0f)
        {
            _hopWaitTimer -= Time.deltaTime;
            RotateTowards(_seriesTargetWaypoint, _rotationSpeed * 2f, context.IsGroundRestricted);

            if (_hopWaitTimer <= 0f)
            {
                if (Vector3.Distance(_agentTransform.position, _seriesTargetWaypoint) <= _stoppingDistance)
                {
                    Arrived();
                }
                else
                {
                    PlanNextHop(context);
                    _isCurrentlyMidHop = true;
                    _eventBus?.RabbitStartHop();
                }
            }
        }
        // --- Phase 2: Actively Moving Mid-Hop ---
        else if (_isCurrentlyMidHop)
        {
            if (MoveTowards(_singleHopTargetPosition, _hopSpeed))
            {
                _agentTransform.position = _singleHopTargetPosition;
                _isCurrentlyMidHop = false;
                _hopWaitTimer = _hopWaitDuration;
                _eventBus?.RabbitEndHop();

                if (Vector3.Distance(_agentTransform.position, _seriesTargetWaypoint) <= _stoppingDistance)
                {
                    Arrived();
                }
            }
            else
            {
                RotateTowards(_singleHopTargetPosition, _rotationSpeed, context.IsGroundRestricted);
            }
        }
    }
    
    private void PlanNextHop(AgentMovement context)
    {
        Vector3 directionToWaypoint = (_seriesTargetWaypoint - _agentTransform.position).normalized;
        float distanceToFinalTarget = Vector3.Distance(_agentTransform.position, _seriesTargetWaypoint);
        
        float hopTravelDistance = Mathf.Min(_hopDistance, distanceToFinalTarget);
        _singleHopTargetPosition = _agentTransform.position + directionToWaypoint * hopTravelDistance;

        if (context.IsGroundRestricted)
        {
            _singleHopTargetPosition = _groundingModule.SnapToGround(_singleHopTargetPosition, _singleHopTargetPosition.y);
        }
    }
    
    private bool MoveTowards(Vector3 target, float speed)
    {
        Vector3 direction = target - _agentTransform.position;
        if (direction.magnitude <= 0.01f) return true;

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
        if (!_isCurrentlyMidHop && _hopWaitTimer <= 0f && OnArrival == null) return; // Prevent multi-calls
        
        _isCurrentlyMidHop = false;
        _hopWaitTimer = 0f;
        OnArrival?.Invoke();
    }
    
    public void Stop()
    {
        _agentTransform = null;
        _isCurrentlyMidHop = false;
        _hopWaitTimer = 0f;
        _seriesTargetWaypoint = Vector3.zero;
    }

    public void DrawGizmos()
    {
        if (_agentTransform == null || _seriesTargetWaypoint == Vector3.zero) return;
        
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(_agentTransform.position, _seriesTargetWaypoint);
        Gizmos.DrawWireSphere(_seriesTargetWaypoint, 0.3f);

        if (_isCurrentlyMidHop)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(_agentTransform.position, _singleHopTargetPosition);
            Gizmos.DrawSphere(_singleHopTargetPosition, 0.15f);
        }
    }
}