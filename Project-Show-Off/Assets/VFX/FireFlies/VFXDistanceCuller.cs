using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class VisualEffectDistanceToggle : MonoBehaviour
{
    [Header("Target Camera")]
    [Tooltip("Player's camera transform")]
    public Transform target;

    [Header("Settings")]
    [Tooltip("Distance at which the VFX is turned off")]
    public float disableDistance = 50f;

    VisualEffect vfx;
    float sqrThreshold;

    void Awake()
    {
        vfx = GetComponent<VisualEffect>();

        if (target == null)
        {
            Debug.LogWarning("VFXDistanceToggle: No target camera assigned on " + gameObject.name);
            enabled = false;
            return;
        }

        sqrThreshold = disableDistance * disableDistance;
    }

    void Update()
    {
        if (target == null) return;

        float sqrDist = (transform.position - target.position).sqrMagnitude;
        bool shouldBeEnabled = sqrDist <= sqrThreshold;

        if (vfx.enabled != shouldBeEnabled)
            vfx.enabled = shouldBeEnabled;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, disableDistance);
    }
}
