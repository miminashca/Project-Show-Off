using System;

public static class HemannekenEventBus
{
    public static event Action OnWaterTouch;
    public static event Action OnHemannekenAttached;
    public static event Action OnHemannekenDetached;
    public static event Action OnStartChase;
    public static event Action OnEndChase;

    public static void AttachHemanneken() => OnHemannekenAttached?.Invoke();
    public static void DetachHemanneken() => OnHemannekenDetached?.Invoke();
    public static void TouchWater() => OnWaterTouch?.Invoke();
    public static void StartChase() => OnStartChase?.Invoke();
    public static void EndChase() => OnEndChase?.Invoke();
}