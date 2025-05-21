using System.Collections;
using UnityEngine;
using FMODUnity;

public class RandomSoundEmitter : MonoBehaviour
{
    public EventReference fmodEvent; // Drag your FMOD event here
    public float minDelay = 25f;
    public float maxDelay = 45f;

    private Coroutine playRoutine;

    void Start()
    {
        playRoutine = StartCoroutine(PlaySoundAtRandomIntervals());
    }

    IEnumerator PlaySoundAtRandomIntervals()
    {
        while (true)
        {
            float delay = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(delay);

            RuntimeManager.PlayOneShot(fmodEvent, transform.position);
        }
    }

    // Optional: Stop playback if needed
    public void StopEmitting()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }
    }
}
