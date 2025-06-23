using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Rendering;


public class Hemannikenvignette : MonoBehaviour
{
    [SerializeField] private float vignetteIntensity; // attach post-processing vignette intensity
    PostProcessVolume postProcessingVolume; // reference to the PostProcessingVolume component
    Vignette vignette; // reference to the Vignette effect

    private void Start()
    {
        postProcessingVolume = GetComponent<PostProcessVolume>();
        postProcessingVolume.profile.TryGetSettings(out vignette);
        if (vignette == null)
        {
            Debug.LogError("Vignette effect not found in PostProcessVolume profile.");
            return;
        }
        else
        {
            vignette.enabled.Override(false); // disable vignette by default
        }
    }
    private void Update()
    {
        if (vignette != null)
        {
            vignette.intensity.Override(vignetteIntensity); // set vignette intensity
        }
    }
}
