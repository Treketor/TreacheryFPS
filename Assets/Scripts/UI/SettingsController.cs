using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Handles all game settings including audio, video, controls, and gameplay options.
/// Settings are automatically saved to PlayerPrefs and loaded on startup.
/// </summary>
public class SettingsController : MonoBehaviour
{
    [Header("Settings Panels")]
    [SerializeField] GameObject settingsPanel;
    [SerializeField] GameObject audioPanel;
    [SerializeField] GameObject videoPanel;
    [SerializeField] GameObject controlsPanel;
    [SerializeField] GameObject gameplayPanel;
    
    [Header("Tab Buttons")]
    [SerializeField] Button audioTabButton;
    [SerializeField] Button videoTabButton;
    [SerializeField] Button controlsTabButton;
    [SerializeField] Button gameplayTabButton;
    [SerializeField] Button backButton;
    
    [Header("Audio Settings")]
    [SerializeField] AudioMixer audioMixer;
    [SerializeField] Slider masterVolumeSlider;
    [SerializeField] Slider musicVolumeSlider;
    [SerializeField] Slider sfxVolumeSlider;
    [SerializeField] Slider uiVolumeSlider;
    [SerializeField] TextMeshProUGUI masterVolumeText;
    [SerializeField] TextMeshProUGUI musicVolumeText;
    [SerializeField] TextMeshProUGUI sfxVolumeText;
    [SerializeField] TextMeshProUGUI uiVolumeText;
    
    [Header("Video Settings")]
    [SerializeField] TMP_Dropdown resolutionDropdown;
    [SerializeField] TMP_Dropdown qualityDropdown;
    [SerializeField] TMP_Dropdown windowModeDropdown;
    [SerializeField] Slider mainFOVSlider;
    [SerializeField] Slider weaponFOVSlider;
    [SerializeField] TextMeshProUGUI mainFOVText;
    [SerializeField] TextMeshProUGUI weaponFOVText;
    [SerializeField] Toggle vsyncToggle;
    [SerializeField] Toggle showFPSToggle;
    
    [Header("Controls Settings")]
    [SerializeField] Slider mouseSensitivitySlider;
    [SerializeField] TextMeshProUGUI mouseSensitivityText;
    [SerializeField] Toggle invertYAxisToggle;
    [SerializeField] Transform keybindContainer;
    [SerializeField] GameObject keybindItemPrefab;
    
    [Header("Gameplay Settings")]
    [SerializeField] TMP_Dropdown uiScaleDropdown;
    [SerializeField] TMP_Dropdown crosshairDropdown;
    [SerializeField] Toggle showDamageNumbersToggle;
    [SerializeField] Toggle autoCollectSoulsToggle;
    [SerializeField] Slider uiOpacitySlider;
    [SerializeField] TextMeshProUGUI uiOpacityText;
    
    [Header("Audio")]
    [SerializeField] AudioSource uiAudioSource;
    [SerializeField] AudioClip menuNavigateSound;
    [SerializeField] AudioClip menuSelectSound;
    
    // Current settings tab
    private SettingsTab _currentTab = SettingsTab.Audio;
    
    // Resolution data
    private Resolution[] _resolutions;
    private List<Resolution> _filteredResolutions = new List<Resolution>();
    
    public static SettingsController Instance { get; private set; }
    
