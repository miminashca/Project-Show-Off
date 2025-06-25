using System;

/// <summary>
/// Static event bus for broadcasting Nixie-specific gameplay events.
/// This allows different systems (like UI, Audio, Player's Lantern) to react
/// to the Nixie's state without needing a direct reference to it.
/// </summary>
public static class NixieEventBus
{
    /// <summary>
    /// Fired when the Nixie enters its Chasing state.
    /// </summary>
    public static event Action OnNixieChaseStart;

    /// <summary>
    /// Fired when the Nixie exits its Chasing state for any reason.
    /// </summary>
    public static event Action OnNixieChaseEnd;

    public static void NotifyChaseStart() => OnNixieChaseStart?.Invoke();
    public static void NotifyChaseEnd() => OnNixieChaseEnd?.Invoke();
}