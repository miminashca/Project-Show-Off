using System;
using UnityEngine;

public class LanternParts : MonoBehaviour
{
    public PhysicsLanternSway swayScript;
    public Rigidbody handleRigidbody;
    public Rigidbody swingingLanternBodyRB;
    public Light[] lanternLights;
    public LightFlicker lightFlicker;
    public GameObject lanternVFXHolder;
}