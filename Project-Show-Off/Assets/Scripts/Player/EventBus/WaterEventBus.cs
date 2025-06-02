using System;

public static class WaterEventBus
{
    public static event Action OnPlayerSubmerge;
    public static event Action OnPlayerEmerge;

    public static void InvokeSubmerge()
    {
        OnPlayerSubmerge?.Invoke();
    }

    public static void InvokeEmerge()
    {
        OnPlayerEmerge?.Invoke();
    }
}