using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class WLSoundController : MonoBehaviour
{
    [SerializeField] private EventReference WLKillNoise;
    [SerializeField] private EventReference WLLullaby;

    private EventInstance killNoiseInstance;
    private EventInstance lullabyInstance;

    public void StartKillNoise()
    {
        if (IsInstancePlaying(killNoiseInstance))
        {
            return;
        }

        if (!WLKillNoise.IsNull)
        {
            killNoiseInstance = RuntimeManager.CreateInstance(WLKillNoise);
            RuntimeManager.AttachInstanceToGameObject(killNoiseInstance, gameObject);
            killNoiseInstance.start();
        }
    }

    public void StopKillNoise()
    {
        if (killNoiseInstance.isValid())
        {
            // FIX 1: Specify the FMOD.Studio namespace to resolve ambiguity.
            killNoiseInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            killNoiseInstance.release();
        }
    }

    public void PlayLullaby()
    {
        if (IsInstancePlaying(lullabyInstance))
        {
            return;
        }

        if (!WLLullaby.IsNull)
        {
            lullabyInstance = RuntimeManager.CreateInstance(WLLullaby);
            RuntimeManager.AttachInstanceToGameObject(lullabyInstance, gameObject);
            lullabyInstance.start();
            lullabyInstance.release();
        }
    }

    private bool IsInstancePlaying(EventInstance instance)
    {
        if (!instance.isValid())
        {
            return false;
        }

        instance.getPlaybackState(out PLAYBACK_STATE state);
        return state != PLAYBACK_STATE.STOPPED;
    }

    private void OnDestroy()
    {
        if (killNoiseInstance.isValid())
        {
            // FIX 2: Specify the FMOD.Studio namespace here as well.
            killNoiseInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            killNoiseInstance.release();
        }

        if (lullabyInstance.isValid())
        {
            // FIX 3: And specify it here for the lullaby's cleanup.
            lullabyInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            lullabyInstance.release();
        }
    }
}