# Pause Menu UI Setup Guide

## Canvas Structure

Create this UI hierarchy in your scene:

```
Canvas (Screen Space - Overlay)
├── GameplayUI (Panel)
│   ├── SoulsCounter
│   ├── WeaponSlots
│   ├── AmmoDisplay
│   └── ScoreDisplay
│
├── PauseMenuPanel (Panel)
│   ├── Background (Image - dark overlay)
│   ├── MenuTitle (Text - "PAUSED")
│   └── ButtonContainer (Vertical Layout Group)
│       ├── ResumeButton (Button)
│       ├── SettingsButton (Button) 
│       ├── RestartButton (Button)
│       ├── MainMenuButton (Button)
│       └── QuitButton (Button)
│
└── ConfirmationPanel (Panel)
    ├── DialogBackground (Image - darker overlay)
    ├── ConfirmationText (TextMeshPro)
    └── ButtonRow (Horizontal Layout Group)
        ├── ConfirmYesButton (Button - "YES")
        └── ConfirmNoButton (Button - "NO")
```

## Setup Instructions

### 1. Create Canvas
1. Right-click in Hierarchy → UI → Canvas
2. Set Canvas Scaler to "Scale With Screen Size"
3. Reference Resolution: 1920x1080

### 2. Setup GameplayUI Panel
1. Create Panel under Canvas, rename to "GameplayUI"
2. Set Anchors to stretch (full screen)
3. Move your existing UI elements (souls counter, weapon slots, etc.) under this panel

### 3. Create PauseMenuPanel
1. Create Panel under Canvas, rename to "PauseMenuPanel"
2. Set Anchors to stretch (full screen)
3. Set Color to transparent black (0,0,0,128) for overlay effect

### 4. Add Menu Title
1. Create Text under PauseMenuPanel
2. Text: "PAUSED"
3. Font Size: 48-60
4. Anchor: Top Center
5. Color: White or your game's accent color

### 5. Create Button Container
1. Create Panel under PauseMenuPanel, rename to "ButtonContainer"
2. Add Vertical Layout Group component
3. Set Spacing: 10-20
4. Child Alignment: Middle Center
5. Anchor to center of screen

### 6. Add Buttons
Create 5 buttons under ButtonContainer:

**Resume Button**
- Text: "RESUME"
- Navigation: First selected

**Settings Button**  
- Text: "SETTINGS"

**Restart Button**
- Text: "RESTART RUN"

**Main Menu Button**
- Text: "MAIN MENU"

**Quit Button**
- Text: "QUIT GAME"

### 7. Create Confirmation Dialog
1. Create Panel under Canvas, rename to "ConfirmationPanel"
2. Set Anchors to stretch (full screen)
3. Set Color to very dark transparent (0,0,0,200) for darker overlay
4. Add TextMeshPro component, rename to "ConfirmationText"
   - Anchor: Center
   - Text: "Placeholder text" (will be overridden by code)
   - Font Size: 24-32
   - Alignment: Center
   - Note: Text is set dynamically by the code for different actions
5. Create Panel for buttons, rename to "ButtonRow"
   - Add Horizontal Layout Group
   - Child Alignment: Middle Center
   - Spacing: 20
6. Add two buttons under ButtonRow:
   - **ConfirmYesButton**: Text "YES"
   - **ConfirmNoButton**: Text "NO"

### 8. Style Buttons (Optional)
- Use consistent button styling
- Gothic/dark theme to match your game
- Hover effects and sounds
- Make buttons larger (150x50) for better usability
- Style confirmation buttons differently (Yes=red, No=gray)

### 8. Add Pause Action to Input Actions
1. Open your `InputSystem_Actions.inputactions` file
2. Add a new action called "Pause":
   - Action Type: Button
   - Binding: Keyboard ESC
3. Save the Input Actions asset

### 9. Connect PauseMenuController
1. Add PauseMenuController script to an empty GameObject
2. Assign all UI references in inspector:
   - **Input Actions** → Your InputSystem_Actions asset
   - **pauseMenuPanel** → PauseMenuPanel
   - **gameplayUI** → GameplayUI  
   - **resumeButton** → ResumeButton
   - **settingsButton** → SettingsButton
   - **restartButton** → RestartButton
   - **mainMenuButton** → MainMenuButton
   - **quitButton** → QuitButton
   - **confirmationPanel** → ConfirmationPanel
   - **confirmationText** → ConfirmationText
   - **confirmYesButton** → ConfirmYesButton
   - **confirmNoButton** → ConfirmNoButton
   - **restartConfirmationText** → Set your restart message (e.g., "Restart current run?\n\nAll progress will be lost!")
   - **quitConfirmationText** → Set your quit message (e.g., "Quit the game?\n\nCurrent progress will be lost!")

### 10. Audio Setup (Optional)
1. Create AudioSource on PauseMenuController GameObject
2. Add menu navigation/selection sounds
3. Assign to uiAudioSource field

## Testing
- Press ESC to pause/unpause
- Verify all gameplay input stops when paused
- Test button navigation with keyboard
- Ensure cursor shows/hides correctly