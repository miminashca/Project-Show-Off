using UnityEngine;

public class ClosePanel : MonoBehaviour
{
    [SerializeField] private GameObject activePannel;
    

    private void closePanel()
    {
        activePannel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            closePanel();
        }
    }
}
