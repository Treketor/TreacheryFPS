# Aim Down Sight (ADS) System Setup Guide

## Overview
The ADS system provides smooth camera FOV transitions, weapon positioning, improved accuracy, and animation integration for aiming down sights.

## Input System Setup

### 1. Add ADS Action to Input Actions
1. Open your `InputSystem_Actions.inputactions` file
2. Add a new action called "Aim":
   - **Action Type**: Button
   - **Binding**: Mouse Right Button (or your preferred key)
   - **Interactions**: Hold (for continuous aiming)
3. Save the Input Actions asset

### 1.1. Crosshair Manager Setup
1. **Create UI Crosshair**: Add an Image component to your UI Canvas for the crosshair display
2. **Add CrosshairManager**: Attach the script to any GameObject in your scene
3. **Configure Settings**:
   - **Crosshair Image**: Assign your UI crosshair Image (or let it auto-find)
   - **Auto Find Crosshair**: Enable to automatically locate crosshair UI elements
   - **Default Crosshair**: Set fallback sprite for weapons without specific crosshairs
4. **Weapon Assignment**: Assign unique crosshair sprites to weapons in their inspectors (optional)

## Weapon Configuration

### 2. Configure ADS Settings per Weapon
Each weapon has these ADS settings in the inspector:

#### **Aim Down Sights Section:**
- **Supports ADS**: Enable/disable ADS for this weapon
- **Weapon GFX**: Reference to the weapon model/visual object (child transform)
- **Auto Find GFX**: Automatically finds GFX child object by common names
- **ADS FOV**: Camera field of view when aiming (default: 40°)
- **ADS Transition Speed**: How fast camera transitions (default: 8)
- **ADS Position**: Weapon GFX position offset when aiming (X, Y, Z)
- **ADS Position Speed**: How fast weapon GFX moves to ADS position (default: 12)
- **ADS Spread Multiplier**: Accuracy improvement when aiming (0.3 = 70% less spread)

#### **Animation Settings:**
- **ADS Animation Trigger**: Trigger name for entering ADS (default: "ADS")
- **ADS Animation Bool**: Bool parameter for ADS state (default: "IsAiming")

#### **Crosshair Settings:**
- **Crosshair Sprite**: Custom crosshair sprite for this weapon (assign manually)
- **Auto Find Crosshair**: Reserved for future use (currently unused)

## Weapon Model Setup

### 2.1. Weapon Hierarchy Structure
For optimal ADS positioning, organize your weapon prefab hierarchy:

```
WeaponPrefab
├── WeaponInstance_Hitscan (script)
├── GFX (or Model/Mesh/Visual)
│   ├── WeaponModel.fbx
│   └── Attachments (scopes, etc.)
└── Other Components (Colliders, etc.)
```

#### **GFX Object Guidelines:**
- **Name**: "GFX", "Model", "Mesh", or "Visual" (auto-detected)
- **Purpose**: Contains the visual weapon model only
- **Benefits**: ADS positioning doesn't affect weapon logic/colliders
- **Setup**: Move your weapon model under a GFX child object

#### **Auto-Detection Feature:**
- The system automatically finds GFX objects by common names
- Falls back to first child object if standard names not found
- Can be manually assigned if needed via **Weapon GFX** field

#### **Manual Assignment (Optional):**
1. Create a child GameObject named "GFX"
2. Move weapon model/mesh under the GFX object
3. Assign GFX object to **Weapon GFX** field if auto-detection fails
4. Uncheck **Auto Find GFX** for manual control

### 2.2. Crosshair Setup

#### **Crosshair System Overview:**
The crosshair system uses your existing UI crosshair element and changes its sprite based on the active weapon.

#### **Setup Steps:**

1. **Create/Locate UI Crosshair**:
   - Ensure you have a UI Image component for the crosshair in your Canvas
   - Name it "Crosshair", "crosshair", "Reticle", or similar for auto-detection
   - Position it at the center of your screen

2. **Configure CrosshairManager**:
   - Add CrosshairManager script to your scene
   - Let it auto-find your UI crosshair, or assign manually
   - Set a **Default Crosshair** sprite (used when weapons have no specific crosshair)

3. **Assign Weapon Crosshairs**:
   - In each weapon's inspector, assign a **Crosshair Sprite** (optional)
   - If no sprite is assigned, the default crosshair will be used
   - Each weapon can have its unique crosshair design

#### **Behavior:**
- **Weapon Switching**: Crosshair sprite changes automatically to match active weapon
- **Default Fallback**: Weapons without assigned crosshairs use the default sprite
- **ADS Integration**: Crosshair automatically hides when aiming down sights
- **No Crosshair**: If no default and no weapon crosshair, UI element is hidden

## Animation Setup

### 3. Configure Weapon Animator
Add these parameters to your weapon's Animator Controller:

#### **Required Parameters:**
- **IsAiming** (Bool): Tracks current ADS state
- **ADS** (Trigger): Triggered when entering ADS

#### **Animation States:**
Create transitions between:
- **Idle → ADS Idle** (when IsAiming = true)
- **ADS Idle → Idle** (when IsAiming = false)
- **Shoot → ADS Shoot** (when IsAiming = true + Shoot trigger)
- **ADS Shoot → ADS Idle** (automatic after animation)

#### **Transition Conditions:**
```
Any State → ADS Idle:
- IsAiming = true
- Exit Time = false

ADS Idle → Any State:  
- IsAiming = false
- Exit Time = false
```

## Weapon Positioning

### 4. Configure ADS Position/Rotation
Adjust these values per weapon for optimal sight alignment:

#### **Typical Settings:**
**Rifle/SMG:**
- Position: (0, -0.05, 0.15) - slightly forward and down

