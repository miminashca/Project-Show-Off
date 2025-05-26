using System;

public static class HemannekenEventBus
{
    public static event Action OnHeyTriggered;
    public static event Action OnWaterTouch;
    public static event Action OnHemannekenAttached;
    public static event Action OnHemannekenDetached;
    public static event Action OnStartChase; // Consider if these are still needed or handled by states
    public static event Action OnEndChase;   // Consider if these are still needed or handled by states

    public static void TriggerHey() => OnHeyTriggered?.Invoke();
    public static void AttachHemanneken() => OnHemannekenAttached?.Invoke();
    public static void DetachHemanneken() => OnHemannekenDetached?.Invoke();
    public static void TouchWater() => OnWaterTouch?.Invoke();
    public static void StartChase() => OnStartChase?.Invoke();
    public static void EndChase() => OnEndChase?.Invoke();
}