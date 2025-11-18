# Settings System Setup Guide

## Overview
Complete PC-focused settings system with Audio, Video, Controls, and Gameplay options. All settings are automatically saved to PlayerPrefs and restored on game startup.

## Settings Panel Structure

Create this UI hierarchy under your Canvas:

```
Canvas
└── SettingsPanel (Panel)
    ├── Background (Image - dark overlay)
    ├── SettingsTitle (Text - "SETTINGS")
    ├── TabContainer (Horizontal Layout Group)
    │   ├── AudioTabButton (Button - "AUDIO")
    │   ├── VideoTabButton (Button - "VIDEO")
    │   ├── ControlsTabButton (Button - "CONTROLS")
    │   └── GameplayTabButton (Button - "GAMEPLAY")
    ├── BackButton (Button - "BACK")
    │
    ├── AudioPanel (Panel)
    │   ├── MasterVolumeContainer
    │   │   ├── MasterVolumeLabel (Text - "Master Volume")
    │   │   ├── MasterVolumeSlider (Slider - 0 to 1)
    │   │   └── MasterVolumeText (Text - "80%")
    │   ├── MusicVolumeContainer
    │   ├── SFXVolumeContainer
    │   └── UIVolumeContainer
    │
    ├── VideoPanel (Panel)
    │   ├── ResolutionContainer
    │   │   ├── ResolutionLabel (Text - "Resolution")
    │   │   └── ResolutionDropdown (TMP_Dropdown)
    │   ├── QualityContainer
    │   │   ├── QualityLabel (Text - "Quality Preset")
    │   │   └── QualityDropdown (TMP_Dropdown)
    │   ├── WindowModeContainer
    │   │   ├── WindowModeLabel (Text - "Window Mode")
    │   │   └── WindowModeDropdown (TMP_Dropdown)
    │   ├── MainFOVContainer
    │   │   ├── MainFOVLabel (Text - "Field of View")
    │   │   ├── MainFOVSlider (Slider - 60 to 120)
    │   │   └── MainFOVText (Text - "75°")
    │   ├── WeaponFOVContainer
    │   │   ├── WeaponFOVLabel (Text - "Weapon FOV")
    │   │   ├── WeaponFOVSlider (Slider - 40 to 80)
    │   │   └── WeaponFOVText (Text - "60°")
    │   ├── VSyncToggle (Toggle - "V-Sync")
    │   └── ShowFPSToggle (Toggle - "Show FPS")
    │
    ├── ControlsPanel (Panel)
    │   ├── MouseSensitivityContainer
    │   │   ├── MouseSensitivityLabel (Text - "Mouse Sensitivity")
    │   │   ├── MouseSensitivitySlider (Slider - 0.1 to 5)
    │   │   └── MouseSensitivityText (Text - "2.0")
    │   ├── InvertYAxisToggle (Toggle - "Invert Y-Axis")
    │   └── KeybindContainer (For future key binding system)
    │
    └── GameplayPanel (Panel)
        ├── UIScaleContainer
        │   ├── UIScaleLabel (Text - "UI Scale")
        │   └── UIScaleDropdown (TMP_Dropdown - "Small/Medium/Large")
        ├── CrosshairContainer
        │   ├── CrosshairLabel (Text - "Crosshair Style")
        │   └── CrosshairDropdown (TMP_Dropdown - "Cross/Dot/Circle/None")
        ├── ShowDamageNumbersToggle (Toggle - "Show Damage Numbers")
        ├── AutoCollectSoulsToggle (Toggle - "Auto-Collect Souls")
        └── UIOpacityContainer
            ├── UIOpacityLabel (Text - "UI Opacity")
            ├── UIOpacitySlider (Slider - 0.5 to 1)
            └── UIOpacityText (Text - "100%")
```

## Setup Instructions

### 1. Create Settings Panel Structure
1. Create the UI hierarchy above under your main Canvas
2. Use Vertical Layout Groups for containers within each panel
3. Set appropriate anchoring and sizing for responsive design

### 2. Audio Mixer Setup (Optional but Recommended)
1. Create AudioMixer asset: Assets → Create → Audio Mixer
2. Add groups for: Master, Music, SFX, UI
3. Add "Exposed Parameters" for volume control:
   - MasterVolume
   - MusicVolume
   - SFXVolume
   - UIVolume

### 3. Slider Configurations

