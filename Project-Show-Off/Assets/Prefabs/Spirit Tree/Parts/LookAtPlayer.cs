using UnityEngine;

public class EyeLookAtPlayer : MonoBehaviour
{
    public Transform player; // Assign your player's transform in the Inspector
    public float rotationSpeed = 5f; // Adjust for smoother or snappier rotation
    public float maxLookAngle = 45f; // Limits how far the eyes can rotate

    private Quaternion initialRotation; // Store the initial rotation of the eye

    void Start()
    {
        // Store the initial rotation of the eye to limit its movement later
        initialRotation = transform.localRotation;

        if (player == null)
        {
            Debug.LogError("Player Transform not assigned to EyeLookAtPlayer script on " + gameObject.name);
            // Optionally try to find the player if not assigned
            GameObject foundPlayer = GameObject.FindWithTag("Player"); // Or by name if you prefer
            if (foundPlayer != null)
            {
                player = foundPlayer.transform;
            }
            else
            {
                Debug.LogError("Could not find player GameObject. Please assign it manually or tag your player with 'Player'.");
                this.enabled = false; // Disable the script if no player is found
            }
        }
    }

    void Update()
    {
        if (player != null)
        {
            // Calculate the direction from the eye to the player
            Vector3 directionToPlayer = player.position - transform.position;

            // Calculate the target rotation to look at the player
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);

            // Convert the target rotation to local space
            // This is important if your eyes are children of a parent object that also rotates
            targetRotation = Quaternion.Inverse(transform.parent.rotation) * targetRotation;


            // Apply rotation limits based on the initial rotation
            // We'll calculate the difference from the initial rotation and clamp it
            Quaternion deltaRotation = targetRotation * Quaternion.Inverse(initialRotation);
            Vector3 eulerAngles = deltaRotation.eulerAngles;

            // Normalize angles to be between -180 and 180 for easier clamping
            eulerAngles.x = NormalizeAngle(eulerAngles.x);
            eulerAngles.y = NormalizeAngle(eulerAngles.y);
            eulerAngles.z = NormalizeAngle(eulerAngles.z);

            // Clamp the angles
            eulerAngles.x = Mathf.Clamp(eulerAngles.x, -maxLookAngle, maxLookAngle);
            eulerAngles.y = Mathf.Clamp(eulerAngles.y, -maxLookAngle, maxLookAngle);
            // You might not want to clamp Z (roll) for eyes, or set it to a very small range
            eulerAngles.z = 0; // Or clamp based on your needs

            // Reconstruct the clamped rotation relative to the initial rotation
            Quaternion clampedLocalRotation = initialRotation * Quaternion.Euler(eulerAngles);


            // Smoothly interpolate between the current rotation and the target rotation
            transform.localRotation = Quaternion.Slerp(transform.localRotation, clampedLocalRotation, Time.deltaTime * rotationSpeed);
        }
    }

    // Helper function to normalize angles to -180 to 180
    float NormalizeAngle(float angle)
    {
        while (angle > 180) angle -= 360;
        while (angle < -180) angle += 360;
        return angle;
    }
}
