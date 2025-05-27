using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(HunterAI))] // Relies on ThimbleHunterAI for some parameters
public class HunterNavigation : MonoBehaviour
{
    private HunterAI _hunterAI;

    [Header("Roaming Node Graph")]
    public List<Transform> RoamingNodes = new List<Transform>();
    public enum NodeSelectionMode
    {
        Random,
        Sequential,
        NearestToPlayer // Subtly guide hunter
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

        List<Transform> candidateNodes = new List<Transform>();
        Vector3 playerPos = _hunterAI.PlayerTransform.position;
        Vector3 hunterPos = _hunterAI.transform.position;

        foreach (Transform node in RoamingNodes)
        {
            if (node == null) continue;

            float distToPlayer = Vector3.Distance(node.position, playerPos);
            float distToHunter = Vector3.Distance(node.position, hunterPos);

            // Basic distance checks
            if (distToPlayer >= MinSuperpositionDistFromPlayer &&
                distToPlayer <= MaxSuperpositionDistFromPlayer &&
                distToHunter >= MinDistFromHunterForSuperposition)
            {
                // Line of Sight Check from Player to Node (we want it to be NOT visible)
                // This is a simplified check; a more robust one might involve checking from player's camera.
                // We want the Hunter to spawn "around a corner" or "out of sight".
                Vector3 directionToNodeFromPlayer = (node.position - (_hunterAI.PlayerTransform.position + Vector3.up * 1.5f)).normalized; // Player eye level approx
                RaycastHit hit;
                // Check if there's an obstacle between player and the node
                if (Physics.Raycast(_hunterAI.PlayerTransform.position + Vector3.up * 1.5f,
                                    directionToNodeFromPlayer,
                                    out hit,
                                    distToPlayer * 0.95f, // Check slightly less than full distance to avoid node itself blocking
                                    ~(1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("Hunter")), // Ignore player and hunter layers
                                    QueryTriggerInteraction.Ignore))
                {
                    // If something is hit, it means the node is likely obscured from player's current view
                    candidateNodes.Add(node);
                }
                // If nothing is hit, the path to the node MIGHT be clear. We prefer obscured nodes.
                // For a simpler start, you can skip the LoS check for superposition,
                // or invert it if you want nodes the player *could* soon see.
                // The current logic prefers nodes the player *cannot* currently see directly.
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