**Volume Sliders:**
- Min Value: 0.001 (not 0, for logarithmic calculation)
- Max Value: 1
- Default: 0.8

**FOV Sliders:**
- Main FOV: Min 60, Max 120, Default 75
- Weapon FOV: Min 40, Max 80, Default 60

**Mouse Sensitivity:**
- Min: 0.1, Max: 5.0, Default: 2.0

**UI Opacity:**
- Min: 0.5, Max: 1.0, Default: 1.0

### 4. Dropdown Options

**Resolution Dropdown:**
- Auto-populated with available screen resolutions
- Filters by current refresh rate

**Quality Dropdown:**
- Auto-populated from Unity's Quality Settings

**Window Mode:**
- Options: "Fullscreen", "Windowed", "Borderless"

**UI Scale:**
- Options: "Small", "Medium", "Large"

**Crosshair:**
- Options: "Cross", "Dot", "Circle", "None"

### 5. Connect SettingsController
1. Add SettingsController script to an empty GameObject
2. Assign all UI references in inspector:

**Settings Panels:**
- settingsPanel → SettingsPanel
- audioPanel → AudioPanel  
- videoPanel → VideoPanel
- controlsPanel → ControlsPanel
- gameplayPanel → GameplayPanel

**Tab Buttons:**
- audioTabButton → AudioTabButton
- videoTabButton → VideoTabButton
- controlsTabButton → ControlsTabButton
- gameplayTabButton → GameplayTabButton
- backButton → BackButton

**Audio Settings:**
- audioMixer → Your Audio Mixer asset
- masterVolumeSlider → MasterVolumeSlider
- masterVolumeText → MasterVolumeText
- (Repeat for music, sfx, ui)

**Video Settings:**
- resolutionDropdown → ResolutionDropdown
- qualityDropdown → QualityDropdown
- windowModeDropdown → WindowModeDropdown
- mainFOVSlider → MainFOVSlider
- mainFOVText → MainFOVText
- weaponFOVSlider → WeaponFOVSlider
- weaponFOVText → WeaponFOVText
- vsyncToggle → VSyncToggle
- showFPSToggle → ShowFPSToggle

**Controls Settings:**
- mouseSensitivitySlider → MouseSensitivitySlider
- mouseSensitivityText → MouseSensitivityText
- invertYAxisToggle → InvertYAxisToggle

**Gameplay Settings:**
- uiScaleDropdown → UIScaleDropdown
- crosshairDropdown → CrosshairDropdown
- showDamageNumbersToggle → ShowDamageNumbersToggle
- autoCollectSoulsToggle → AutoCollectSoulsToggle
- uiOpacitySlider → UIOpacitySlider
- uiOpacityText → UIOpacityText

### 6. Integration Points

**Camera Integration:**
- Main FOV automatically applies to Camera.main
- Weapon FOV ready for weapon camera system

**Audio Integration:**
- Volume sliders work with AudioMixer groups
- Logarithmic scaling for natural volume perception

**Input Integration:**
- Mouse sensitivity ready for FirstPersonLook integration
- Invert Y-axis setting ready for mouse look

**UI Integration:**
- UI Scale applies to all Canvas components
- UI Opacity ready for UI transparency system

### 7. Testing Features

**Immediate Effects:**
- ✅ Resolution changes
- ✅ Quality preset changes
- ✅ Window mode switching
- ✅ V-Sync toggle
- ✅ Audio volume (if AudioMixer connected)
- ✅ Main camera FOV
- ✅ UI scaling

**Ready for Integration:**
- 🔄 Weapon FOV (needs weapon camera)
- 🔄 Mouse sensitivity (needs FirstPersonLook)
- 🔄 Crosshair changes (needs crosshair system)
- 🔄 FPS display (needs FPS counter)
- 🔄 Damage numbers (needs damage system)

### 8. Additional Features

**Reset to Defaults:**
- Add "Reset to Defaults" button that calls `ResetToDefaults()`

**Key Bindings:**
- Framework ready for key binding system
- keybindContainer and keybindItemPrefab prepared

**Persistent Settings:**
- All settings automatically save to PlayerPrefs
- Loaded on game startup
- Survives game restarts

## Usage
1. Settings accessible through pause menu
2. Tabbed interface for organized options
3. Real-time preview of most changes
4. Automatic saving of all preferences
5. Professional PC game settings experience