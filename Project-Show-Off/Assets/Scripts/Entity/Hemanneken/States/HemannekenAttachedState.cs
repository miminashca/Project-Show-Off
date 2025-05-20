using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HemannekenAttachedState : State
{
    public HemannekenAttachedState(StateMachine pSM) : base(pSM)
    {
    }
    private HemannekenStateMachine HSM => (HemannekenStateMachine)SM;
    List<MeshRenderer> meshes;


    public override void OnEnterState()
    {
        Debug.Log("Entered Attached State");
        
        HSM.LockNavMeshAgent(true);

        HemannekenEventBus.AttachHemanneken();
        meshes = HSM.GetComponentsInChildren<MeshRenderer>().ToList();
        foreach (MeshRenderer mR in meshes)
        {
            if (mR.gameObject.tag == "HemannekenMesh") mR.gameObject.SetActive(false);
        }
    }

    public override void Handle()
    {
        HSM.gameObject.transform.position = HSM.GetPlayerPosition();
    }

    public override void OnExitState()
    {
        HemannekenEventBus.DetachHemanneken();
        foreach (MeshRenderer mR in meshes)
        {
            if (mR.gameObject.tag == "HemannekenMesh") mR.gameObject.SetActive(true);
        }
        HSM.LockNavMeshAgent(false);
    }
}
