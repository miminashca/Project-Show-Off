using System;
using UnityEngine;
using UnityEngine.VFX; // Make sure to include this namespace!

public class FirefliesVfxController : MonoBehaviour
{
    private VisualEffect fireflyVFX;

    private void Awake()
    {
        fireflyVFX = GetComponent<VisualEffect>();
    }

    public void TurnOff()
    {
        if (fireflyVFX == null)
        {
            Debug.LogError("VFX reference is not set! Please assign it in the Inspector.", this.gameObject);
            return; // Stop the function if the reference is missing.
        }

        fireflyVFX.SetBool("DisableSpawn", false);
        fireflyVFX.SetFloat("AmountFireflies", 0f);

        Debug.Log("VFX parameters have been set: DisableSpawn to false, AmountFireflies to 0.");
    }
}