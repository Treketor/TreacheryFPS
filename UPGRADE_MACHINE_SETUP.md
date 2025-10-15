# Weapon Upgrade Machine System - Setup Guide

## ✅ Scripts Created

1. **WeaponTierSystem.cs** - Tier definitions and stat multipliers
2. **WeaponInstance_Hitscan.cs** (Modified) - Added tier upgrade functionality
3. **WeaponController.cs** (Modified) - Added GetCurrentWeapon() method
4. **WeaponUpgradeMachine.cs** - Interactable upgrade machine
5. **UIWeaponUpgradePrompt.cs** - UI prompt when near machine
6. **UIWeaponUpgradeFlash.cs** - Visual feedback on upgrade

---

## 🎮 Unity Editor Setup Steps

### 0. Setup Input Action (If Not Already Created)

If you don't have an "Interact" action in your Input System:

1. **Open your Input Actions asset** (`InputSystem_Actions.inputactions`)
2. **Add new action**:
   - Name: **Interact**
   - Action Type: **Button**
   - Binding: **E [Keyboard]** (or your preferred key)
3. **Save the asset**

### 1. Update Existing Weapons

Your existing weapons need to be reconfigured for the new tier system:

1. **Open your weapon prefab/GameObject** (e.g., Pistol)
2. **Find WeaponInstance_Hitscan component**
3. **New fields will appear:**
   - **Base Damage**: Set to your original damage value (e.g., 20)
   - **Base Fire Rate**: Original fire rate (e.g., 5)
   - **Base Mag Size**: Original mag size (e.g., 12)
   - **Base Reload Time**: Original reload time (e.g., 1.2)
   - **Base Spread**: Original spread (e.g., 1.5)
   - **Current Tier**: Set to **Common** (starts at tier 0)

4. **Important**: The old `damage`, `fireRate`, etc. fields are now calculated automatically from base stats × tier multipliers!

---

### 2. Create the Upgrade Machine GameObject

1. **Create Empty GameObject** in scene:
   - Right-click Hierarchy → Create Empty
   - Name: "Weapon Upgrade Machine"
   - Position: Place it somewhere accessible in your map

2. **Add WeaponUpgradeMachine component**:
   - Interaction Range: **3** (meters)
   - Player: Drag your Player GameObject here
   - Weapon Controller: Drag the WeaponController component from Player
   - Player Input: Drag your **InputSystem_Actions** asset (same one used for movement/shooting)

3. **Create Visual Representation** (child of Upgrade Machine):
   - Add a **Cube** or custom model as child
   - Scale it to look like a machine (e.g., 1 × 2 × 1)
   - Add a glowing material (optional)
   - Create a **Highlight object** (child):
     - Add another Cube, slightly larger
     - Make material emissive/glowing
     - Drag this highlight to **Highlight Object** field in WeaponUpgradeMachine

4. **Optional - Add Particles**:
   - Add **Particle System** as child
   - Configure for arcane/magical look
   - Drag to **Upgrade Effect** field

5. **Optional - Add Audio**:
   - Add **AudioSource** component to machine
   - Import upgrade sound effects
   - Assign to **Upgrade Sound** and **Denied Sound** fields

---

### 3. Create Upgrade Prompt UI

1. **Open your UI Canvas**

2. **Create Upgrade Prompt Panel**:
   - Right-click Canvas → UI → Panel
   - Name: "Upgrade Prompt Panel"
   - Position: Center-bottom of screen (or wherever you prefer)
   - Add **CanvasGroup** component

3. **Add UI Elements** (children of panel):

   **a) Weapon Name Text:**
   - UI → Text - TextMeshPro
   - Name: "Weapon Name"
   - Text: "Pistol" (placeholder)
   - Font Size: 32
   - Bold, centered

   **b) Current Tier Display:**
   - UI → Text - TextMeshPro
   - Name: "Current Tier"
   - Text: "Common" (placeholder)
   - Font Size: 24
   - Position: Left side

   **c) Arrow/Divider:**
   - UI → Text - TextMeshPro
   - Text: "→"
   - Font Size: 36

   **d) Next Tier Display:**
   - UI → Text - TextMeshPro
   - Name: "Next Tier"
   - Text: "Rare" (placeholder)
   - Font Size: 24
   - Position: Right side

   **e) Cost Text:**
   - UI → Text - TextMeshPro
   - Name: "Cost Text"
   - Text: "Cost: 100 Souls" (placeholder)
   - Font Size: 20

   **f) Prompt Text:**
   - UI → Text - TextMeshPro
   - Name: "Prompt Text"
   - Text: "Press [E] to Upgrade" (placeholder)
   - Font Size: 18

   **g) Optional - Tier Color Bars:**
   - UI → Image (two of them)
   - Names: "Current Tier Color", "Next Tier Color"
   - Small colored bars under tier names
   - These will change color based on tier

4. **Add UIWeaponUpgradePrompt Component** to the panel:
   - Upgrade Machine: Drag your WeaponUpgradeMachine GameObject
   - Canvas Group: Drag the CanvasGroup component
   - Assign all the text fields you created
   - Assign color bar images if you made them
   - Configure display settings:
     - Prompt Format: "Press [E] to Upgrade"
     - Cost Format: "Cost: {0} Souls"
     - Max Tier Message: "MAX TIER"
     - Fade Speed: 8

---

### 4. Create Upgrade Flash Effect (Optional)

