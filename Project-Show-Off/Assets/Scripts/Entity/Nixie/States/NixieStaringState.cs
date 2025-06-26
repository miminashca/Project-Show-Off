using UnityEngine;

public class NixieStaringState : State
{
    private NixieStateMachine nixieSM;
    private NixieAI nixieAI;
    private NixieNavigation nixieNav;
    // --- MODIFIED: We will get this reference in OnEnterState ---
    private NixieSoundController nixieSoundController;

    public NixieStaringState(StateMachine pSM) : base(pSM)
    {
        nixieSM = (NixieStateMachine)SM;
        nixieAI = nixieSM.NixieAI;
        nixieNav = nixieSM.NixieNav;
        // --- REMOVED: Do not get the reference here, it's too early!
        // nixieSoundController = nixieAI.SoundController;
    }

    public override void OnEnterState()
    {
        Debug.Log("Nixie entering STARING state.");

        // --- MODIFIED: Get the reference here. It is guaranteed to exist now. ---
        nixieSoundController = nixieAI.SoundController;

        nixieNav.StopMoving();
        nixieNav.SetPeeking(true);

        if (nixieSoundController != null)
        {
            nixieSoundController.StartProvocativeLoop();
        }
        else
        {
            Debug.LogError("NixieStaringState could not find the NixieSoundController!");
        }
    }

    public override void Handle()
    {
        // If player turns lantern off, Nixie loses interest and roams.
        if (!nixieAI.PlayerStatus.IsLanternOn)
        {
            SM.TransitToState(nixieSM.RoamingState);
            return;
        }

        bool isPointBlank = nixieAI.DistanceToPlayer <= nixieAI.PointBlankRadius;

        if (nixieAI.IsPlayerInMyZone && (nixieAI.DistanceToPlayer <= nixieAI.CurrentDetectionRadius || isPointBlank))
        {
            SM.TransitToState(nixieSM.ChasingState);
            return;
        }
        if (nixieAI.DistanceToPlayer > nixieAI.StaringRadius)
        {
            SM.TransitToState(nixieSM.RoamingState);
            return;
        }

        nixieNav.LookAt(nixieAI.PlayerTransform.position);
    }

    public override void OnExitState()
    {
        if (nixieSoundController != null)
        {
            nixieSoundController.StopProvocativeLoop();
        }
    }
}