**Pistol:**
- Position: (0, -0.02, 0.1) - subtle positioning

**Sniper/Scoped:**
- Position: (0, -0.08, 0.25) - more forward for scope alignment

#### **Fine-Tuning Tips:**
- **X**: Left/right positioning (usually 0)
- **Y**: Up/down alignment with sights
- **Z**: Forward/back for proper sight picture
- Test in-game and adjust incrementally

## Camera Settings

### 5. FOV Configuration
Configure appropriate FOV values:

#### **Main Camera FOV**: 70-75° (normal gameplay)
#### **ADS FOV by Weapon Type:**
- **Pistol**: 50-55°
- **Rifle/SMG**: 40-45°  
- **Sniper**: 20-30°
- **Shotgun**: 45-50°

#### **Transition Speed**: 6-10 (higher = faster transition)

## Gameplay Effects

### 6. Accuracy Improvements
The ADS Spread Multiplier affects accuracy:

- **0.1**: 90% spread reduction (very accurate)
- **0.3**: 70% spread reduction (good balance)
- **0.5**: 50% spread reduction (moderate improvement)  
- **0.7**: 30% spread reduction (slight improvement)

## Code Integration

### 7. Accessing ADS State
You can check if a weapon is aiming:

```csharp
WeaponInstance_Hitscan weapon = weaponController.CurrentWeapon;
if (weapon.IsAiming)
{
    // Player is currently aiming down sights
    // Apply movement speed reduction, etc.
}
```

### 8. Movement & Input Integration
The ADS system automatically integrates with player systems:

#### **Built-in Movement Effects:**
- **Speed Reduction**: Universal ADS speed multiplier (default: 40% speed)
- **Sprint Prevention**: Cannot sprint while aiming
- **Automatic Sprint Cancel**: Sprinting stops when ADS begins

#### **Built-in Mouse Sensitivity:**
- **Sensitivity Scaling**: Universal ADS sensitivity multiplier (default: 60% sensitivity)
- **Precise Aiming**: Reduced mouse sensitivity for better accuracy
- **Automatic Application**: Works with all weapons when aiming

#### **Configuration Settings:**
- **PlayerMovement - ADS Speed Multiplier**: Movement speed reduction (0.4 = 60% slower)
- **FirstPersonLook - ADS Sensitivity Multiplier**: Mouse sensitivity scaling (0.6 = 40% less sensitive)
- **Universal Application**: Same settings for all weapons

#### **Custom ADS Effects:**
Add additional features by checking `IsAiming`:

```csharp
WeaponInstance_Hitscan weapon = weaponController.CurrentWeapon;
if (weapon != null && weapon.IsAiming)
{
    // Mouse sensitivity is automatically handled by FirstPersonLook
    // Add other custom effects here:
    
    // UI changes (crosshairs are automatically managed and hidden during ADS)
    
    // Audio effects
    PlayADSAmbienceSound();
    
    // Visual effects
    ApplyDepthOfField();
}
```

## Testing & Tuning

### 9. Testing Checklist
- ✅ Right-click to aim smoothly
- ✅ Camera FOV transitions properly
- ✅ Weapon moves to correct position
- ✅ Improved accuracy when aiming
- ✅ Animations play correctly
- ✅ Can shoot while aiming
- ✅ Cannot ADS while reloading
- ✅ Cannot ADS while switching weapons
- ✅ ADS automatically cancels when starting reload/switch
- ✅ Movement speed reduced when aiming
- ✅ Cannot sprint while aiming
- ✅ Sprinting stops when ADS begins
- ✅ Mouse sensitivity reduced when aiming
- ✅ Look sensitivity returns to normal when exiting ADS
- ✅ Weapon-specific crosshairs display correctly
- ✅ Crosshair changes automatically when switching weapons
- ✅ Default crosshair shows for weapons without specific crosshairs
- ✅ Crosshair hides automatically when entering ADS
- ✅ Crosshair shows again when exiting ADS

### 10. Common Issues & Solutions

#### **Weapon clips through camera:**
- Reduce ADS Position Z value
- Adjust weapon model positioning

#### **FOV transition too fast/slow:**
- Adjust ADS Transition Speed
- Typical range: 4-12

#### **Poor sight alignment:**
- Fine-tune ADS Position Y and Z values
- Test with weapon model's iron sights

#### **Animation not triggering:**
- Verify "IsAiming" parameter exists in Animator
- Check ADS Animation Bool name matches
- Ensure transitions have correct conditions

#### **Crosshair not changing:**
- Check CrosshairManager is in the scene and configured
- Verify weapon has crosshair sprite assigned or auto-detection enabled
- Check console for crosshair detection debug messages
- Ensure UI Image component is properly assigned to CrosshairManager

## Advanced Features

### 11. Per-Weapon Customization
Each weapon can have unique ADS behavior:
- Sniper rifles: Lower FOV, slower transition
- SMGs: Minimal FOV change, fast transition  
- Shotguns: Moderate changes for close-range combat

### 12. Future Enhancements
Ready for additional features:
- **Scope overlays** for sniper rifles
- **Breathing effects** for steadier aim
- **Different reticles** per weapon
- **ADS movement penalties**
- **Separate weapon camera** for no clipping

## Usage
1. Hold right mouse button to aim down sights
2. Camera smoothly zooms to configured FOV
3. Weapon moves to aiming position
4. Movement speed automatically reduces
5. Mouse sensitivity automatically reduces for precision
6. Sprint is disabled while aiming
7. Improved accuracy for precise shots
8. Release to return to normal view
9. Works seamlessly with shooting
10. Automatically cancels when reloading or switching weapons