1. **In Canvas, create Full-Screen Flash**:
   - UI → Image
   - Name: "Upgrade Flash"
   - Anchor: Stretch to fill entire screen
   - Color: White (alpha 0)
   - Raycast Target: OFF

2. **Create Upgrade Message Text**:
   - UI → Text - TextMeshPro
   - Name: "Upgrade Message"
   - Position: Center of screen
   - Font Size: 48, Bold
   - Text: "Weapon Upgraded!" (placeholder)
   - Color: Alpha 0

3. **Create Container GameObject**:
   - Create Empty in Canvas
   - Name: "Upgrade Flash System"
   - Move the flash image and text as children

4. **Add UIWeaponUpgradeFlash Component**:
   - Flash Image: Drag the flash image
   - Upgrade Text: Drag the upgrade message text
   - Flash Duration: 0.5
   - Flash Intensity: 0.3
   - Text Display Duration: 2

---

## 🎨 Tier Colors

The system automatically colors tiers:
- **Common**: Gray (0.8, 0.8, 0.8)
- **Rare**: Blue (0.3, 0.5, 1.0)
- **Epic**: Purple (0.8, 0.2, 0.8)
- **Legendary**: Gold (1.0, 0.8, 0.0)

---

## 💰 Tier Costs & Stat Multipliers

Based on your GDD:

| Tier | Cost to Next | Damage | Fire Rate | Mag Size | Reload Time | Spread |
|------|--------------|--------|-----------|----------|-------------|---------|
| **Common** | 100 | 1.0× | 1.0× | 1.0× | 1.0× | 1.0× |
| **Rare** | 250 | 1.5× | 1.2× | 1.3× | 0.85× | 0.9× |
| **Epic** | 500 | 2.5× | 1.5× | 1.6× | 0.7× | 0.7× |
| **Legendary** | — | 4.0× | 2.0× | 2.0× | 0.5× | 0.5× |

*Note: Lower reload time and spread = better*

---

## 🧪 Testing

1. **Start the game with some souls** (modify SoulManager Starting Souls to 1000 for testing)
2. **Approach the upgrade machine**
3. **You should see:**
   - Highlight object glows
   - UI prompt appears showing current tier, next tier, and cost
   - Cost text turns red if you can't afford it
4. **Press E to upgrade**
5. **If successful:**
   - Souls deducted
   - Weapon stats improve
   - Screen flashes with tier color
   - Upgrade message appears
   - Weapon ammo refilled

### Debug Testing:
- Use SoulManager debug commands (right-click → "Debug: Add 100 Souls")
- Try upgrading through all 4 tiers
- Verify at Legendary tier it says "MAX TIER"
- Check weapon stats actually improve (use Debug.Log or inspector)

---

## 📊 Example: Pistol Upgrade Path

**Starting Stats (Common):**
- Damage: 20
- Fire Rate: 5 shots/sec
- Mag Size: 12
- Reload: 1.2s

**After Rare (100 souls):**
- Damage: 30 (20 × 1.5)
- Fire Rate: 6 shots/sec (5 × 1.2)
- Mag Size: 15 (12 × 1.3, rounded)
- Reload: 1.02s (1.2 × 0.85)

**After Epic (250 souls):**
- Damage: 50 (20 × 2.5)
- Fire Rate: 7.5 shots/sec (5 × 1.5)
- Mag Size: 19 (12 × 1.6, rounded)
- Reload: 0.84s (1.2 × 0.7)

**After Legendary (500 souls):**
- Damage: 80 (20 × 4.0)
- Fire Rate: 10 shots/sec (5 × 2.0)
- Mag Size: 24 (12 × 2.0)
- Reload: 0.6s (1.2 × 0.5)

---

## 🔧 Common Issues

**Machine not working:**
- Verify Player and WeaponController references are assigned
- Check SoulManager exists in scene
- Ensure player has Layer set correctly
- Check interaction range (increase if needed)
- **Verify "Interact" action exists in Input Action Asset**
- Check Console for warnings about missing Interact action

**UI not showing:**
- Verify Canvas is set to Screen Space - Overlay
- Check CanvasGroup alpha isn't locked to 0
- Ensure UIWeaponUpgradePrompt references are assigned

**Stats not improving:**
- Verify weapon has correct BASE stats set
- Check Current Tier is advancing
- Look in console for upgrade success message
- Inspect weapon component to see calculated stats

**Cost is wrong:**
- Check WeaponTierSystem.cs upgrade costs
- Verify weapon's Current Tier is correct

---

## 🎯 Integration with Existing Systems

### Soul Economy
The machine uses `SoulManager.TrySpendSouls()` - it's already integrated!

### Multiple Weapons
When you add weapon switching:
- The machine automatically detects the currently equipped weapon
- Each weapon tracks its own tier independently
- Upgrades are permanent (persist on weapon, not player)

### Wave Progression
Consider adding:
- Discount system after certain waves
- Free upgrades as wave rewards
- Increased costs in higher waves (scaling difficulty)

---

## ✨ Next Steps & Enhancements

1. **Weapon Suffix Names** - Append tier to name: "Pistol" → "Pistol Rare"
2. **Legendary Effects** - Add special effects (explosions, chain lightning, etc.)
3. **Upgrade Animation** - Weapon visuals change per tier
4. **Multiple Machines** - Place machines throughout the map
5. **Cooldown System** - Prevent spam upgrading
6. **Upgrade Requirements** - Lock tiers behind wave progression

The foundation is complete! 🎉
