using UnityEngine;
using UnityEngine.UI;

public class SensitivitySettings : MonoBehaviour
{
    [Header("References")]
    //[SerializeField] private PlayerMovement playerMovement; //not sure if needed, a relic of the past
    [SerializeField] private CameraMovement cameraMovement;
    [SerializeField] private Slider sensitivitySlider;

    private const string SensitivityKey = "mouseSensitivity";

    void Start()
    {
        // Load saved sensitivity if it exists, otherwise use current slider value
        if (PlayerPrefs.HasKey(SensitivityKey))
        {
            LoadSensitivity();
        }
        else
        {
            SetSensitivity(); // Save current slider value as default
        }
    }

    public void SetSensitivity()
    {
        float sensitivity = sensitivitySlider.value;
        if (cameraMovement != null)
        {
            cameraMovement.mouseSensitivity = sensitivity;
        }

        // If playerMovement also uses sensitivity, apply it here:
        if (cameraMovement != null)
        {
            cameraMovement.mouseSensitivity = sensitivity;
        }

        PlayerPrefs.SetFloat(SensitivityKey, sensitivity);
        PlayerPrefs.Save();
    }

    private void LoadSensitivity()
    {
        float savedSensitivity = PlayerPrefs.GetFloat(SensitivityKey);
        sensitivitySlider.value = savedSensitivity;
        SetSensitivity();
    }
}
