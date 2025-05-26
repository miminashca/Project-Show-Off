using System;
using UnityEngine;

public static class HunterEventBus
{
    public static event Action<Vector3> OnPlayerShouted;
    public static event Action<GameObject> OnHunterSpottedPlayer;
    public static event Action OnHunterFiredShot;
    public static event Action OnHunterStartedAiming;

    public static void PlayerShouted(Vector3 position) => OnPlayerShouted?.Invoke(position);
    public static void HunterSpottedPlayer(GameObject player) => OnHunterSpottedPlayer?.Invoke(player);
    public static void HunterFiredShot() => OnHunterFiredShot?.Invoke();
    public static void HunterStartedAiming() => OnHunterStartedAiming?.Invoke();
}