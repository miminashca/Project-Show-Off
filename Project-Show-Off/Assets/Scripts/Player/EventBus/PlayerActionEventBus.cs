using System;
using UnityEngine;

public static class PlayerActionEventBus
{
    public static event Action<Vector3> OnPlayerShouted;

    public static void PlayerShouted(Vector3 position)
    {
        Debug.Log($"PlayerActionEventBus: PlayerShouted event invoked from {position}");
        OnPlayerShouted?.Invoke(position);
    }
}