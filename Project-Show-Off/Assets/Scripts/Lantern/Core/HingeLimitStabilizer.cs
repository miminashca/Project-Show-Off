using UnityEngine;

// Add this script to the same GameObject that has the HingeJoint.
[RequireComponent(typeof(HingeJoint))]
public class HingeLimitStabilizer : MonoBehaviour
{
    private HingeJoint hinge;
    private bool configured = false;
    private Quaternion initialLocalRotation;

    void Awake()
    {
        // Cache the HingeJoint component.
        hinge = GetComponent<HingeJoint>();

        // IMPORTANT: Store the original local rotation of the object.
        // This is the "zero" orientation that the hinge limits should be relative to.
        initialLocalRotation = transform.localRotation;
        configured = true;
    }

    public void ResetHinge()
    {
        // When this component (and thus the GameObject) is enabled,
        // the HingeJoint will also be re-initializing.
        // We must reset the local rotation BEFORE the physics engine
        // has a chance to update and read the transform for the joint.
        // OnEnable is the perfect place for this.
        if(configured) transform.localRotation = initialLocalRotation;
    }

    // // Optional: If you are only disabling/enabling the HingeJoint component
    // // itself, and not the whole GameObject, you would need a public method
    // // to call instead of relying on OnEnable.
    // public void ResetAndReEnableHinge()
    // {
    //     // Disable first to ensure a clean state
    //     hinge.enabled = false;
    //
    //     // Reset the rotation to its original "zero" state
    //     transform.localRotation = initialLocalRotation;
    //
    //     // Re-enable the joint. It will now initialize with the correct reference rotation.
    //     hinge.enabled = true;
    // }
}