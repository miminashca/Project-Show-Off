using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(HunterAI))]
public class HunterNavigation : MonoBehaviour
{
    private HunterAI _hunterAI;
    private Camera _playerCamera;

    [Header("Roaming Node Graph")]
    public List<Transform> RoamingNodes = new List<Transform>();
    public enum NodeSelectionMode
    {
        Random,
        Sequential,
        NearestToPlayer
    }
    public NodeSelectionMode RoamingNodeSelection = NodeSelectionMode.Random;
    private int _currentNodeIndex = -1; // For sequential mode

    [Header("Superposition Settings")]
    public float MinSuperpositionDistFromPlayer = 15f;
    public float MaxSuperpositionDistFromPlayer = 30f;
    public float MinDistFromHunterForSuperposition = 5f; // Node should not be too close to current hunter pos

    void Awake()
    {
        _hunterAI = GetComponent<HunterAI>();
        if (_hunterAI == null)
        {
            Debug.LogError("HunterNavigation requires a ThimbleHunterAI component on the same GameObject!", this);
            enabled = false;
        }

        if (_hunterAI.PlayerTransform != null)
        {
            _playerCamera = Camera.main;
        }

        if (RoamingNodes.Count == 0)
        {
            Debug.LogWarning("HunterNavigation: No roaming nodes assigned. Roaming will be limited.", this);
        }
    }

    /// <summary>
    /// Gets the next node for the Hunter to roam to, based on the selected mode.
    /// </summary>
    public Transform GetNextRoamNode()
    {
        if (RoamingNodes.Count == 0) return null;

        switch (RoamingNodeSelection)
        {
            case NodeSelectionMode.Random:
                return RoamingNodes[Random.Range(0, RoamingNodes.Count)];

            case NodeSelectionMode.Sequential:
                _currentNodeIndex = (_currentNodeIndex + 1) % RoamingNodes.Count;
                return RoamingNodes[_currentNodeIndex];

            case NodeSelectionMode.NearestToPlayer:
                if (_hunterAI.PlayerTransform == null) return GetRandomRoamNode(); // Fallback
                return FindNearestNodeToPoint(RoamingNodes, _hunterAI.PlayerTransform.position);

            default:
                return RoamingNodes[0]; // Fallback
        }
    }

    /// <summary>
    /// Helper to get a purely random roam node.
    /// </summary>
    public Transform GetRandomRoamNode()
    {
        if (RoamingNodes.Count == 0) return null;
        return RoamingNodes[Random.Range(0, RoamingNodes.Count)];
    }


    /// <summary>
    /// Selects a "valid" node for superposition.
    /// A good node is within a certain range of the player,
    /// not in direct line of sight, and not too close to the hunter's current position.
    /// </summary>
    public Transform GetSuperpositionNode()
    {
        if (RoamingNodes.Count == 0 || _hunterAI.PlayerTransform == null) return null;
        if (_playerCamera == null) _playerCamera = Camera.main; // Try to get it again if it was null
        if (_playerCamera == null)
        {
            Debug.LogWarning("Superposition check failed: Player Camera not found!");
            return GetRandomRoamNode(); // Fallback if no camera
        }


        List<Transform> candidateNodes = new List<Transform>();
        Vector3 playerPos = _hunterAI.PlayerTransform.position;
        Vector3 hunterPos = _hunterAI.transform.position;
        Vector3 playerCamPos = _playerCamera.transform.position;
        LayerMask obstacleMask = ~(1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("Hunter")); // Ignore player and hunter

        foreach (Transform node in RoamingNodes)
        {
            if (node == null) continue;

            float distToPlayer = Vector3.Distance(node.position, playerPos);
            float distToHunter = Vector3.Distance(node.position, hunterPos);

            // Basic distance checks (same as before)
            if (distToPlayer >= MinSuperpositionDistFromPlayer &&
                distToPlayer <= MaxSuperpositionDistFromPlayer &&
                distToHunter >= MinDistFromHunterForSuperposition)
            {
                // --- NEW: Player Line of Sight Check ---
                // Is the node hidden from the player's view?
                Vector3 dirToNodeFromPlayer = (node.position - playerCamPos).normalized;
                float distToNodeFromPlayer = Vector3.Distance(node.position, playerCamPos);

                // If a raycast from the player's camera HITS something before the node, it means the node is obscured.
                if (Physics.Raycast(playerCamPos, dirToNodeFromPlayer, distToNodeFromPlayer * 0.95f, obstacleMask, QueryTriggerInteraction.Ignore))
                {
                    // This is a good candidate! It's out of the player's sight.
                    candidateNodes.Add(node);
                }
            }
        }

        if (candidateNodes.Count > 0)
        {
            // Optional: Add further scoring (e.g., nodes in player's general direction of movement)
            return candidateNodes[Random.Range(0, candidateNodes.Count)];
        }
        else
        {
            // Fallback: No ideal node found, maybe pick a random one outside immediate player vicinity
            // Or a node far from the hunter but within a broader range of the player.
            // For now, return null or a less optimal random node.
            Debug.LogWarning("HunterNavigation: Could not find an ideal superposition node. Falling back.");
            List<Transform> fallbackNodes = RoamingNodes.Where(n =>
                Vector3.Distance(n.position, playerPos) > MinSuperpositionDistFromPlayer &&
                Vector3.Distance(n.position, hunterPos) > MinDistFromHunterForSuperposition
            ).ToList();

            return fallbackNodes.Count > 0 ? fallbackNodes[Random.Range(0, fallbackNodes.Count)] : GetRandomRoamNode();
        }
    }

    /// <summary>
    /// Finds the nearest node from a list to a given world point.
    /// </summary>
    private Transform FindNearestNodeToPoint(List<Transform> nodes, Vector3 point)
    {
        if (nodes == null || nodes.Count == 0) return null;

        Transform nearestNode = null;
        float minDistanceSqr = Mathf.Infinity;

        foreach (Transform node in nodes)
        {
            if (node == null) continue;
            Vector3 diff = node.position - point;
            float distSqr = diff.sqrMagnitude;
            if (distSqr < minDistanceSqr)
            {
                minDistanceSqr = distSqr;
                nearestNode = node;
            }
        }
        return nearestNode;
    }

    void OnDrawGizmosSelected()
    {
        if (RoamingNodes != null)
        {
            Gizmos.color = Color.green;
            foreach (Transform node in RoamingNodes)
            {
                if (node != null)
                {
                    Gizmos.DrawWireSphere(node.position, 0.5f);
                }
            }
        }

        // Visualize Superposition Ranges if Player is present
        if (_hunterAI != null && _hunterAI.PlayerTransform != null)
        {
            Gizmos.color = new Color(0.5f, 0f, 0.5f, 0.3f); // Purpleish
            Gizmos.DrawWireSphere(_hunterAI.PlayerTransform.position, MinSuperpositionDistFromPlayer);
            Gizmos.DrawWireSphere(_hunterAI.PlayerTransform.position, MaxSuperpositionDistFromPlayer);
        }
    }
}