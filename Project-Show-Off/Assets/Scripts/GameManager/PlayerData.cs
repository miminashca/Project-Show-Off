using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PlayerData
{
    // Player Status
    public int woundLevel;
    public float currentStamina;
    public Vector3 playerPosition;
    public Quaternion playerRotation;

    // Lantern Status
    public float lanternFuel;

    // Progression
    public List<string> collectedClueIDs;
    public List<string> submittedClueIDs;

    // You can add more data here in the future!
    // For example:
    // public bool hasTransformed; 
}