using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SplineMovementStrategy : IMovementStrategy
{
    public event Action OnArrival;

    private readonly float _speed, _rotationSpeed, _stoppingDistance, _waveAmplitude, _waveFrequency;
    private readonly int _wavePathResolution;
    private readonly GroundingModule _groundingModule;

    private Transform _agentTransform;
    private List<Vector3> _pathPoints = new List<Vector3>();
    private int _currentSegmentIndex;
    private float _segmentProgress;
    private bool _isMoving;

    public SplineMovementStrategy(HemannekenAIConfig config, GroundingModule grounding)
    {
        _speed = config.defaultSpeed;
        _rotationSpeed = config.rotationSpeed;
        _stoppingDistance = config.stoppingDistance;
        _waveAmplitude = config.waveAmplitude;
        _waveFrequency = config.waveFrequency;
        _wavePathResolution = config.wavePathResolution;
        _groundingModule = grounding;
    }

    // Correctly implements the interface signature
    public void SetDestination(AgentMovement context, Vector3 destination)
    {
        _agentTransform = context.transform;
        GenerateWavePath(context, destination);

        if (_pathPoints.Count >= 4)
        {
            _currentSegmentIndex = 1;
            _segmentProgress = 0f;
            _isMoving = true;
        }
        else
        {
            Arrived();
        }
    }

    // Correctly implements the interface signature
    public void UpdateMovement(AgentMovement context)
    {
        if (!_isMoving || _agentTransform == null) return;
        
        float distanceThisFrame = _speed * Time.deltaTime;
        float segmentLength = Vector3.Distance(_pathPoints[_currentSegmentIndex], _pathPoints[_currentSegmentIndex + 1]);
        if (segmentLength > 0.01f)
        {
            _segmentProgress += distanceThisFrame / segmentLength;
        }

        while (_segmentProgress >= 1.0f)
        {
            _segmentProgress -= 1.0f;
            _currentSegmentIndex++;
            if (_currentSegmentIndex >= _pathPoints.Count - 2)
            {
                Arrived();
                return;
            }
        }
        
        Vector3 targetPosition = GetPointOnSpline();
        _agentTransform.position = Vector3.MoveTowards(_agentTransform.position, targetPosition, _speed * Time.deltaTime * 1.5f);
        RotateTowards(targetPosition, _rotationSpeed, context.IsGroundRestricted);

        if (Vector3.Distance(_agentTransform.position, _pathPoints.Last()) < _stoppingDistance)
        {
            Arrived();
        }
    }

    private void GenerateWavePath(AgentMovement context, Vector3 end)
    {
        _pathPoints.Clear();
        Vector3 start = context.transform.position;
        Vector3 pathDirection = end - start;
        float pathDistance = pathDirection.magnitude;
        Vector3 pathNormal = pathDirection.normalized;

        _pathPoints.Add(start);
        _pathPoints.Add(start);

        if (pathDistance > _stoppingDistance && _waveAmplitude > 0.01f && _wavePathResolution > 1)
        {
            Vector3 perpendicular = Vector3.Cross(pathNormal, Vector3.up).normalized;
            for (int i = 1; i < _wavePathResolution; i++)
            {
                float t = (float)i / _wavePathResolution;
                Vector3 pointOnLine = start + pathNormal * (t * pathDistance);
                float sineOffset = Mathf.Sin(t * _waveFrequency * Mathf.PI) * _waveAmplitude;
                Vector3 wavePoint = pointOnLine + perpendicular * sineOffset;

                if (context.IsGroundRestricted)
                {
                    float referenceY = Mathf.Lerp(start.y, end.y, t);
                    wavePoint = _groundingModule.SnapToGround(wavePoint, referenceY);
                }
                else
                {
                    wavePoint.y = Mathf.Lerp(start.y, end.y, t);
                }
                _pathPoints.Add(wavePoint);
            }
        }
        
        _pathPoints.Add(end);
        _pathPoints.Add(end);
    }
    
    private void RotateTowards(Vector3 target, float rotationSpeed, bool isGroundRestricted)
    {
        Vector3 direction = target - _agentTransform.position;
        if (isGroundRestricted) direction.y = 0;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            _agentTransform.rotation = Quaternion.RotateTowards(_agentTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private Vector3 GetPointOnSpline()
    {
        if (_currentSegmentIndex < 1 || _currentSegmentIndex + 1 >= _pathPoints.Count) return _agentTransform.position;
        Vector3 p0 = _pathPoints[_currentSegmentIndex - 1];
        Vector3 p1 = _pathPoints[_currentSegmentIndex];
        Vector3 p2 = _pathPoints[_currentSegmentIndex + 1];
        Vector3 p3 = (_currentSegmentIndex + 2 >= _pathPoints.Count) ? p2 : _pathPoints[_currentSegmentIndex + 2];
        return GetCatmullRomPosition(_segmentProgress, p0, p1, p2, p3);
    }
    
    private void Arrived()
    {
        if (!_isMoving) return;
        _isMoving = false;
        if (_agentTransform != null && _pathPoints.Any())
        {
            _agentTransform.position = _pathPoints.Last();
        }
        OnArrival?.Invoke();
    }

    public void Stop()
    {
        _isMoving = false;
        _agentTransform = null;
        _pathPoints.Clear();
    }

    public void DrawGizmos()
    {
        if (_pathPoints.Count < 4) return;
        
        Gizmos.color = Color.cyan;
        for (int i = 0; i < _pathPoints.Count - 1; i++)
        {
            Gizmos.DrawLine(_pathPoints[i], _pathPoints[i + 1]);
            Gizmos.DrawSphere(_pathPoints[i], 0.05f);
        }
        Gizmos.DrawSphere(_pathPoints.Last(), 0.05f);

        Gizmos.color = Color.magenta;
        for (int i = 1; i < _pathPoints.Count - 2; i++)
        {
            Vector3 p0 = _pathPoints[i - 1];
            Vector3 p1 = _pathPoints[i];
            Vector3 p2 = _pathPoints[i + 1];
            Vector3 p3 = (_pathPoints.Count > i + 2) ? _pathPoints[i + 2] : p2;
            Vector3 lastPoint = p1;
            for (int j = 1; j <= 20; j++)
            {
                float t = j / 20f;
                Vector3 newPoint = GetCatmullRomPosition(t, p0, p1, p2, p3);
                Gizmos.DrawLine(lastPoint, newPoint);
                lastPoint = newPoint;
            }
        }
    }

    private static Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }
}