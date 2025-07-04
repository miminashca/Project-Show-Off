using UnityEngine;
using System.Collections;

public class JumpscareLogic : MonoBehaviour
{
    [Header("Monster")]
    [SerializeField] private GameObject TargetEntity; //the monstr

    [Header("Triggers")]
    [SerializeField] private GameObject enableTrigger; //the trigger to enable the jumpscare
    [SerializeField] private GameObject disableTrigger; //the trigger to disable the jumpscare

    [Header("Settings")]
    [SerializeField] private float disableDelay = 1.5f;
    private void Awake()
    {
        TargetEntity.SetActive(false); // Ensure the target entity is inactive at the start
        disableTrigger.SetActive(false); // Ensure the disable trigger is inactive at the start
        enableTrigger.SetActive(true); // Ensure the enable trigger is active at the start
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // --- Logic for the ENABLE Trigger ---
        // Now, check the tag of the GameObject THIS script is attached to.
        if (gameObject.CompareTag("JumpscareEnable"))
        {
            Debug.Log("Player entered the ENABLE trigger. Jumpscare ON!");
            if(TargetEntity != null)
            {
                TargetEntity.SetActive(true); // Activate the target entity
            }
            if(disableTrigger != null)
            {
                disableTrigger.SetActive(true); // Activate the disable trigger
            }
            gameObject.SetActive(false); // Disable the enable trigger
        }
        else if (gameObject.CompareTag("JumpscareDisable"))
        {
            Debug.Log("Player entered the DISABLE trigger. Starting disable timer...");

            // We don't disable the monster immediately. We start a Coroutine.
            StartCoroutine(DisableMonsterAfterDelay());
        }
    }

    private IEnumerator DisableMonsterAfterDelay()
    {
        // Wait for the specified amount of time
        yield return new WaitForSeconds(disableDelay);

        Debug.Log("Timer finished. Disabling monster.");

        // After the delay, disable the monster
        if (TargetEntity != null) TargetEntity.SetActive(false);

        // Disable this disable trigger so it can't be used again
        gameObject.SetActive(false);
    }
}
