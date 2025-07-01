using System;
using NUnit.Framework;
using UnityEngine;

public class GazeSystem : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [Tooltip("The layer mask for objects that can block the player's view of the White Lady.")]
    [SerializeField] private LayerMask occlusionLayers;

    private Renderer targetRenderer;
    private Plane[] cameraFrustumPlanes;

    public bool IsTargetVisible { get; private set; }
    private bool IsTargetCurrentlyVisible = false;

    public event Action<bool> PlayerCaughtSightOfLady;

    private void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogError("Player_GazeSystem could not find a Camera!", this);
                enabled = false;
            }
        }
    }

    public void SetTarget(Renderer newTarget)
    {
        targetRenderer = newTarget;
    }

    public void ClearTarget()
    {
        targetRenderer = null;
        IsTargetVisible = false;
    }

    private void Update()
    {
        if (targetRenderer == null || !targetRenderer.enabled)
        {
            IsTargetVisible = false;
            return;
        }

        // Req 3.1.3: Perform the two visibility checks
        IsTargetVisible = IsInFrustum() && IsNotOccluded();
        if (IsTargetCurrentlyVisible != IsTargetVisible)
        {
            if(IsTargetVisible) PlayerCaughtSightOfLady?.Invoke(true);
            else PlayerCaughtSightOfLady?.Invoke(false);
        }
        IsTargetCurrentlyVisible = IsTargetVisible;
        //Debug.Log("Visible: " + IsTargetVisible);
    }

    private bool IsInFrustum()
    {
        // Frustum check is relatively cheap.
        cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(playerCamera);
        return GeometryUtility.TestPlanesAABB(cameraFrustumPlanes, targetRenderer.bounds);
    }

    private bool IsNotOccluded()
    {
        // Raycast is more expensive, so we do it after the frustum check.
        Vector3 direction = targetRenderer.bounds.center - playerCamera.transform.position;
        float distance = direction.magnitude;

        // We use Linecast which is slightly more efficient than Raycast for this purpose.
        if (Physics.Raycast(playerCamera.transform.position, targetRenderer.bounds.center, out RaycastHit hit, distance, occlusionLayers))
        {
            // If we hit something, we need to check if it's part of the target.
            // This handles cases where the ray hits the target's own collider.
            if (hit.transform.IsChildOf(targetRenderer.transform) || hit.transform == targetRenderer.transform)
            {
                return true; // The hit was the target itself, so it's not occluded.
            }
            return false; // The hit was something else, so it's occluded.
        }

        // If the Linecast didn't hit anything, it means there's a clear line of sight.
        return true;
    }
}