using UnityEngine;
using UnityEngine.UI;

public class fuelUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider FuelBar;

    [Header("References")]
    [SerializeField] private LanternController lanternController;

    void Start()
    {
        if (lanternController == null)
        {
            Debug.LogError("LanternController reference not set in fuelUI!", this);
            enabled = false;
            return;
        }

        if (FuelBar == null)
        {
            Debug.LogError("FuelBar Slider reference not set in fuelUI!", this);
            enabled = false;
            return;
        }

        FuelBar.minValue = 0f;
        FuelBar.maxValue = lanternController.maxFuel;
    }

    void Update()
    {
        if (lanternController != null && FuelBar != null)
        {
            FuelBar.value = lanternController.currentFuel;
        }
    }
}
