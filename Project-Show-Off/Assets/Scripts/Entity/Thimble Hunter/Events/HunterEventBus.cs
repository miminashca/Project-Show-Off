using System;
using UnityEngine;

public static class HunterEventBus
{
    public static event Action<Vector3> OnHunterHeardPlayer;
    public static event Action<GameObject> OnHunterSpottedPlayer;
    public static event Action OnHunterFiredShot;
    public static event Action OnHunterStartedAiming;

    public static void HunterHeardPlayer(Vector3 position) => OnHunterHeardPlayer?.Invoke(position);
    public static void HunterSpottedPlayer(GameObject player) => OnHunterSpottedPlayer?.Invoke(player);
    public static void HunterFiredShot() => OnHunterFiredShot?.Invoke();
    public static void HunterStartedAiming() => OnHunterStartedAiming?.Invoke();
}