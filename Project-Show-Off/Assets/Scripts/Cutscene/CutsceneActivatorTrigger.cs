using UnityEngine;
using UnityEngine.Playables;
using System.Collections;
using Unity.Cinemachine;
using FMODUnity;

public class CutsceneActivatorTrigger : MonoBehaviour
{
    [Header("Cutscene Components")]
    [Tooltip("The PlayableDirector that controls the cutscene.")]
    public PlayableDirector cutsceneDirector;

    //New
    [Tooltip("The CinemachineBrain on your main camera. It will be enabled when the cutscene starts.")]
    public CinemachineBrain cinemachineBrain;
    //---
    [Header("Activation Conditions")]
    [Tooltip("The number of clues that must be submitted to the altar to trigger the cutscene.")]
    [SerializeField] private int requiredClueCount = 5;

    [Header("Player Feedback")]
    [Tooltip("(Optional) A UI Text or Panel to show the player if they don't have enough clues.")]
    [SerializeField] private GameObject hintMessageUI;
    [Tooltip("How long the hint message should stay on screen.")]
    [SerializeField] private float hintDisplayTime = 4f;

    // NEW FMOD CHANGE
    [SerializeField] private EventReference branchSound;
    [SerializeField] private EventReference kidSound;
    // END FMOD CHANGE

    private Collider triggerCollider;

    private void Awake()
    {
        // Get the collider component on this object
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            Debug.LogError("CutsceneActivatorTrigger requires a Collider component on the same GameObject!", this);
        }

        // Ensure the hint message is hidden at the start
        if (hintMessageUI != null)
        {
            hintMessageUI.SetActive(false);
        }

        if (cinemachineBrain == null)
        {
            Debug.LogWarning("The Cinemachine Brain has not been assigned in the inspector!", this);
        }
        else
        {
            cinemachineBrain.GetComponent<Camera>().enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only proceed if the object that entered is the player
        if (other.CompareTag("Player"))
        {
     
           
            // First, check if the ClueEventManager exists
            if (ClueEventManager.Instance == null)
            {
                Debug.LogError("Cannot check clue count because ClueEventManager.Instance is null!");
                return;
            }

            // THE CORE LOGIC: Check if the number of submitted clues meets the requirement
            if (ClueEventManager.Instance.GetSubmittedClueCount() >= requiredClueCount)
            {
                // --- SUCCESS: Player has all clues ---
                Debug.Log("All clues submitted! Starting final cutscene.");
                StartCoroutine(WaitForTimeline());
                // Hide the hint message just in case it was somehow active
                if (hintMessageUI != null) hintMessageUI.SetActive(false);

                //New
                if (cinemachineBrain != null)
                {
                    cinemachineBrain.enabled = true;
                   
                    Debug.Log("CinemachineBrain enabled for cutscene.");
                }
                //

                

                // Disable the trigger collider so this can't be activated again accidentally
                if (triggerCollider != null)
                {
                    triggerCollider.enabled = false;
                }
            }
            else
            {
                // --- FAILURE: Player is missing clues ---
                int currentClues = ClueEventManager.Instance.GetSubmittedClueCount();
                Debug.Log($"Player tried to activate the cutscene but only has {currentClues}/{requiredClueCount} clues submitted.");

                // Show a hint to the player if a UI element is assigned
                if (hintMessageUI != null && !hintMessageUI.activeInHierarchy)
                {
                    StartCoroutine(ShowHintMessage());
                }
            }
        }
    }

    /// <summary>
    /// A small coroutine to display a message to the player for a few seconds.
    /// </summary>
    private IEnumerator ShowHintMessage()
    {
        hintMessageUI.SetActive(true);
        yield return new WaitForSeconds(hintDisplayTime);
        hintMessageUI.SetActive(false);
    }

    IEnumerator WaitForTimeline()
    {
        yield return new WaitForSeconds(1f);

        if (!kidSound.IsNull)
        {
            RuntimeManager.PlayOneShot(kidSound, transform.position);
        }

        // Play the branch sound at the position of this GameObject.
        if (!branchSound.IsNull)
        {
            RuntimeManager.PlayOneShot(branchSound, transform.position);
        }

        cinemachineBrain.GetComponent<Camera>().enabled = true;
        cutsceneDirector.Play();
    }
}
