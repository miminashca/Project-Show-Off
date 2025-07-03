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
    
    
    public event Action OnGameDataLoaded; // This event signals that the save data is ready.
    
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
        if (string.IsNullOrEmpty(clueID) || submittedClueIDs.Contains(clueID))
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
            submittedClueIDs.Add(clueID);
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

    //Stefani CutScnene trigger logic
    public int GetSubmittedClueCount()
    {
        return submittedClueIDs.Count;
    }
    //--------------------------------

    public int GetCollectedClueCount()
    {
        return collectedClueIDs.Count;
    }

    public void PickUpFuel()
    {
        OnFuelPickedUp?.Invoke();
    }
    
    public void LoadClues(List<string> loadedCollected, List<string> loadedSubmitted)
    {
        collectedClueIDs = loadedCollected != null ? new HashSet<string>(loadedCollected) : new HashSet<string>();
        submittedClueIDs = loadedSubmitted != null ? new HashSet<string>(loadedSubmitted) : new HashSet<string>();
        Debug.Log($"Loaded {collectedClueIDs.Count} collected clues and {submittedClueIDs.Count} submitted clues.");
        
        // This is fine for UI counters that need an immediate update
        OnClueCountChanged?.Invoke(collectedClueIDs.Count);

        // --- INVOKE THE NEW EVENT ---
        // Signal to all scene objects that the data is now populated.
        Debug.Log("Invoking OnGameDataLoaded event.");
        OnGameDataLoaded?.Invoke();
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