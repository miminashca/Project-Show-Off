using UnityEngine;
using System.Collections;

public class ProximityLight : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("The light component to control.")]
    [SerializeField] private Light controlledLight;
    [Tooltip("The player's transform. If null, will search for a 'Player' tag.")]
    [SerializeField] private Transform playerTransform;

    [Header("Settings")]
    [Tooltip("The maximum distance at which the light starts to appear.")]
    [SerializeField] private float detectionRadius = 5f;
    [Tooltip("The intensity of the light when the player is very close.")]
    [SerializeField] private float maxIntensity = 2f;
    [Tooltip("How quickly the light fades in and out.")]
    [SerializeField] private float transitionSpeed = 3f;

    private float _targetIntensity = 0f;
    private bool _isEnabled = true;

    private void Awake()
    {
        if (controlledLight == null)
        {
            controlledLight = GetComponent<Light>();
        }

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogError("ProximityLight: Could not find a GameObject with the 'Player' tag. Disabling component.", this);
                _isEnabled = false;
            }
        }

        if (controlledLight != null)
        {
            controlledLight.intensity = 0; // Start with the light off
        }
    }

    private void Update()
    {
        if (!_isEnabled || controlledLight == null || playerTransform == null)
        {
            // If disabled, ensure light smoothly fades out
            _targetIntensity = 0f;
        }
        else
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);

            if (distance < detectionRadius)
            {
                // Calculate intensity based on distance (brighter when closer)
                // Using 1 - (distance / radius) creates a linear falloff
                _targetIntensity = Mathf.Lerp(maxIntensity, 0, distance / detectionRadius);
            }
            else
            {
                _targetIntensity = 0f;
            }
        }

        // Smoothly transition the light's current intensity to the target intensity
        controlledLight.intensity = Mathf.Lerp(controlledLight.intensity, _targetIntensity, Time.deltaTime * transitionSpeed);
    }

    /// <summary>
    /// Enables or disables the proximity light logic.
    /// </summary>
    public void SetEnabled(bool state)
    {
        _isEnabled = state;
        // If we are disabling it, the Update loop will automatically fade it out.
    }
}