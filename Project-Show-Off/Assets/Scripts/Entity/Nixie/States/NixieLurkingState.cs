using UnityEngine;

public class NixieLurkingState : State
{
    private NixieStateMachine nixieSM;
    private NixieAI nixieAI;
    private NixieNavigation nixieNav;
    private NixieSoundController nixieSoundController; // --- ADDED: Reference to the sound controller

    private float lurkTimer;
    private const float LURK_DURATION = 8f;
    private Vector3 targetLurkPosition;

    public NixieLurkingState(StateMachine pSM) : base(pSM)
    {
        nixieSM = (NixieStateMachine)SM;
        nixieAI = nixieSM.NixieAI;
        nixieNav = nixieSM.NixieNav;
    }

    public override void OnEnterState()
    {
        Debug.Log("Nixie entering LURKING state.");

        nixieSoundController = nixieAI.SoundController; // --- ADDED: Get the sound controller from the AI

        lurkTimer = LURK_DURATION;
        nixieNav.SetPeeking(true); // Peek above water to investigate

        // --- ADDED: Start the provocative sound loop when the Nixie starts lurking.
        if (nixieSoundController != null)
        {
            nixieSoundController.StartProvocativeLoop();
        }

        targetLurkPosition = nixieAI.PlayerLastKnownPosition;
        nixieNav.MoveTo(targetLurkPosition, nixieNav.RoamingSpeed);
    }

    public override void Handle()
    {
        // --- High-priority transition: Player becomes visible again ---
        bool isLanternOn = nixieAI.PlayerStatus.IsLanternOn;
        bool isPointBlank = nixieAI.DistanceToPlayer <= nixieAI.PointBlankRadius;
        if (nixieAI.IsPlayerInMyZone && (isLanternOn || isPointBlank))
        {
            SM.TransitToState(nixieSM.ChasingState);
            return;
        }

        // --- Behavior Logic ---
        nixieNav.LookAt(targetLurkPosition);

        // Check if we've reached the destination
        if (Vector3.Distance(nixieAI.transform.position, targetLurkPosition) < 1.0f)
        {
            nixieNav.StopMoving();

            // Once stopped, start the countdown timer.
            lurkTimer -= Time.deltaTime;
            if (lurkTimer <= 0)
            {
                Debug.Log("Nixie is done lurking, returning to roam.");
                SM.TransitToState(nixieSM.RoamingState);
            }
        }
    }

    public override void OnExitState()
    {
        Debug.Log("Nixie exiting LURKING state.");

        // --- ADDED: Stop the provocative loop when the Nixie is no longer lurking.
        if (nixieSoundController != null)
        {
            nixieSoundController.StopProvocativeLoop();
        }

        nixieNav.StopMoving();
    }
}