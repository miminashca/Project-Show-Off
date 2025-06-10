using UnityEngine;
using System.Collections;

public class TutorialLamp : MonoBehaviour
{

    [SerializeField] private GameObject BackgroundPanel;
    [SerializeField] private GameObject EquipPanel;
    [SerializeField] private GameObject ShinePanel;
    [SerializeField] private GameObject triggerArea;
    [SerializeField] private CanvasGroup BackgroundCanvas;
    [SerializeField] private CanvasGroup EquipCanvas;
    [SerializeField] private CanvasGroup ShineCanvas;
    private bool isEquiped = false;
    private bool isShining = false;
    private bool isFading = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        BackgroundCanvas.alpha = 0;
        EquipCanvas.alpha = 0;
        ShineCanvas.alpha = 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && !isEquiped)
        {
            StartCoroutine(FadeCanvasGroup(BackgroundCanvas, 0f, 1f, 0.5f));
            StartCoroutine(FadeCanvasGroup(EquipCanvas, 0f, 1f, 0.5f));
            Debug.Log("triggered");
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.F) && !isEquiped)
        {
            isEquiped = true;
            EquipPanel.SetActive(false);
            StartCoroutine(FadeCanvasGroup(ShineCanvas, 0f, 1f, 0.5f));
        }

        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.Mouse1) && !isShining)
        {
            isShining = true;
            StartCoroutine(FadeCanvasGroup(ShineCanvas, 1f, 0f, 0.5f));
            StartCoroutine(FadeCanvasGroup(BackgroundCanvas, 1f, 0f, 0.5f));
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            BackgroundCanvas.alpha = 0;
            EquipCanvas.alpha = 0;
            ShineCanvas.alpha = 0;
        }
    }

    // Update is called once per frame
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
    {
        isFading = true;
        float elapsed = 0f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        cg.alpha = end;

        if (end == 0f)
        {
            cg.interactable = false;
            cg.blocksRaycasts = false;
            cg.gameObject.SetActive(false);
        }

        isFading = false;
    }
}
