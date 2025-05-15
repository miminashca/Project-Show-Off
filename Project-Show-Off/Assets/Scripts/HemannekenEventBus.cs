using System;
using UnityEngine;

public static class HemannekenEventBus
{
    public static event Action HeyTriggered;

    public static void TriggerHey()
    {
        HeyTriggered?.Invoke();
    }
    
}
