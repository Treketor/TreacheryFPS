using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Treachery.Weapons.Runtime;

/// <summary>
/// Controls the pause menu functionality - pausing gameplay, showing menu, and handling basic menu actions.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] GameObject pauseMenuPanel;
    [SerializeField] GameObject gameplayUI;
    
    [Header("Menu Buttons")]
    [SerializeField] Button resumeButton;
    [SerializeField] Button settingsButton;
    [SerializeField] Button restartButton;
    [SerializeField] Button mainMenuButton;
    [SerializeField] Button quitButton;
    
    [Header("Confirmation Dialog")]
    [SerializeField] GameObject confirmationPanel;
    [SerializeField] TMPro.TextMeshProUGUI confirmationText;
    [SerializeField] Button confirmYesButton;
    [SerializeField] Button confirmNoButton;
    
    [Header("Confirmation Messages")]
    [TextArea(3, 5)]
    [SerializeField] string restartConfirmationText = "Restart current run?\n\nAll progress will be lost!";
    [TextArea(3, 5)]
    [SerializeField] string quitConfirmationText = "Quit the game?\n\nCurrent progress will be lost!";
    
    [Header("Audio")]
    [SerializeField] AudioSource uiAudioSource;
    [SerializeField] AudioClip menuNavigateSound;
    [SerializeField] AudioClip menuSelectSound;
    
    [Header("Input")]
    [SerializeField] InputActionAsset inputActions;
    
    public static PauseMenuController Instance { get; private set; }
    
    private bool _isPaused = false;
    private InputAction _pauseAction;
    private System.Action _pendingConfirmAction;
    
    public bool IsPaused => _isPaused;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Make sure pause menu is hidden at start
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }
    
    void Start()
    {
        // Setup input actions
        if (inputActions != null)
        {
            _pauseAction = inputActions.FindAction("Pause");
            if (_pauseAction == null)
            {
                Debug.LogWarning("PauseMenuController: 'Pause' action not found in InputActionAsset!");
            }
        }
        else
        {
            Debug.LogWarning("PauseMenuController: No InputActionAsset assigned!");
        }

        // Setup button listeners
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartRun);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
            
        // Setup confirmation dialog buttons
        if (confirmYesButton != null)
            confirmYesButton.onClick.AddListener(ConfirmYes);
        if (confirmNoButton != null)
            confirmNoButton.onClick.AddListener(ConfirmNo);
    }
    
    void OnEnable()
    {
        // Enable input actions
        if (inputActions != null)
        {
            inputActions.Enable();
        }
        _pauseAction?.Enable();
    }

    void OnDisable()
    {
        // Disable input actions
        _pauseAction?.Disable();
        if (inputActions != null)
        {
            inputActions.Disable();
        }
    }

    void Update()
    {
        // Handle pause input
        if (_pauseAction != null && _pauseAction.WasPressedThisFrame())
        {
            if (_isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }
    
    public void PauseGame()
    {
        if (_isPaused) return;
        
        _isPaused = true;
        
        // Pause time
        Time.timeScale = 0f;
        
        // Show pause menu, hide gameplay UI
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
        if (gameplayUI != null)
            gameplayUI.SetActive(false);
        
        // Enable cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Disable player input systems
        DisableGameplayInput();
        
        // Play pause sound
        PlayMenuSound(menuSelectSound);
        
        // Focus on resume button
        if (resumeButton != null)
            resumeButton.Select();
        

    }
    
    public void ResumeGame()
    {
        if (!_isPaused) return;
        
        _isPaused = false;
        
        // Resume time
        Time.timeScale = 1f;
        
        // Hide pause menu, show gameplay UI
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        if (gameplayUI != null)
            gameplayUI.SetActive(true);
        
        // Lock cursor back to center
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Re-enable player input systems
        EnableGameplayInput();
        
        // Play resume sound
        PlayMenuSound(menuSelectSound);
        

    }
    
    void DisableGameplayInput()
    {
        // Disable weapon controller input (safer approach - just disable the component)
        var weaponController = FindFirstObjectByType<WeaponController>();
        if (weaponController != null)
        {
            weaponController.enabled = false;
        }

        var weaponControllerV2 = FindFirstObjectByType<WeaponControllerV2>();
        if (weaponControllerV2 != null)
        {
            weaponControllerV2.enabled = false;
        }
        
        // Disable other gameplay input components by finding common player scripts
        var playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            // Disable any PlayerInput components (New Input System)
            var playerInputs = playerObject.GetComponentsInChildren<PlayerInput>();
            foreach (var playerInput in playerInputs)
            {
                if (playerInput != null)
                    playerInput.enabled = false;
            }
            
            // Disable common movement/look components
            var allComponents = playerObject.GetComponentsInChildren<MonoBehaviour>();
            foreach (var component in allComponents)
            {
                var typeName = component.GetType().Name.ToLower();
                if (typeName.Contains("movement") || typeName.Contains("look") || typeName.Contains("controller"))
                {
                    component.enabled = false;
                }
            }
        }
    }
    
    void EnableGameplayInput()
    {
        // Re-enable weapon controller input
        var weaponController = FindFirstObjectByType<WeaponController>();
        if (weaponController != null)
        {
            weaponController.enabled = true;
        }

        var weaponControllerV2 = FindFirstObjectByType<WeaponControllerV2>();
        if (weaponControllerV2 != null)
        {
            weaponControllerV2.enabled = true;
        }
        
        // Re-enable other gameplay input components
        var playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            // Re-enable any PlayerInput components (New Input System)
            var playerInputs = playerObject.GetComponentsInChildren<PlayerInput>();
            foreach (var playerInput in playerInputs)
            {
                if (playerInput != null)
                    playerInput.enabled = true;
            }
            
            // Re-enable common movement/look components
            var allComponents = playerObject.GetComponentsInChildren<MonoBehaviour>();
            foreach (var component in allComponents)
            {
                var typeName = component.GetType().Name.ToLower();
                if (typeName.Contains("movement") || typeName.Contains("look") || typeName.Contains("controller"))
                {
                    component.enabled = true;
                }
            }
        }
    }
    
    void PlayMenuSound(AudioClip clip)
    {
        if (uiAudioSource != null && clip != null)
        {
            uiAudioSource.PlayOneShot(clip);
        }
    }
    
    // Button callback methods
    public void OpenSettings()
    {
        PlayMenuSound(menuNavigateSound);
        
        if (SettingsController.Instance != null)
        {
            SettingsController.Instance.OpenSettings();
        }
        else
        {
            Debug.LogWarning("PauseMenuController: SettingsController not found!");
        }
    }
    
    public void HidePauseMenuForSettings()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
    }
    
    public void ShowPauseMenuFromSettings()
    {
        // Only show pause menu if we're still in paused state
        if (_isPaused && pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            
            // Return focus to settings button
            if (settingsButton != null)
                settingsButton.Select();
        }
    }
    
    public void RestartRun()
    {
        PlayMenuSound(menuNavigateSound);
        ShowConfirmationDialog(restartConfirmationText, () => {
            Debug.Log("Restart confirmation - YES clicked");
            PlayMenuSound(menuSelectSound);
            
            // Properly reset pause state before scene reload
            _isPaused = false;
            Time.timeScale = 1f;
            
            // Hide all UI panels
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
            if (confirmationPanel != null)
                confirmationPanel.SetActive(false);
            if (gameplayUI != null)
                gameplayUI.SetActive(true);
            
            // Reset cursor state
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // Re-enable gameplay input before scene change
            EnableGameplayInput();
            
            // Get current scene info for debugging
            Scene currentScene = SceneManager.GetActiveScene();
            Debug.Log($"Attempting to restart scene: '{currentScene.name}' (Build Index: {currentScene.buildIndex})");
            
            // Try scene name first, then build index if that fails
            try
            {
                if (!string.IsNullOrEmpty(currentScene.name))
                {
                    SceneManager.LoadScene(currentScene.name);
                }
                else if (currentScene.buildIndex >= 0)
                {
                    SceneManager.LoadScene(currentScene.buildIndex);
                }
                else
                {
                    Debug.LogError("Cannot restart scene: Invalid scene name and build index!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to restart scene: {e.Message}");
            }
        });
    }
    
    public void ReturnToMainMenu()
    {
        PlayMenuSound(menuSelectSound);
        // TODO: Add confirmation dialog and scene loading
        Debug.Log("Return to main menu not yet implemented");
    }
    
    public void QuitGame()
    {
        PlayMenuSound(menuNavigateSound);
        ShowConfirmationDialog(quitConfirmationText, () => {
            Debug.Log("Quit confirmation - YES clicked");
            PlayMenuSound(menuSelectSound);
            
            Debug.Log("Quitting game...");
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        });
    }
    
    // Confirmation dialog methods
    void ShowConfirmationDialog(string message, System.Action onConfirm)
    {
        if (confirmationPanel == null) 
        {
            Debug.LogWarning("PauseMenuController: No confirmation panel assigned!");
            return;
        }
        
        _pendingConfirmAction = onConfirm;
        
        // Set the confirmation message
        if (confirmationText != null)
            confirmationText.text = message;
        
        // Hide pause menu and show confirmation dialog
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
            
        confirmationPanel.SetActive(true);
        
        // Focus on "No" button by default (safer choice)
        if (confirmNoButton != null)
            confirmNoButton.Select();
    }
    
    void HideConfirmationDialog()
    {
        Debug.Log("HideConfirmationDialog called");
        
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
            
        // Only restore pause menu if we're still paused (action might have changed game state)
        if (_isPaused && pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
            
        // Clear pending action if not already cleared
        if (_pendingConfirmAction != null)
            _pendingConfirmAction = null;
        
        // Return focus to resume button (default selection) only if still paused
        if (_isPaused && resumeButton != null)
            resumeButton.Select();
    }
    
    public void ConfirmYes()
    {
        Debug.Log("ConfirmYes called");
        PlayMenuSound(menuSelectSound);
        
        // Execute the pending action BEFORE hiding the dialog
        var actionToExecute = _pendingConfirmAction;
        _pendingConfirmAction = null; // Clear it first to prevent issues
        
        HideConfirmationDialog();
        
        // Execute the action after UI cleanup
        actionToExecute?.Invoke();
    }
    
    public void ConfirmNo()
    {
        Debug.Log("ConfirmNo called");
        PlayMenuSound(menuNavigateSound);
        HideConfirmationDialog();
    }
    
    void OnDestroy()
    {
        // Clean up button listeners
        if (resumeButton != null)
            resumeButton.onClick.RemoveAllListeners();
        if (settingsButton != null)
            settingsButton.onClick.RemoveAllListeners();
        if (restartButton != null)
            restartButton.onClick.RemoveAllListeners();
        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveAllListeners();
        if (quitButton != null)
            quitButton.onClick.RemoveAllListeners();
        if (confirmYesButton != null)
            confirmYesButton.onClick.RemoveAllListeners();
        if (confirmNoButton != null)
            confirmNoButton.onClick.RemoveAllListeners();
    }
}