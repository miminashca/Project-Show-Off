using System;

public class HemannekenEventBus
{
    public static event Action OnWaterTouch;
    public static event Action OnHemannekenAttached;
    public static event Action OnHemannekenDetached;
    public static event Action OnStartChase;
    public static event Action OnEndChase;
    public event Action OnRabbitHopStart;
    public event Action OnRabbitHopEnd;

    public static void AttachHemanneken() => OnHemannekenAttached?.Invoke();
    public static void DetachHemanneken() => OnHemannekenDetached?.Invoke();
    public static void TouchWater() => OnWaterTouch?.Invoke();
    public static void StartChase() => OnStartChase?.Invoke();
    public static void EndChase() => OnEndChase?.Invoke();
    public void RabbitEndHop() => OnRabbitHopEnd?.Invoke();
    public void RabbitStartHop() => OnRabbitHopStart?.Invoke();
}