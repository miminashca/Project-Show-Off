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

        ClueEventManager.Instance.OnClueSubmittedWithId += SwapClueGhost;
        
        if (ClueEventManager.Instance.IsClueSubmitted(clueID))
        {
            // This clue was already submitted in the loaded save data.
            // Immediately set the correct visual state.
            SwapClueGhost(clueID);
        }
        else
        {
            // Clue is not yet submitted, show the ghost.
            clueGhost.SetActive(true);
            clueNormal.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        ClueEventManager.Instance.OnClueSubmittedWithId -= SwapClueGhost;
    }

    public void SubmitClue()
    {
        ClueEventManager.Instance.RegisterClueSubmit(clueID);
    }

    private void SwapClueGhost(string pClueId)
    {
        if(pClueId != clueID) return;
        clueGhost.SetActive(false);
        clueNormal.SetActive(true);
    }
    
}
