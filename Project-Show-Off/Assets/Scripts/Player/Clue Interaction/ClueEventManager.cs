// ClueEventManager.cs
using UnityEngine;
using System;
using System.Collections.Generic;

public class ClueEventManager : MonoBehaviour
{
    public static ClueEventManager Instance { get; private set; }

    private HashSet<string> collectedClueIDs = new HashSet<string>();

    public event Action<string> OnClueCollected; // Event for when a specific clue is collected
    public event Action<int> OnClueCountChanged; // Event for when the total count of collected clues changes

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Make it persistent across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterClueCollected(string clueID)
    {
        if (string.IsNullOrEmpty(clueID))
        {
            Debug.LogWarning("Attempted to register a clue with an empty ID.");
            return;
        }

        if (collectedClueIDs.Add(clueID)) // .Add returns true if the item was new
        {
            Debug.Log($"Clue Event Manager: Clue '{clueID}' registered. Total clues: {collectedClueIDs.Count}");
            OnClueCollected?.Invoke(clueID);
            OnClueCountChanged?.Invoke(collectedClueIDs.Count);

            // Example: Trigger an event if 3 clues are collected
            if (collectedClueIDs.Count == 3)
            {
                Debug.Log("Three clues collected! Something mysterious happens...");
                // TriggerYourCustomEventForThreeClues();
            }
        }
        else
        {
            Debug.LogWarning($"Clue Event Manager: Clue '{clueID}' was already collected.");
        }
    }

    public bool IsClueCollected(string clueID)
    {
        return collectedClueIDs.Contains(clueID);
    }

    public int GetCollectedClueCount()
    {
        return collectedClueIDs.Count;
    }

    // Example of how another script might subscribe:
    // void OnEnable() { ClueEventManager.Instance.OnClueCountChanged += HandleClueCountChanged; }
    // void OnDisable() { ClueEventManager.Instance.OnClueCountChanged -= HandleClueCountChanged; }
    // void HandleClueCountChanged(int newCount) { Debug.Log("Clue count is now: " + newCount); }
}