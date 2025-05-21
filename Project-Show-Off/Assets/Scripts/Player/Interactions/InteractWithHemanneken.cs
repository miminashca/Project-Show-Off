using UnityEngine;

public class InteractWithHemanneken : MonoBehaviour
{
    private PlayerInput controls;
    private void OnEnable()
    {
        controls = new PlayerInput();
        controls.Enable();
    }
    private void OnDisable()
    {
        controls.Disable();
    }
    private void Update()
    {
        if (controls.Hemanneken.Hey.triggered) HemannekenEventBus.TriggerHey();
    }
}
