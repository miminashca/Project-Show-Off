using UnityEngine;

public class JumpscareEnableTrigger : MonoBehaviour
{
    [SerializeField] private GameObject monsterObject;
    [SerializeField] private GameObject disableTrigger;

    private void Awake()
    {
        // Ensure the monster is initially inactive
        if (monsterObject != null)
        {
            monsterObject.SetActive(false);
        }
        
        // Ensure the disable trigger is initially inactive
        if (disableTrigger != null)
        {
            disableTrigger.SetActive(false);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Activate the monster and the next trigger
            monsterObject.SetActive(true);
            disableTrigger.SetActive(true);

            // Disable this trigger so it only happens once
            gameObject.SetActive(false);
        }
    }
}