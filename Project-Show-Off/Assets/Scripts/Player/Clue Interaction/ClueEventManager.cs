// ClueEventManager.cs
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class ClueEventManager : MonoBehaviour
{
    public static ClueEventManager Instance { get; private set; }

    private HashSet<string> collectedClueIDs = new HashSet<string>();
    private HashSet<string> submittedClueIDs = new HashSet<string>();

    public event Action OnClueCollected; // Event for when a specific clue is collected
    public event Action<string> OnClueCollectedWithId; // Event for when a specific clue is collected
    public event Action<int> OnClueCollectedAmount; // Event for when a specific clue is collected
    public event Action OnClueSubmitted; // Event for when a specific clue is submitted
    public event Action<string> OnClueSubmittedWithId; // Event for when a specific clue is submitted
    public event Action<int> OnClueCountChanged; // Event for when the total count of collected clues changes
    public event Action OnFuelPickedUp; // Event for when the total count of collected clues changes
    
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
            OnClueCollectedWithId?.Invoke(clueID);
            OnClueCollectedAmount?.Invoke(collectedClueIDs.Count);
            OnClueCollected?.Invoke();
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
    public void RegisterClueSubmit(string clueID)
    {
        if (string.IsNullOrEmpty(clueID))
        {
            Debug.LogWarning("Attempted to register a clue with an empty ID.");
            return;
        }

        if (collectedClueIDs.Contains(clueID))
        {
            Debug.Log($"Clue Event Manager: Clue '{clueID}' submitted.");
            collectedClueIDs.Remove(clueID);
            OnClueSubmitted?.Invoke();
            OnClueSubmittedWithId?.Invoke(clueID);
            OnClueCountChanged?.Invoke(collectedClueIDs.Count);
        }
        else
        {
            Debug.LogWarning($"Clue Event Manager: Clue '{clueID}' has not been collected yet.");
        }
    }

    public bool IsClueCollected(string clueID)
    {
        return collectedClueIDs.Contains(clueID);
    }
    public bool IsClueSubmitted(string clueID)
    {
        return submittedClueIDs.Contains(clueID);
    }

    public int GetCollectedClueCount()
    {
        return collectedClueIDs.Count;
    }

    public void PickUpFuel()
    {
        OnFuelPickedUp?.Invoke();
    }
    
    public void LoadClues(List<string> loadedCollectedIDs, List<string> loadedSubmittedIDs)
    {
        collectedClueIDs = (loadedCollectedIDs != null)
            ? new HashSet<string>(loadedCollectedIDs)
            : new HashSet<string>();

        submittedClueIDs = (loadedSubmittedIDs != null)
            ? new HashSet<string>(loadedSubmittedIDs)
            : new HashSet<string>();

        Debug.Log($"Loaded {collectedClueIDs.Count} collected clues and {submittedClueIDs.Count} submitted clues.");
    
        OnClueCountChanged?.Invoke(collectedClueIDs.Count);
    }

    public List<string> GetCollectedClueIDs()
    {
        return collectedClueIDs.ToList();
    }
    public List<string> GetSubmittedClueIDs()
    {
        return submittedClueIDs.ToList();
    }
    // Example of how another script might subscribe:
    // void OnEnable() { ClueEventManager.Instance.OnClueCountChanged += HandleClueCountChanged; }
    // void OnDisable() { ClueEventManager.Instance.OnClueCountChanged -= HandleClueCountChanged; }
    // void HandleClueCountChanged(int newCount) { Debug.Log("Clue count is now: " + newCount); }
}