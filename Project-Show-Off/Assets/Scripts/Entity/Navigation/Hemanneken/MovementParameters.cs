// File: MovementParameters.cs
using UnityEngine;

/// <summary>
/// A runtime container for an agent's movement settings.
/// This can be created from a HemannekenAIConfig and then modified
/// by states for specific behaviors (e.g., fast fleeing, slow roaming).
/// </summary>
[System.Serializable]
public class MovementParameters
{
    // General Movement
    public float speed;
    public float rotationSpeed;
    public float stoppingDistance;
    
    // Spline Movement
    public float waveAmplitude;
    public float waveFrequency;
    public int wavePathResolution;
    
    // Hop Movement
    public float hopSpeed;
    public float hopDistance;
    public float hopWaitDuration;

    /// <summary>
    /// Creates a new set of parameters by copying values from a config asset.
    /// </summary>
    /// <param name="config">The ScriptableObject template to copy from.</param>
    public MovementParameters(HemannekenAIConfig config)
    {
        // Copy all the default values
        speed = config.defaultSpeed;
        rotationSpeed = config.rotationSpeed;
        stoppingDistance = config.stoppingDistance;

        waveAmplitude = config.waveAmplitude;
        waveFrequency = config.waveFrequency;
        wavePathResolution = config.wavePathResolution;

        hopSpeed = config.hopSpeed;
        hopDistance = config.hopDistance;
        hopWaitDuration = config.hopWaitDuration;
    }
}