using UnityEngine;

public class HunterActivationManager : MonoBehaviour
{
    [Header("Hunter Objects")]
    [Tooltip("The static, sitting Hunter object that is visible at the start.")]
    [SerializeField] private GameObject hunterPhase1_Static;

    [Tooltip("The full AI Hunter object that is disabled at the start.")]
    [SerializeField] private GameObject hunterPhase2_Active;

    [Header("Activation Condition")]
    [Tooltip("How many clues must be collected to trigger the activation.")]
    [SerializeField] private int cluesNeededForActivation = 3;

    // A flag to ensure we only activate once.
    private bool isHunterActivated = false;

    void Awake()
    {
        // Ensure the initial state is correct, just in case.
        // This is important if you are testing and might have left the objects in the wrong state.
        if (hunterPhase1_Static != null) hunterPhase1_Static.SetActive(true);
        if (hunterPhase2_Active != null) hunterPhase2_Active.SetActive(false);

        // Subscribe to the clue event manager. This is safe to do in Awake.
        if (ClueEventManager.Instance != null)
        {
            ClueEventManager.Instance.OnClueCountChanged += HandleClueCountChanged;
        }
        else
        {
            Debug.LogError("HunterActivationManager: ClueEventManager.Instance not found!");
        }
    }

    void Start()
    {
        // Also check the condition on Start. This handles the case where the
        // player loads a save file where the clue count is already high enough.
        if (ClueEventManager.Instance != null)
        {
            HandleClueCountChanged(ClueEventManager.Instance.GetCollectedClueCount());
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
    /// This method is called whenever a clue is collected.
    /// </summary>
    private void HandleClueCountChanged(int newClueCount)
    {
        // If we haven't activated yet AND the condition is met...
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
        isHunterActivated = true;

        // Perform the swap
        if (hunterPhase1_Static != null)
        {
            hunterPhase1_Static.SetActive(false);
        }
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

        // Optional: you could even destroy this manager object now
        Destroy(this.gameObject);
    }

    public void PlayActivationSFX()
    {

    }
}