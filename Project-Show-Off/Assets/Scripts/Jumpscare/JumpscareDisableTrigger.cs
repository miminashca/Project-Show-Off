using UnityEngine;
using System.Collections;

public class JumpscareDisableTrigger : MonoBehaviour
{
    [SerializeField] private GameObject monsterObject;
    [SerializeField] private float delay = 1.5f;

    private bool hasBeenTriggered = false; // Prevents the coroutine from starting multiple times

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasBeenTriggered)
        {
            hasBeenTriggered = true;
            StartCoroutine(DisableMonster());
        }
    }

    private IEnumerator DisableMonster()
    {
        // Wait for the delay
        yield return new WaitForSeconds(delay);

        // Disable the monster
        monsterObject.SetActive(false);

        // Optional: Disable this trigger object as well
         gameObject.SetActive(false);
    }
}