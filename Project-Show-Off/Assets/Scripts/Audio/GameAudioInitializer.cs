using UnityEngine;
using FMODUnity;
// We still need FMOD.Studio for the EventInstance type.
using FMOD.Studio;

/// <summary>
/// This script runs once at the start of the game to create a smooth
/// audio fade-in for all sounds routed to the Master Bus in FMOD.
/// </summary>
public class GameAudioInitializer : MonoBehaviour
{
    // --- MODIFIED: Using the modern EventReference struct instead of the obsolete [EventRef] attribute.
    [SerializeField] private EventReference muteSnapshot;

    private EventInstance muteSnapshotInstance;

    void Start()
    {
        // Check if a snapshot has been assigned in the Inspector.
        if (muteSnapshot.IsNull)
        {
            Debug.LogWarning("<color=aqua>FMOD:</color> MuteOnStart snapshot is not assigned in GameAudioInitializer. No fade-in will occur.");
            return;
        }

        Debug.Log("<color=aqua>FMOD:</color> Initializing master bus audio fade-in.");

        // Create an instance of our snapshot using the EventReference.
        muteSnapshotInstance = RuntimeManager.CreateInstance(muteSnapshot);

        // Start the snapshot. Because its Attack time is 0,
        // this will instantly mute the Master Bus.
        muteSnapshotInstance.start();

        // --- MODIFIED: Explicitly specify FMOD.Studio.STOP_MODE to resolve the ambiguity.
        // STOP_MODE.ALLOWFADEOUT makes it use the "Release Time" we set in FMOD (3 seconds).
        // This is what creates the smooth fade-in.
        muteSnapshotInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

        // Release the snapshot instance from memory after it's done fading.
        // We can do this right away; FMOD will handle the fade and then clean up.
        muteSnapshotInstance.release();
    }
}