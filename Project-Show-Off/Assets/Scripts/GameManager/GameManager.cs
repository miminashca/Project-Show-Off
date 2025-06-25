// GameManager.cs

using System;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public enum GameStartState
{
    Undecided,
    NewGame,
    Continue
}
    
public class GameManager : MonoBehaviour
{ 
    public static GameManager Instance { get; private set; }

    private const string SaveKey = "gameSaveData";

    // --- State & References ---
    private GameStartState startState = GameStartState.Undecided;
    private PlayerHealth playerHealth;
    private PlayerMovement playerMovement;
    private LanternController lanternController;
    private Transform playerTransform;
    private ClueEventManager clueManager;
    private PlayerInput playerInput;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if(!ClueEventManager.Instance) return;
        ClueEventManager.Instance.OnClueCollected -= SaveGame;
        ClueEventManager.Instance.OnClueSubmitted -= SaveGame;
    }

    // <<< NEW: This is the method that sets our state from the start menu >>>
    public void SetStartState(GameStartState state)
    {
        this.startState = state;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "LanaStartScene")
        {
            return;
        }

        // Start a coroutine to wait until the next frame, ensuring all Awake() methods have been called.
        StartCoroutine(InitializeGameAfterSceneLoad());
    }

    private IEnumerator InitializeGameAfterSceneLoad()
    {
        // Wait for one frame to allow all other objects to initialize.
        yield return null;

        Debug.Log($"Executing start choice: {startState}");

        if (!FindPlayerComponents())
        {
            Debug.LogError("Could not find player components on scene load. Aborting start logic.");
            yield break; // Stop the coroutine
        }
        
        ClueEventManager.Instance.OnClueCollected += SaveGame;
        ClueEventManager.Instance.OnClueSubmitted += SaveGame;
        
        switch (startState)
        {
            case GameStartState.NewGame:
                if (PlayerPrefs.HasKey(SaveKey))
                {
                    PlayerPrefs.DeleteKey(SaveKey);
                    PlayerPrefs.Save();
                    Debug.Log("Starting new game. Old save data cleared.");
                }
                clueManager.LoadClues(null, null);
                SetPlayerControl(true);
                break;

            case GameStartState.Continue:
                LoadGame();
                SetPlayerControl(true);
                break;

            case GameStartState.Undecided:
            default:
                Debug.LogWarning("Game scene loaded directly. Defaulting to a New Game state.");
                if (PlayerPrefs.HasKey(SaveKey)) PlayerPrefs.DeleteKey(SaveKey);
                clueManager.LoadClues(null, null);
                SetPlayerControl(true);
                break;
        }
        
        startState = GameStartState.Undecided;
    }
    private void SetPlayerControl(bool isEnabled)
    {
        // We find the playerInput here again because it's only available after the scene loads
        if (playerInput == null)
        {
            playerInput = new PlayerInput();
        }

        if (playerInput != null)
        {
            if (isEnabled) playerInput.Enable();
            else playerInput.Disable();
        } 
        else 
        {
            Debug.LogError("GameManager could not find PlayerInput component in the scene!");
        }
    }

    public void SaveGame()
    {
        if (!FindPlayerComponents()) return;
        
        PlayerData data = new PlayerData();
        data.woundLevel = playerHealth.CurrentWoundLevel;
        data.currentStamina = playerMovement.CurrentStamina;
        data.lanternFuel = lanternController.currentFuel;
        data.playerPosition = playerTransform.position;
        data.playerRotation = playerTransform.rotation;
        data.collectedClueIDs = clueManager.GetCollectedClueIDs();
        data.submittedClueIDs = clueManager.GetSubmittedClueIDs();

        string json = JsonUtility.ToJson(data, true);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
        Debug.Log($"<color=cyan>Game Saved!</color>");
    }

    public void LoadGame()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
        {
            Debug.LogWarning("Load Failed: No save data found.");
            return;
        }
        if (!FindPlayerComponents()) return;

        string json = PlayerPrefs.GetString(SaveKey);
        PlayerData data = JsonUtility.FromJson<PlayerData>(json);

        playerHealth.SetWoundLevel(data.woundLevel);
        playerMovement.SetStamina(data.currentStamina);
        lanternController.ApplyLoadedFuel(data.lanternFuel);
        clueManager.LoadClues(data.collectedClueIDs, data.submittedClueIDs);
        
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            playerTransform.position = data.playerPosition;
            playerTransform.rotation = data.playerRotation;
            cc.enabled = true;
        }
        else
        {
            playerTransform.position = data.playerPosition;
            playerTransform.rotation = data.playerRotation;
        }
        Debug.Log("<color=lime>Game Loaded Successfully!</color>");
    }

    private bool FindPlayerComponents()
    {
        playerHealth = FindObjectOfType<PlayerHealth>();
        playerMovement = FindObjectOfType<PlayerMovement>();
        lanternController = FindObjectOfType<LanternController>();
        clueManager = ClueEventManager.Instance; // Singleton is reliable
        playerInput = new PlayerInput();

        if (playerHealth != null)
        {
            playerTransform = playerHealth.transform;
        }

        return playerHealth != null && playerMovement != null && lanternController != null && playerTransform != null && clueManager != null && playerInput != null;
    }
}