using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class VisualEffectDistanceToggle : MonoBehaviour
{
    [Header("Target Camera")]
    [Tooltip("Players camera tramsform")]
    public Transform target;

    [Header("Settings")]
    [Tooltip("Distance for turned fireflies off")]
    public float disableDistance = 50f;

    VisualEffect vfx;
    float sqrThreshold;

    void Awake()
    {
        
        vfx = GetComponent<VisualEffect>();
        if (target == null)
            

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