    public enum SettingsTab
    {
        Audio,
        Video,
        Controls,
        Gameplay
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
            return;
        }
    }
    
    void Start()
    {
        SetupResolutions();
        SetupQualitySettings();
        SetupWindowModes();
        SetupUIScaleOptions();
        SetupCrosshairOptions();
        SetupButtonListeners();
        LoadAllSettings();
        
        // Hide settings panel initially
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }
    
    void SetupButtonListeners()
    {
        if (audioTabButton != null)
            audioTabButton.onClick.AddListener(() => SwitchTab(SettingsTab.Audio));
        if (videoTabButton != null)
            videoTabButton.onClick.AddListener(() => SwitchTab(SettingsTab.Video));
        if (controlsTabButton != null)
            controlsTabButton.onClick.AddListener(() => SwitchTab(SettingsTab.Controls));
        if (gameplayTabButton != null)
            gameplayTabButton.onClick.AddListener(() => SwitchTab(SettingsTab.Gameplay));
        if (backButton != null)
            backButton.onClick.AddListener(CloseSettings);
            
        // Audio sliders
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        if (uiVolumeSlider != null)
            uiVolumeSlider.onValueChanged.AddListener(SetUIVolume);
            
        // Video controls
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(SetQuality);
        if (windowModeDropdown != null)
            windowModeDropdown.onValueChanged.AddListener(SetWindowMode);
        if (mainFOVSlider != null)
            mainFOVSlider.onValueChanged.AddListener(SetMainFOV);
        if (weaponFOVSlider != null)
            weaponFOVSlider.onValueChanged.AddListener(SetWeaponFOV);
        if (vsyncToggle != null)
            vsyncToggle.onValueChanged.AddListener(SetVSync);
        if (showFPSToggle != null)
            showFPSToggle.onValueChanged.AddListener(SetShowFPS);
            
        // Controls
        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSensitivity);
        if (invertYAxisToggle != null)
            invertYAxisToggle.onValueChanged.AddListener(SetInvertYAxis);
            
        // Gameplay
        if (uiScaleDropdown != null)
            uiScaleDropdown.onValueChanged.AddListener(SetUIScale);
        if (crosshairDropdown != null)
            crosshairDropdown.onValueChanged.AddListener(SetCrosshair);
        if (showDamageNumbersToggle != null)
            showDamageNumbersToggle.onValueChanged.AddListener(SetShowDamageNumbers);
        if (autoCollectSoulsToggle != null)
            autoCollectSoulsToggle.onValueChanged.AddListener(SetAutoCollectSouls);
        if (uiOpacitySlider != null)
            uiOpacitySlider.onValueChanged.AddListener(SetUIOpacity);
    }
    
    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            SwitchTab(SettingsTab.Audio); // Default to audio tab
        }
        
        // Hide pause menu when settings is open
        if (PauseMenuController.Instance != null)
        {
            PauseMenuController.Instance.HidePauseMenuForSettings();
        }
        
        PlayMenuSound(menuSelectSound);
    }
    
    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
            
        // Restore pause menu when settings closes
        if (PauseMenuController.Instance != null)
        {
            PauseMenuController.Instance.ShowPauseMenuFromSettings();
        }
        
        PlayMenuSound(menuNavigateSound);
    }
    
    public void SwitchTab(SettingsTab tab)
    {
        _currentTab = tab;
        
        // Hide all panels
        if (audioPanel != null) audioPanel.SetActive(false);
        if (videoPanel != null) videoPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (gameplayPanel != null) gameplayPanel.SetActive(false);
        
        // Show selected panel
        switch (tab)
        {
            case SettingsTab.Audio:
                if (audioPanel != null) audioPanel.SetActive(true);
                break;
            case SettingsTab.Video:
                if (videoPanel != null) videoPanel.SetActive(true);
                break;
            case SettingsTab.Controls:
                if (controlsPanel != null) controlsPanel.SetActive(true);
                break;
            case SettingsTab.Gameplay:
                if (gameplayPanel != null) gameplayPanel.SetActive(true);
                break;
        }
        
        PlayMenuSound(menuNavigateSound);
    }
    
    // === AUDIO SETTINGS ===
    public void SetMasterVolume(float value)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("MasterVolume", Mathf.Log10(value) * 20);
        if (masterVolumeText != null)
            masterVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        PlayerPrefs.SetFloat("MasterVolume", value);
    }
    
    public void SetMusicVolume(float value)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("MusicVolume", Mathf.Log10(value) * 20);
        if (musicVolumeText != null)
            musicVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        PlayerPrefs.SetFloat("MusicVolume", value);
    }
    
    public void SetSFXVolume(float value)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20);
        if (sfxVolumeText != null)
            sfxVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        PlayerPrefs.SetFloat("SFXVolume", value);
    }
    
    public void SetUIVolume(float value)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("UIVolume", Mathf.Log10(value) * 20);
        if (uiVolumeText != null)
            uiVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        PlayerPrefs.SetFloat("UIVolume", value);
    }
    
    // === VIDEO SETTINGS ===
    void SetupResolutions()
    {
        _resolutions = Screen.resolutions;
        _filteredResolutions.Clear();
        
        // Filter out duplicate resolutions with different refresh rates
        var currentRefreshRatio = Screen.currentResolution.refreshRateRatio;
        foreach (var resolution in _resolutions)
        {
            if (Mathf.Approximately((float)resolution.refreshRateRatio.value, (float)currentRefreshRatio.value))
                _filteredResolutions.Add(resolution);
        }
        
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            List<string> options = new List<string>();
            int currentResolutionIndex = 0;
            
            for (int i = 0; i < _filteredResolutions.Count; i++)
            {
                string option = $"{_filteredResolutions[i].width} x {_filteredResolutions[i].height}";
                options.Add(option);
                
                if (_filteredResolutions[i].width == Screen.width && 
                    _filteredResolutions[i].height == Screen.height)
                {
                    currentResolutionIndex = i;
                }
            }
            
            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
        }
    }
    
    void SetupQualitySettings()
    {
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
            qualityDropdown.value = QualitySettings.GetQualityLevel();
        }
    }
    
    void SetupWindowModes()
    {
        if (windowModeDropdown != null)
        {
            windowModeDropdown.ClearOptions();
            windowModeDropdown.AddOptions(new List<string> { "Fullscreen", "Windowed", "Borderless" });
            
            if (Screen.fullScreen)
            {
                windowModeDropdown.value = Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen ? 0 : 2;
            }
            else
            {
                windowModeDropdown.value = 1;
            }
        }
    }
    
    void SetupUIScaleOptions()
    {
        if (uiScaleDropdown != null)
        {
            uiScaleDropdown.ClearOptions();
            uiScaleDropdown.AddOptions(new List<string> { "Small", "Medium", "Large" });
            uiScaleDropdown.value = 1; // Default to medium
        }
    }
    
    void SetupCrosshairOptions()
    {
        if (crosshairDropdown != null)
        {
            crosshairDropdown.ClearOptions();
            crosshairDropdown.AddOptions(new List<string> { "Cross", "Dot", "Circle", "None" });
            crosshairDropdown.value = 0; // Default to cross
        }
    }
    
    public void SetResolution(int resolutionIndex)
    {
        if (resolutionIndex >= 0 && resolutionIndex < _filteredResolutions.Count)
        {
            Resolution resolution = _filteredResolutions[resolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
            PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
        }
    }
    
    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("QualityLevel", qualityIndex);
    }
    
    public void SetWindowMode(int modeIndex)
    {
        switch (modeIndex)
        {
            case 0: // Fullscreen
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                Screen.fullScreen = true;
                break;
            case 1: // Windowed
                Screen.fullScreen = false;
                break;
            case 2: // Borderless
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                Screen.fullScreen = true;
                break;
        }
        PlayerPrefs.SetInt("WindowMode", modeIndex);
    }
    
    public void SetMainFOV(float value)
    {
        if (mainFOVText != null)
            mainFOVText.text = $"{Mathf.RoundToInt(value)}°";
        
        // Apply to main camera
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            mainCamera.fieldOfView = value;
            
        PlayerPrefs.SetFloat("MainFOV", value);
    }
    
    public void SetWeaponFOV(float value)
    {
        if (weaponFOVText != null)
            weaponFOVText.text = $"{Mathf.RoundToInt(value)}°";
        
        // Apply to weapon camera (if exists)
        // This would need to be connected to your weapon camera system
        
        PlayerPrefs.SetFloat("WeaponFOV", value);
    }
    
    public void SetVSync(bool enabled)
    {
        QualitySettings.vSyncCount = enabled ? 1 : 0;
        PlayerPrefs.SetInt("VSync", enabled ? 1 : 0);
    }
    
    public void SetShowFPS(bool enabled)
    {
        // This would need to be connected to your FPS display system
        PlayerPrefs.SetInt("ShowFPS", enabled ? 1 : 0);
    }
    
    // === CONTROLS SETTINGS ===
    public void SetMouseSensitivity(float value)
    {
        if (mouseSensitivityText != null)
            mouseSensitivityText.text = value.ToString("F1");
        
        // Apply to player look script
        // This would need to be connected to your FirstPersonLook script
        
        PlayerPrefs.SetFloat("MouseSensitivity", value);
    }
    
    public void SetInvertYAxis(bool inverted)
    {
        PlayerPrefs.SetInt("InvertYAxis", inverted ? 1 : 0);
    }
    
    // === GAMEPLAY SETTINGS ===
    public void SetUIScale(int scaleIndex)
    {
        float[] scales = { 0.8f, 1.0f, 1.2f }; // Small, Medium, Large
        float scale = scales[Mathf.Clamp(scaleIndex, 0, scales.Length - 1)];
        
        // Apply UI scale - this would need to be connected to your UI system
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvas.scaleFactor = scale;
            }
        }
        
        PlayerPrefs.SetInt("UIScale", scaleIndex);
    }
    
    public void SetCrosshair(int crosshairIndex)
    {
        PlayerPrefs.SetInt("CrosshairType", crosshairIndex);
    }
    
    public void SetShowDamageNumbers(bool enabled)
    {
        PlayerPrefs.SetInt("ShowDamageNumbers", enabled ? 1 : 0);
    }
    
    public void SetAutoCollectSouls(bool enabled)
    {
        PlayerPrefs.SetInt("AutoCollectSouls", enabled ? 1 : 0);
    }
    
    public void SetUIOpacity(float value)
    {
        if (uiOpacityText != null)
            uiOpacityText.text = $"{Mathf.RoundToInt(value * 100)}%";
        PlayerPrefs.SetFloat("UIOpacity", value);
    }
    
    // === SAVE/LOAD SETTINGS ===
    void LoadAllSettings()
    {
        // Audio settings
        float masterVol = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 0.6f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        float uiVol = PlayerPrefs.GetFloat("UIVolume", 0.6f);
        
        if (masterVolumeSlider != null) { masterVolumeSlider.value = masterVol; SetMasterVolume(masterVol); }
        if (musicVolumeSlider != null) { musicVolumeSlider.value = musicVol; SetMusicVolume(musicVol); }
        if (sfxVolumeSlider != null) { sfxVolumeSlider.value = sfxVol; SetSFXVolume(sfxVol); }
        if (uiVolumeSlider != null) { uiVolumeSlider.value = uiVol; SetUIVolume(uiVol); }
        
        // Video settings
        int resIndex = PlayerPrefs.GetInt("ResolutionIndex", _filteredResolutions.Count - 1);
        int quality = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
        int windowMode = PlayerPrefs.GetInt("WindowMode", 0);
        float mainFOV = PlayerPrefs.GetFloat("MainFOV", 75f);
        float weaponFOV = PlayerPrefs.GetFloat("WeaponFOV", 60f);
        bool vsync = PlayerPrefs.GetInt("VSync", 1) == 1;
        bool showFPS = PlayerPrefs.GetInt("ShowFPS", 0) == 1;
        
        if (resolutionDropdown != null && resIndex < _filteredResolutions.Count) 
        { 
            resolutionDropdown.value = resIndex; 
            SetResolution(resIndex); 
        }
        if (qualityDropdown != null) { qualityDropdown.value = quality; SetQuality(quality); }
        if (windowModeDropdown != null) { windowModeDropdown.value = windowMode; SetWindowMode(windowMode); }
        if (mainFOVSlider != null) { mainFOVSlider.value = mainFOV; SetMainFOV(mainFOV); }
        if (weaponFOVSlider != null) { weaponFOVSlider.value = weaponFOV; SetWeaponFOV(weaponFOV); }
        if (vsyncToggle != null) { vsyncToggle.isOn = vsync; SetVSync(vsync); }
        if (showFPSToggle != null) { showFPSToggle.isOn = showFPS; SetShowFPS(showFPS); }
        
        // Controls settings
        float mouseSens = PlayerPrefs.GetFloat("MouseSensitivity", 2f);
        bool invertY = PlayerPrefs.GetInt("InvertYAxis", 0) == 1;
        
        if (mouseSensitivitySlider != null) { mouseSensitivitySlider.value = mouseSens; SetMouseSensitivity(mouseSens); }
        if (invertYAxisToggle != null) { invertYAxisToggle.isOn = invertY; SetInvertYAxis(invertY); }
        
        // Gameplay settings
        int uiScale = PlayerPrefs.GetInt("UIScale", 1);
        int crosshair = PlayerPrefs.GetInt("CrosshairType", 0);
        bool showDamage = PlayerPrefs.GetInt("ShowDamageNumbers", 1) == 1;
        bool autoSouls = PlayerPrefs.GetInt("AutoCollectSouls", 0) == 1;
        float uiOpacity = PlayerPrefs.GetFloat("UIOpacity", 1f);
        
        if (uiScaleDropdown != null) { uiScaleDropdown.value = uiScale; SetUIScale(uiScale); }
        if (crosshairDropdown != null) { crosshairDropdown.value = crosshair; SetCrosshair(crosshair); }
        if (showDamageNumbersToggle != null) { showDamageNumbersToggle.isOn = showDamage; SetShowDamageNumbers(showDamage); }
        if (autoCollectSoulsToggle != null) { autoCollectSoulsToggle.isOn = autoSouls; SetAutoCollectSouls(autoSouls); }
        if (uiOpacitySlider != null) { uiOpacitySlider.value = uiOpacity; SetUIOpacity(uiOpacity); }
    }
    
    public void ResetToDefaults()
    {
        PlayerPrefs.DeleteAll();
        LoadAllSettings();
        PlayMenuSound(menuSelectSound);
    }
    
    void PlayMenuSound(AudioClip clip)
    {
        if (uiAudioSource != null && clip != null)
        {
            uiAudioSource.PlayOneShot(clip);
        }
    }
    
    void OnDestroy()
    {
        // Clean up listeners
        if (audioTabButton != null) audioTabButton.onClick.RemoveAllListeners();
        if (videoTabButton != null) videoTabButton.onClick.RemoveAllListeners();
        if (controlsTabButton != null) controlsTabButton.onClick.RemoveAllListeners();
        if (gameplayTabButton != null) gameplayTabButton.onClick.RemoveAllListeners();
        if (backButton != null) backButton.onClick.RemoveAllListeners();
    }
}