using UnityEngine;
using FMODUnity;

public class HunterActivationManager : MonoBehaviour
{
    [Header("Hunter Objects")]
    [Tooltip("The static, sitting Hunter object that is visible at the start.")]
    [SerializeField] private GameObject hunterPhase1_Static;

    [Tooltip("The full AI Hunter object that is disabled at the start.")]
    [SerializeField] private GameObject hunterPhase2_Active;

    [Header("Activation Condition")]
    [Tooltip("How many clues must be collected to trigger the activation.")]
    [SerializeField] private int cluesNeededForActivation = 1;

    [SerializeField] private EventReference hunterActivationYell;

    [SerializeField] private Vector3 yellOrigin = new Vector3(116f, 5f, -107f);

    // A flag to ensure we only activate once.
    private bool isHunterActivated = false;

    public bool IsHunterActive() => isHunterActivated;

    void Awake()
    {
        // Subscribe to the clue event manager. This is for the *first time* activation.
        if (ClueEventManager.Instance != null)
        {
            ClueEventManager.Instance.OnClueCountChanged += HandleClueCountChanged;
        }
        else
        {
            Debug.LogError("HunterActivationManager: ClueEventManager.Instance not found!");
        }
    }

    private void OnDestroy()
    {
        // Always unsubscribe to prevent errors.
        if (ClueEventManager.Instance != null)
        {
            ClueEventManager.Instance.OnClueCountChanged -= HandleClueCountChanged;
        }
    }

    /// <summary>
    /// This is the new method called by GameManager to set the state on load.
    /// </summary>
    public void InitializeState(bool shouldBeActive)
    {
        Debug.Log($"HunterActivationManager initializing. Should be active: {shouldBeActive}");
        isHunterActivated = shouldBeActive;

        if (isHunterActivated)
        {
            // If already active, set the objects correctly and stop listening for clues.
            if (hunterPhase1_Static != null) hunterPhase1_Static.SetActive(false);
            if (hunterPhase2_Active != null) hunterPhase2_Active.SetActive(true);

            // We don't need to listen anymore, the event already happened.
            if (ClueEventManager.Instance != null)
            {
                ClueEventManager.Instance.OnClueCountChanged -= HandleClueCountChanged;
            }
        }
        else
        {
            // If not yet active, ensure the default state.
            if (hunterPhase1_Static != null) hunterPhase1_Static.SetActive(true);
            if (hunterPhase2_Active != null) hunterPhase2_Active.SetActive(false);
        }
    }

    /// <summary>
    /// This method is called whenever a clue is collected.
    /// </summary>
    private void HandleClueCountChanged(int newClueCount)
    {
        // This logic is still needed for when the player activates the hunter for the first time.
        if (!isHunterActivated && newClueCount >= cluesNeededForActivation)
        {
            ActivatePhase2Hunter();
        }
    }

    /// <summary>
    /// Performs the swap, disabling the static prop and enabling the real AI.
    /// </summary>
    private void ActivatePhase2Hunter()
    {
        Debug.Log($"CLUE THRESHOLD REACHED. Swapping to Active Hunter!");
        isHunterActivated = true; // Set our state flag

        // Perform the swap
        if (hunterPhase1_Static != null) hunterPhase1_Static.SetActive(false);
        if (hunterPhase2_Active != null)
        {
            hunterPhase2_Active.SetActive(true);
            PlayActivationSFX();
        }

        // We have done our job, so we can stop listening to the event.
        if (ClueEventManager.Instance != null)
        {
            ClueEventManager.Instance.OnClueCountChanged -= HandleClueCountChanged;
        }
    }

    public void PlayActivationSFX()
    {
        if (!hunterActivationYell.IsNull)
        {
            RuntimeManager.PlayOneShot(hunterActivationYell, yellOrigin);
        }
    }
}