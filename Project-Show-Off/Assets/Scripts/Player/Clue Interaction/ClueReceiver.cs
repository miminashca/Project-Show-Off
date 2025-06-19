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
        clueNormal.SetActive(false);

        ClueEventManager.Instance.OnClueSubmitted += SwapClueGhost;
    }

    private void OnDestroy()
    {
        ClueEventManager.Instance.OnClueSubmitted -= SwapClueGhost;
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
