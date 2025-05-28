using UnityEngine;
using TMPro;

public class ClueCounterUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI clueCounterText;

    private void Start()
    {
        if (clueCounterText == null)
        {
            Debug.LogError("ClueCounterUI: TextMeshProUGUI reference is missing!");
            return;
        }

        // Subscribe to the event
        if (InspectionManager.Instance != null)
        {
            InspectionManager.Instance.OnClueCollected += UpdateClueCounter;
        }

        UpdateClueCounter(0); // Initialize with 0
    }

    private void OnDestroy()
    {
        if (InspectionManager.Instance != null)
        {
            InspectionManager.Instance.OnClueCollected -= UpdateClueCounter;
        }
    }

    private void UpdateClueCounter(int count)
    {
        clueCounterText.text = $"{count}";
    }
}