using UnityEngine;
using FMODUnity; // Required for FMOD types
using System.Collections; // Required for Coroutines

/// <summary>
/// Manages all FMOD sound playback for the Nixie monster.
/// Provides methods to start and stop state-specific sound loops (Luring, Provocative)
/// and to play one-shot sounds for specific actions (Chase, Hurt, Kill).
/// </summary>
public class NixieSoundController : MonoBehaviour
{
    [Header("FMOD Event Paths - Nixie")]
    [SerializeField] private EventReference luringSound;
    [SerializeField] private EventReference provocativeGruntSound;
    [SerializeField] private EventReference chaseGruntSound;
    [SerializeField] private EventReference killGruntSound;
    [SerializeField] private EventReference hurtGruntSound;

    [Header("Behavioral Sound Timings")]
    [Tooltip("The minimum time (seconds) to wait before playing a luring sound.")]
    [SerializeField] private float minLuringInterval = 25f;
    [Tooltip("The maximum time (seconds) to wait before playing a luring sound.")]
    [SerializeField] private float maxLuringInterval = 45f;

    [Tooltip("The minimum time (seconds) to wait before playing a provocative grunt.")]
    [SerializeField] private float minProvocativeInterval = 5f;
    [Tooltip("The maximum time (seconds) to wait before playing a provocative grunt.")]
    [SerializeField] private float maxProvocativeInterval = 15f;

    // Separate coroutine references for each sound loop
    private Coroutine luringCoroutine;
    private Coroutine provocativeCoroutine;

    void OnDestroy()
    {
        // Ensure all coroutines are stopped when the Nixie is destroyed.
        StopAllNixieSounds();
    }

    // --- Public Control Methods for Looping Sounds ---

    #region Luring Sound Loop (For Roaming State)
    /// <summary>
    /// Starts the periodic luring sound loop.
    /// Call this when the Nixie enters its 'Roaming' state.
    /// </summary>
    public void StartLuringLoop()
    {
        if (luringCoroutine == null)
        {
            luringCoroutine = StartCoroutine(LuringCoroutine());
        }
        else
        {
            Debug.LogWarning($"NixieSoundController on {gameObject.name}: Tried to start luring loop, but it's already running.");
        }
    }

    /// <summary>
    /// Stops the periodic luring sound loop.
    /// Call this when the Nixie exits its 'Roaming' state.
    /// </summary>
    public void StopLuringLoop()
    {
        if (luringCoroutine != null)
        {
            StopCoroutine(luringCoroutine);
            luringCoroutine = null;
        }
    }
    #endregion

    #region Provocative Sound Loop (For Staring State)
    /// <summary>
    /// Starts the periodic provocative grunt loop.
    /// Call this when the Nixie enters its 'Staring' state.
    /// </summary>
    public void StartProvocativeLoop()
    {
        if (provocativeCoroutine == null)
        {
            provocativeCoroutine = StartCoroutine(ProvocativeCoroutine());
        }
        else
        {
            Debug.LogWarning($"NixieSoundController on {gameObject.name}: Tried to start provocative loop, but it's already running.");
        }
    }

    /// <summary>
    /// Stops the periodic provocative grunt loop.
    /// Call this when the Nixie exits its 'Staring' state.
    /// </summary>
    public void StopProvocativeLoop()
    {
        if (provocativeCoroutine != null)
        {
            StopCoroutine(provocativeCoroutine);
            provocativeCoroutine = null;
        }
    }
    #endregion

    /// <summary>
    /// Stops all looping sounds managed by this controller.
    /// </summary>
    public void StopAllNixieSounds()
    {
        StopLuringLoop();
        StopProvocativeLoop();
    }

    // --- One-Shot Sound Methods ---

    public void PlayChaseGrunt()
    {
        if (!chaseGruntSound.IsNull)
            RuntimeManager.PlayOneShotAttached(chaseGruntSound, gameObject);
        else
            Debug.LogWarning($"NixieSoundController on {gameObject.name}: 'chaseGruntSound' is not assigned.");
    }

    public void PlayKillGrunt()
    {
        if (!killGruntSound.IsNull)
            RuntimeManager.PlayOneShotAttached(killGruntSound, gameObject);
        else
            Debug.LogWarning($"NixieSoundController on {gameObject.name}: 'killGruntSound' is not assigned.");
    }

    public void PlayHurtGrunt()
    {
        if (!hurtGruntSound.IsNull)
            RuntimeManager.PlayOneShotAttached(hurtGruntSound, gameObject);
        else
            Debug.LogWarning($"NixieSoundController on {gameObject.name}: 'hurtGruntSound' is not assigned.");
    }

    // --- Coroutines for Sound Loops ---

    private IEnumerator LuringCoroutine()
    {
        while (true)
        {
            float waitTime = Random.Range(minLuringInterval, maxLuringInterval);
            yield return new WaitForSeconds(waitTime);

            if (!luringSound.IsNull)
                RuntimeManager.PlayOneShotAttached(luringSound, gameObject);
            else
                Debug.LogWarning($"NixieSoundController on {gameObject.name}: 'luringSound' is not assigned.");
        }
    }

    private IEnumerator ProvocativeCoroutine()
    {
        while (true)
        {
            float waitTime = Random.Range(minProvocativeInterval, maxProvocativeInterval);
            yield return new WaitForSeconds(waitTime);

            if (!provocativeGruntSound.IsNull)
                RuntimeManager.PlayOneShotAttached(provocativeGruntSound, gameObject);
            else
                Debug.LogWarning($"NixieSoundController on {gameObject.name}: 'provocativeGruntSound' is not assigned.");
        }
    }
}