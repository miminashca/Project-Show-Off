using UnityEngine;
using FMODUnity;
using System.Collections;

public class NixieSoundController : MonoBehaviour
{
    [Header("FMOD Event Paths - Nixie")]
    [SerializeField] private EventReference luringSound;
    [SerializeField] private EventReference provocativeGruntSound;
    [SerializeField] private EventReference chaseGruntSound;
    [SerializeField] private EventReference killGruntSound;
    [SerializeField] private EventReference hurtGruntSound;

    [Header("Behavioral Sound Timings")]
    [SerializeField] private float minLuringInterval = 25f;
    [SerializeField] private float maxLuringInterval = 45f;
    [SerializeField] private float minProvocativeInterval = 5f;
    [SerializeField] private float maxProvocativeInterval = 15f;

    private Coroutine luringCoroutine;
    private Coroutine provocativeCoroutine;

    void OnDestroy()
    {
        StopAllNixieSounds();
    }

    // --- LURING LOOP ---
    public void StartLuringLoop()
    {
        if (luringCoroutine == null)
        {
            Debug.Log("<color=cyan>SOUND:</color> Starting Luring Loop.", this);
            luringCoroutine = StartCoroutine(LuringCoroutine());
        }
    }

    public void StopLuringLoop()
    {
        if (luringCoroutine != null)
        {
            Debug.Log("<color=cyan>SOUND:</color> Stopping Luring Loop.", this);
            StopCoroutine(luringCoroutine);
            luringCoroutine = null;
        }
    }

    // --- PROVOCATIVE LOOP ---
    public void StartProvocativeLoop()
    {
        if (provocativeCoroutine == null)
        {
            Debug.Log("<color=orange>SOUND:</color> Starting Provocative Loop.", this);
            provocativeCoroutine = StartCoroutine(ProvocativeCoroutine());
        }
    }

    public void StopProvocativeLoop()
    {
        if (provocativeCoroutine != null)
        {
            Debug.Log("<color=orange>SOUND:</color> Stopping Provocative Loop.", this);
            StopCoroutine(provocativeCoroutine);
            provocativeCoroutine = null;
        }
    }

    public void StopAllNixieSounds()
    {
        StopLuringLoop();
        StopProvocativeLoop();
    }

    // --- ONE-SHOTS ---
    public void PlayChaseGrunt()
    {
        if (!chaseGruntSound.IsNull) RuntimeManager.PlayOneShotAttached(chaseGruntSound, gameObject);
    }

    public void PlayKillGrunt()
    {
        if (!killGruntSound.IsNull) RuntimeManager.PlayOneShotAttached(killGruntSound, gameObject);
    }

    public void PlayHurtGrunt()
    {
        if (!hurtGruntSound.IsNull) RuntimeManager.PlayOneShotAttached(hurtGruntSound, gameObject);
    }

    // --- COROUTINES ---
    private IEnumerator LuringCoroutine()
    {
        // This one can wait first, as it's a passive background sound
        while (true)
        {
            float waitTime = Random.Range(minLuringInterval, maxLuringInterval);
            yield return new WaitForSeconds(waitTime);

            if (!luringSound.IsNull)
            {
                Debug.Log("<color=cyan>SOUND:</color> Playing Luring Sound.", this);
                RuntimeManager.PlayOneShotAttached(luringSound, gameObject);
            }
        }
    }

    // --- MODIFIED COROUTINE ---
    private IEnumerator ProvocativeCoroutine()
    {
        // Play the first sound immediately for instant feedback
        if (!provocativeGruntSound.IsNull)
        {
            Debug.Log("<color=orange>SOUND:</color> Playing initial Provocative Grunt.", this);
            RuntimeManager.PlayOneShotAttached(provocativeGruntSound, gameObject);
        }

        // Now loop with the delay
        while (true)
        {
            float waitTime = Random.Range(minProvocativeInterval, maxProvocativeInterval);
            yield return new WaitForSeconds(waitTime);

            if (!provocativeGruntSound.IsNull)
            {
                Debug.Log("<color=orange>SOUND:</color> Playing looped Provocative Grunt.", this);
                RuntimeManager.PlayOneShotAttached(provocativeGruntSound, gameObject);
            }
        }
    }
}