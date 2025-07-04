using UnityEngine;
using FMODUnity;
using FMOD.Studio; // Required for EventInstance and PLAYBACK_STATE

public class WLSoundController : MonoBehaviour
{
    [SerializeField] private EventReference WLKillNoise;
    [SerializeField] private EventReference WLLullaby;

    // 1. To hold a reference to our playing lullaby sound
    private EventInstance lullabyInstance;

    public void PlayKillNoise()
    {
        if (!WLKillNoise.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(WLKillNoise, gameObject);
        }
    }

    public void PlayLullaby()
    {
        // 3. Check if the lullaby is already playing before starting a new one
        if (IsInstancePlaying(lullabyInstance))
        {
            // Optional: log that we are preventing a replay
            // Debug.Log("Lullaby is already playing. Not starting a new one.");
            return;
        }

        // If the event is not null and not already playing, create and play it
        if (!WLLullaby.IsNull)
        {
            // Create an instance of the event
            lullabyInstance = RuntimeManager.CreateInstance(WLLullaby);

            // Attach it to this GameObject for 3D spatialization
            RuntimeManager.AttachInstanceToGameObject(lullabyInstance, gameObject);

            // Start the sound
            lullabyInstance.start();

            // IMPORTANT: Release the instance. This tells FMOD it can free up this event
            // from memory once it has finished playing. If you don't do this,
            // you will have a memory leak.
            lullabyInstance.release();
        }
    }

    // 2. A helper function to check the playback state of any EventInstance
    private bool IsInstancePlaying(EventInstance instance)
    {
        // The instance must be valid to get its state
        if (!instance.isValid())
        {
            return false;
        }

        // Get the current playback state
        instance.getPlaybackState(out PLAYBACK_STATE state);

        // Return true if the state is not STOPPED
        return state != PLAYBACK_STATE.STOPPED;
    }

    // Optional: It's good practice to stop any running instances when the object is destroyed
    // to prevent orphaned sounds.
    private void OnDestroy()
    {
        if (lullabyInstance.isValid())
        {
            lullabyInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            lullabyInstance.release();
        }
    }
}