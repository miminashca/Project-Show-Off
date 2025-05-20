using UnityEngine;

public class ClueObject : MonoBehaviour
{
    [Header("Clue Properties")]
    public string clueID; // Unique identifier for this clue
    public string clueName = "Mysterious Object"; // Display name
    [TextArea]
    public string clueDescription = "An interesting object worth inspecting.";

    // Optional: If you want the object to have a specific orientation or scale when inspected
    public Vector3 inspectionRotationOffset = Vector3.zero;
    public float inspectionScaleFactor = 1f;

    private bool isInteractable = true;

    void Awake()
    {
        // ADDING HIGHLIGHT HERE COULD BE AN OPTION
    }

    public void SetInteractable(bool state)
    {
        isInteractable = state;
    }

    public bool IsInteractable()
    {
        return isInteractable;
    }

    public void Highlight(bool show)
    {

    }

    // Called by the InspectionManager when the clue is "collected"
    public void OnCollected()
    {
        Debug.Log($"Clue '{clueName}' ({clueID}) collected!");
        //ClueEventManager.Instance.RegisterClueCollected(clueID);
        Destroy(gameObject);
    }
}