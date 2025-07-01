using UnityEngine;
using UnityEngine.Playables;

public class Markisawesome : MonoBehaviour
{
    public Transform parentObject;

    public PlayableDirector cutsceneDirector;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
          //  other.transform.SetParent(parentObject);
        //    other.transform.position = parentObject.transform.position;
            cutsceneDirector.Play();
        }
    }
}
