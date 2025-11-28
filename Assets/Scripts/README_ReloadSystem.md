# Weapon Reload System Documentation

## Overview

The weapon reload system now supports two different reload behaviors:

1. **Magazine Reload** (Traditional) - Reloads entire magazine at once
2. **Single Bullet Reload** (Pump Shotgun Style) - Reloads one bullet at a time with interruption support

## Architecture

### Core Components

- `IWeaponReloadBehavior` - Interface for all reload behaviors
- `MagazineReloadBehavior` - Traditional magazine-based reload
- `SingleBulletReloadBehavior` - Single bullet reload for shotguns/revolvers
- `WeaponInstance_Hitscan` - Modified to support both reload types

### Reload Type Selection

In the `WeaponInstance_Hitscan` inspector:
- **Reload Type**: Choose between `Magazine` or `SingleBullet`

## Single Bullet Reload System

### Features

- **Three-Phase Animation**: Start Reload → Load Single Bullet (looped) → Finish Reload
- **Interruptible**: Player can shoot with partial ammo during bullet loading phase
- **Auto-Complete**: Automatically finishes when magazine is full
- **Flexible**: Can be used by any weapon type (shotguns, revolvers, etc.)

### Configuration

#### Animation Settings
```csharp
[Header("Single Bullet Reload Settings")]
public float startReloadDuration = 0.8f;        // "Start Reload" animation time
public float singleBulletReloadDuration = 0.6f; // Each "Reload Single Bullet" time
public float finishReloadDuration = 0.5f;       // "Finish Reload" animation time
```

#### Animation Triggers
```csharp
public string startReloadTrigger = "StartReload";           // Weapon lifts up
public string reloadSingleBulletTrigger = "ReloadSingleBullet"; // Load one bullet
public string finishReloadTrigger = "FinishReload";        // Cock/pump weapon
```

#### Auto-Finish Settings
```csharp
public bool autoFinishWhenFull = true;    // Auto-finish when magazine full
public float autoFinishDelay = 0.3f;      // Delay before auto-finishing
```

### Reload Phases

1. **Start Phase**: Weapon preparation (lifting, opening chamber)
   - Duration: `startReloadDuration`
   - Cannot be interrupted
   
2. **Loading Phase**: Single bullet loading (can be looped)
   - Duration: `singleBulletReloadDuration` per bullet
   - **Can be interrupted** for shooting
   - Continues until magazine full or interrupted
   
3. **Finish Phase**: Weapon readying (cocking, chambering)
   - Duration: `finishReloadDuration`
   - Cannot be interrupted
   - Auto-triggered when magazine full or reload stopped

### Interruption System

#### When Can Reload Be Interrupted?
- Only during the **Loading Phase** (bullet loading)
- NOT during Start or Finish phases (critical animations)

#### How to Interrupt?
```csharp
// Automatic interruption when shooting
weapon.TryFire(); // Will interrupt reload if possible

// Manual interruption check
bool canInterrupt = weapon.TryInterruptReloadForShooting();
```

## Pellet System (Shotgun Support)

### Overview
The pellet system allows weapons to fire multiple projectiles per trigger pull, perfect for shotguns. Each pellet:
- Has independent spread calculation
- Deals fractional damage per pellet
- Creates separate impact effects
- Can hit different targets simultaneously

### Configuration
```csharp
[Header("Pellet System (Shotgun)")]
public bool usePelletSystem = false;           // Enable pellet firing
public int pelletsPerShot = 8;                // Number of pellets per shot
public float pelletSpreadMultiplier = 3.0f;   // Spread multiplier for pellets
public float pelletDamageMultiplier = 1.0f;   // Damage multiplier for distributed pellets
```

### Damage Calculation
- **Damage Distribution**: Base damage is divided equally among all pellets
- **Per Pellet**: `(baseDamage / pelletsPerShot) * pelletDamageMultiplier`
- **Example**: 100 base damage ÷ 8 pellets × 1.0 multiplier = 12.5 damage per pellet
- **Total Possible**: 12.5 × 8 pellets = 100 damage (if all pellets hit)
- **Realistic Scenario**: 12.5 × 4 pellets = 50 damage (partial hit)

### Spread Behavior
- Base weapon spread affects pellet grouping
- `pelletSpreadMultiplier` increases individual pellet spread
- Higher values = wider pellet pattern
- ADS reduces pellet spread (more accurate grouping)

## Setup Instructions

### For Pump Shotgun

1. **Weapon Component Setup**:
   ```csharp
   // Set in WeaponInstance_Hitscan inspector
   reloadType = ReloadType.SingleBullet;
   usePelletSystem = true;
   pelletsPerShot = 8;
   pelletSpreadMultiplier = 3.0f;
   pelletDamageMultiplier = 1.0f;
   ```

2. **Animation Controller Setup**:
   - Add trigger parameters: `StartReload`, `ReloadSingleBullet`, `FinishReload`
   - Create 3 animation states with appropriate transitions
   - Set trigger conditions for each transition

3. **Animation Clips**:
   - **StartReload**: Weapon lifts up, chamber opens (~0.8s)
   - **ReloadSingleBullet**: One shell loads into chamber (~0.6s)
   - **FinishReload**: Weapon pumps/cocks, ready to fire (~0.5s)

4. **Timing Configuration**:
   ```csharp
   startReloadDuration = 0.8f;        // Match animation length
   singleBulletReloadDuration = 0.6f; // Match animation length  
   finishReloadDuration = 0.5f;       // Match animation length
   autoFinishWhenFull = true;         // Recommended for shotguns
   autoFinishDelay = 0.3f;            // Small delay feels natural
   ```

### For Traditional Weapons

Keep using `ReloadType.Magazine` - no changes needed.

## Usage Examples

### Basic Single Bullet Reload
```csharp
// Player presses reload
weapon.TryReload();
// → Starts: StartReload animation
// → Continues: ReloadSingleBullet animations (looped)
// → Finishes: FinishReload animation when full/stopped
```

### Interrupted Reload
```csharp
// During reload, player tries to shoot
weapon.TryFire();
// → If in Loading Phase: Cancels reload, allows shooting
// → If in Start/Finish Phase: Ignores input until phase complete
```

### Manual Interruption
```csharp
// Check if reload can be interrupted
if (singleBulletReload.CanBeInterrupted())
{
    weapon.CancelReload();
    // Handle interruption logic
}
```

## Benefits

1. **Realistic Shotgun Behavior**: Matches real-world pump shotgun reloading
2. **Tactical Gameplay**: Players must decide between full reload vs partial reload
3. **Flexible System**: Can be applied to any weapon type needing single-bullet loading
4. **Smooth Interruption**: Natural feel when transitioning from reload to shooting
5. **Backward Compatible**: Existing weapons continue working unchanged

## Future Extensions

The system can be easily extended for:
- **Clip-fed Weapons**: Similar to single bullet but with clips
- **Belt-fed Weapons**: Continuous reload with different interruption rules
- **Revolver Cylinders**: Speed loaders vs individual bullets
- **Break-action Weapons**: Special two-stage reload process

## Debugging

Use these methods for debugging:
```csharp
weapon.IsReloading                    // Check if currently reloading
singleBulletReload.GetCurrentPhaseString()  // Get current phase name
singleBulletReload.CanBeInterrupted() // Check if interruptible
```

Debug logs will show:
- Reload start/stop events
- Bullet loading progress
- Interruption attempts
- Phase transitions