// Create a new C# script named WaterZone.cs
using UnityEngine;

public class WaterZone : MonoBehaviour
{
    [Tooltip("Assign the Transform of the actual water surface plane for this zone.")]
    public Transform waterSurfacePlane; // e.g., The "WaterSurface" child of your "WaterBlock"

    void Awake()
    {
        if (waterSurfacePlane == null)
        {
            Debug.LogError("WaterZone on " + gameObject.name + " needs its 'Water Surface Plane' assigned in the Inspector!", this);
        }

        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning("WaterZone on " + gameObject.name + " is missing a Collider component. It won't be able to detect the player.", this);
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning("WaterZone on " + gameObject.name + "'s Collider is not set to 'Is Trigger'. Player detection might not work as expected.", this);
        }
    }
}