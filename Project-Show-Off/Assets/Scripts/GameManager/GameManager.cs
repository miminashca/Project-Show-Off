using System;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

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
    private HeadbobController headBob; // Assuming you have a HeadbobController script
    [NonSerialized] public Transform PlayerTransform;
    private ClueEventManager clueManager;

    private GameObject pauseScreenUI;
    private GameObject deathScreenUI;
    
    private CameraMovement cameraMovement;
    //private HeadbobController headbobController;
    
    public static bool IsGamePaused { get; private set; }
     

    //probably have to move to game state manager in future...
    [NonSerialized] public bool isWhiteLadyActive = false;

    public event Action OnGameLoaded;
    private StartMenuManager StartMenuManager;
    private HunterActivationManager hunterActivationManager;

    private void Start()
    {
        // NO NEED TO PARENT OTHER MANAGERS TO GAME MANAGER!!!
        // StartMenuManager = GetComponentInChildren<StartMenuManager>();  
    }

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
            return; // Exit if another instance already exists
        }
        //// <<< NEW: Ensure UI is disabled when the game manager is first created >>>
        //pauseScreenUI = GetComponent<UIpauseMarker>()?.gameObject;
        //deathScreenUI = GetComponent<UIdeathMarker>()?.gameObject;  
        //if (pauseScreenUI != null) pauseScreenUI.SetActive(false);
        //if (deathScreenUI != null) deathScreenUI.SetActive(false);
    }

    private void Update()
    {
        // Don't allow pausing if the death screen is active or if we are in the main menu
        if (deathScreenUI != null && deathScreenUI.activeSelf || SceneManager.GetActiveScene().name == "StarScene")
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
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
    
    public void SetStartState(GameStartState state)
    {
        this.startState = state;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        IsGamePaused = false; // reset pause state when a new scene is loaded

        if (scene.name == "LanaStartScene" || scene.name == "StartScreen")
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
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

        if (!FindSceneReferences())
        {
            Debug.LogError("Could not find player components on scene load. Aborting start logic.");
            yield break; // Stop the coroutine
        }

        if (pauseScreenUI != null) pauseScreenUI.SetActive(false);
        if (deathScreenUI != null) deathScreenUI.SetActive(false);

        if (playerHealth == null) // A simple check to see if references were found
        {
            Debug.LogError("Could not find player components on scene load. Aborting start logic.");
            yield break;
        }

        OnGameLoaded?.Invoke();
        
        // Unsubscribe first to prevent double-subscription if the scene is ever reloaded
        if (clueManager != null)
        {
            clueManager.OnClueCollected -= SaveGame;
            clueManager.OnClueSubmitted -= SaveGame;
        }

        // Subscribe to the correct, unified events
        clueManager.OnClueCollected += SaveGame;
        clueManager.OnClueSubmitted += SaveGame;

        if (hunterActivationManager == null)
        {
            Debug.LogWarning("Could not find HunterActivationManager in the scene. State will not be saved/loaded.");
        }

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
                if (hunterActivationManager != null) hunterActivationManager.InitializeState(false);
                SetPlayerInputActive(true);
                break;

            case GameStartState.Continue:
                LoadGame();
                //SetPlayerControl(true);
                SetPlayerInputActive(true);
                break;

            case GameStartState.Undecided:
            default:
                Debug.LogWarning("Game scene loaded directly. Defaulting to a New Game state.");
                if (PlayerPrefs.HasKey(SaveKey)) PlayerPrefs.DeleteKey(SaveKey);
                clueManager.LoadClues(null, null);
                if (hunterActivationManager != null) hunterActivationManager.InitializeState(false);
                SetPlayerInputActive(true);
                break;
        }
        
        startState = GameStartState.Undecided;
    }

    // <<< NEW: Renamed and expanded from SetPlayerControl to handle all components for pausing >>>
    private void SetPlayerInputActive(bool isActive)
    {
        if (playerMovement != null) playerMovement.enabled = isActive;
        if (lanternController != null) lanternController.enabled = isActive;
        if (cameraMovement != null) cameraMovement.enabled = isActive;
        if (headBob != null) headBob.enabled = isActive; 

        Debug.Log($"Player controls set to: {isActive}");

        // Also manage cursor state here
        if (isActive)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void TogglePause()
    {
        IsGamePaused = !IsGamePaused;
        if (IsGamePaused)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
    }

    public void PauseGame()
    {
        IsGamePaused = true;
        Time.timeScale = 0f;
        SetPlayerInputActive(false);
        if (pauseScreenUI != null) pauseScreenUI.SetActive(true);
    }

    public void ResumeGame()
    {
        IsGamePaused = false;
        Time.timeScale = 1f;
        SetPlayerInputActive(true);
        if (pauseScreenUI != null) pauseScreenUI.SetActive(false);
    }

    public void PlayerDied()
    {
        SetPlayerInputActive(false); // 1. Disable controls and show cursor.
        if (deathScreenUI != null)   // 2. Activate the death screen.
        {
            deathScreenUI.SetActive(true);
        }
        else
        {
            Debug.LogError("PlayerDied was called, but deathScreenUI is null! Make sure the DeathPanel is active in the scene and has the UIdeathMarker component.");
        }

        // These two lines manage the game state.
        IsGamePaused = true;         // 3. Set the state flag.
        Time.timeScale = 0f;         // 4. Freeze time.
    }
    
    public void Retry()
    {
        Time.timeScale = 1f;

        SetStartState(GameStartState.Continue);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        // Always reset time scale before leaving a scene.
        Time.timeScale = 1f;
        SceneManager.LoadScene("StartScreen"); // Make sure you have this scene in your build settings
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void SetPlayerControl(bool isEnabled)
    {
        // The player components themselves manage their own input instances.
        // We just need to enable/disable the components.
        if (playerMovement != null) playerMovement.enabled = isEnabled;
    
        // Find other input-driven components and enable/disable them too
        var lantern = FindFirstObjectByType<LanternController>();
        if (lantern != null) lantern.enabled = isEnabled;
    
        var playerController = FindFirstObjectByType<PlayerMovement>(); // Assuming you have a script like this
        if (playerController != null) playerController.enabled = isEnabled;
        
        var cameraController = FindFirstObjectByType<CameraMovement>(); // Assuming you have a script like this
        if (cameraController != null) cameraController.enabled = isEnabled;

        Debug.Log($"Player controls set to: {isEnabled}");
    }

    public void SaveGame()
    {
        if (!FindSceneReferences()) return;
        
        PlayerData data = new PlayerData();
        data.woundLevel = playerHealth.CurrentWoundLevel;
        data.currentStamina = playerMovement.CurrentStamina;
        data.lanternFuel = lanternController.currentFuel;
        data.playerPosition = PlayerTransform.position;
        data.playerRotation = PlayerTransform.rotation;
        data.collectedClueIDs = clueManager.GetCollectedClueIDs();
        data.submittedClueIDs = clueManager.GetSubmittedClueIDs();

        if (hunterActivationManager != null) data.isHunterActivated = hunterActivationManager.IsHunterActive();

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
        if (!FindSceneReferences()) return;

        string json = PlayerPrefs.GetString(SaveKey);
        PlayerData data = JsonUtility.FromJson<PlayerData>(json);

        playerHealth.SetWoundLevel(data.woundLevel);
        playerMovement.SetStamina(data.currentStamina);
        lanternController.ApplyLoadedFuel(data.lanternFuel);
        clueManager.LoadClues(data.collectedClueIDs, data.submittedClueIDs);

        if (hunterActivationManager != null) hunterActivationManager.InitializeState(data.isHunterActivated);

        CharacterController cc = PlayerTransform.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            PlayerTransform.position = data.playerPosition;
            PlayerTransform.rotation = data.playerRotation;
            cc.enabled = true;
        }
        else
        {
            PlayerTransform.position = data.playerPosition;
            PlayerTransform.rotation = data.playerRotation;
        }
        Debug.Log("<color=lime>Game Loaded Successfully!</color>");
    }

    //old
    //private bool FindPlayerComponents()
    //{
    //    playerHealth = FindFirstObjectByType<PlayerHealth>();
    //    playerMovement = FindFirstObjectByType<PlayerMovement>();
    //    lanternController = FindFirstObjectByType<LanternController>();
    //    clueManager = ClueEventManager.Instance; // Singleton is reliable

    //    // <<< NEW: Find extra components here >>>
    //    cameraMovement = FindFirstObjectByType<CameraMovement>();
    //    // headbobController = FindFirstObjectByType<HeadbobController>(); // Uncomment if you have this

    //    if (playerHealth != null)
    //    {
    //        PlayerTransform = playerHealth.transform;
    //    }

    //    // Only the core components are essential for the game to run.
    //    // UI-related components can be null-checked later.
    //    return playerHealth != null && playerMovement != null && lanternController != null && PlayerTransform != null && clueManager != null;
    //}

    private bool FindSceneReferences()
    {
        Debug.Log("GameManager finding scene references...");

        // Find Player Components
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        lanternController = FindFirstObjectByType<LanternController>();
        cameraMovement = FindFirstObjectByType<CameraMovement>();
        headBob = FindFirstObjectByType<HeadbobController>(); // Assuming you have a Camera component on the player
        hunterActivationManager = FindFirstObjectByType<HunterActivationManager>();

        clueManager = ClueEventManager.Instance; // Singleton is reliable

        if (playerHealth != null)
        {
            PlayerTransform = playerHealth.transform;
        }

        // Find UI Components using our markers
        // Note: I'm using the names from your script now, e.g., UIpauseMarker
        UIpauseMarker pauseMarker = FindFirstObjectByType<UIpauseMarker>();
        if (pauseMarker != null)
        {
            pauseScreenUI = pauseMarker.gameObject;
            Debug.Log("Found Pause Screen UI.");
        }
        else
        {
            // This is not a critical error, the game can run without a pause screen
            Debug.LogWarning("Could not find object with UIpauseMarker component in the scene.");
        }

        UIdeathMarker deathMarker = FindFirstObjectByType<UIdeathMarker>();
        if (deathMarker != null)
        {
            deathScreenUI = deathMarker.gameObject;
            Debug.Log("Found Death Screen UI.");
        }
        else
        {
            Debug.LogWarning("Could not find object with UIdeathMarker component in the scene.");
        }

        // This is the important part. We return true only if the CORE components are found.
        // The game cannot run without these. UI is optional.
        return playerHealth != null && playerMovement != null && lanternController != null && PlayerTransform != null && clueManager != null;
    }

}