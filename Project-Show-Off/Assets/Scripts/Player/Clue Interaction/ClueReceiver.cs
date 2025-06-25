// ClueReceiver.cs
using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ClueReceiver : MonoBehaviour
{
    [SerializeField] private GameObject clueGhostPrefab;
    [SerializeField] private GameObject clueNormalPrefab;
    [SerializeField] private string clueID;
    private GameObject clueGhost;
    private GameObject clueNormal;
    
    void Start()
    {
        clueGhost = Instantiate(clueGhostPrefab, this.transform);
        clueNormal = Instantiate(clueNormalPrefab, this.transform);

        if (ClueEventManager.Instance != null)
        {
            // Subscribe to future submissions
            ClueEventManager.Instance.OnClueSubmittedWithId += SetToSubmittedStateIfMatching;
            // Subscribe to the data load event
            ClueEventManager.Instance.OnGameDataLoaded += SetInitialState;
        }
        
        // Check initial state on load
        if (ClueEventManager.Instance != null && ClueEventManager.Instance.IsClueSubmitted(clueID))
        {
            // This clue was already submitted in the loaded save data.
            // Immediately set the correct visual state.
            SetToSubmittedState();
        }
        else
        {
            // Clue is not yet submitted, show the ghost.
            clueGhost.SetActive(false);
            clueNormal.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe safely
        if (ClueEventManager.Instance != null)
        {
            ClueEventManager.Instance.OnClueSubmittedWithId -= SetToSubmittedStateIfMatching;
            ClueEventManager.Instance.OnGameDataLoaded -= SetInitialState;
        }
    }
    
    // This method runs once after the game data is confirmed to be loaded.
    private void SetInitialState()
    {
        if (ClueEventManager.Instance.IsClueSubmitted(clueID))
        {
            SetToSubmittedState();
        }
        else
        {
            // If not submitted, show the ghost.
            clueGhost.SetActive(true);
        }
    }

    public void SubmitClue()
    {
        if (ClueEventManager.Instance != null)
        {
            ClueEventManager.Instance.RegisterClueSubmit(clueID);
        }
    }
    
    // Renamed from SwapClueGhost to be more descriptive and check the ID
    private void SetToSubmittedStateIfMatching(string submittedClueId)
    {
        if(submittedClueId == clueID)
        {
            SetToSubmittedState();
        }
    }
    
    // A helper method to avoid duplicating code
    private void SetToSubmittedState()
    {
        if (clueGhost != null) clueGhost.SetActive(false);
        if (clueNormal != null) clueNormal.SetActive(true);
    }
}