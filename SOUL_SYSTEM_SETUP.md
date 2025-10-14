# Soul Currency System - Setup Guide

## ✅ Scripts Created
1. **SoulManager.cs** - Singleton that tracks player souls
2. **SoulPickup.cs** - Physical soul pickup with ejection and attraction
3. **UISoulCounter.cs** - UI display for soul count
4. **EnemyHealth.cs** - Modified to spawn souls on death

---

## 🎮 Unity Editor Setup Steps

### 1. Create the Soul Pickup Prefab

1. **Create a new GameObject** in the scene:
   - Right-click in Hierarchy → Create Empty
   - Name it "Soul Pickup"

2. **Add a visual component**:
   - Add a Sphere: Right-click Soul Pickup → 3D Object → Sphere
   - Scale the sphere down to **0.3, 0.3, 0.3**
   - Optional: Create a glowing material:
     - Create Material: Project → Create → Material → "Soul Material"
     - Set Emission to bright cyan/blue color
     - Increase Emission intensity (2-3)
     - Apply to sphere

3. **Add Components to Soul Pickup**:
   - Add **SoulPickup** script
   - Add **Sphere Collider** (should auto-add)
   - Set Collider to **Is Trigger = true**

4. **Configure SoulPickup script**:
   - Soul Value: **1**
   - Ejection Speed: **5**
   - Ejection Duration: **0.3**
   - Attraction Speed: **12**
   - Player Layer: Set to your Player layer mask

5. **Save as Prefab**:
   - Drag "Soul Pickup" from Hierarchy to `Assets/Prefabs/` folder
   - Delete from scene

---

### 2. Setup SoulManager in Scene

1. **Create SoulManager GameObject**:
   - Right-click Hierarchy → Create Empty
   - Name it "Soul Manager"
   - Add **SoulManager** component
   - Starting Souls: **0** (or whatever you want for testing)

---

### 3. Configure Enemy to Drop Souls

1. **Open Enemy Prefab** (`Assets/Prefabs/Enemies/Enemy Basic.prefab`)

2. **Find EnemyHealth component**:
   - Assign **Soul Pickup Prefab** to the `soulPickupPrefab` field
   - Souls To Drop Min: **1**
   - Souls To Drop Max: **3**

3. **Save prefab changes**

---

### 4. Add Soul Counter to UI

1. **Open your UI Canvas** in the scene

2. **Create Soul Counter UI**:
   - Right-click Canvas → UI → Text - TextMeshPro
   - Name it "Soul Counter"
   - Position in top-left or wherever you want (suggested: near ammo/health)

3. **Configure TextMeshPro**:
   - Font Size: **36** (or match your other UI)
   - Color: **Cyan/Yellow/Gold** (soul color)
   - Alignment: Left or Center
   - Text: "Souls: 0" (placeholder)

4. **Add UISoulCounter Component**:
   - Select Soul Counter object
   - Add **UISoulCounter** script
   - Soul Text: Drag the TextMeshProUGUI component here (or leave empty to auto-find)
   - Prefix: "Souls: "
   - Pulse On Change: **true** (for visual feedback)

---

### 5. Verify Player Layer

Make sure your Player GameObject has a **Layer** set (e.g., "Player" layer):
1. Select Player in Hierarchy
2. Set Layer to "Player" in Inspector (create layer if needed)
3. The SoulPickup script uses this layer to detect collision

---

## 🧪 Testing

1. **Start the game**
2. **Kill an enemy**
3. You should see:
   - Souls eject from the enemy in random directions
   - Souls pause briefly, then fly toward the player
   - When collected, the UI counter increments
   - The UI text pulses on collection

### Debug Commands (in SoulManager Inspector)
- Right-click SoulManager component → "Debug: Add 100 Souls"
- Right-click SoulManager component → "Debug: Spend 50 Souls"

---

## 🎨 Visual Customization (Optional)

### Make Souls Glow More
1. Create a new **Material** for souls
2. Set **Rendering Mode** to Transparent (if using Standard shader)
3. Enable **Emission** and set intensity high (2-5)
4. Use cyan/blue/yellow/gold colors
5. Apply to Soul Pickup sphere

### Add Particle Effects
1. Add **Particle System** to Soul Pickup prefab
2. Small, sparkly particles that follow the soul
3. Emit particles on collection (instantiate at collection point)

### Add Sound Effects
In `SoulPickup.cs`, line 122 has a TODO:
```csharp
// TODO: Play collection sound/VFX here
```
Add an AudioSource and play a "ding" or "chime" sound

---

## 🔧 Common Issues

**Souls not spawning:**
- Check that Enemy prefab has soulPickupPrefab assigned
- Check that soulsToDropMin/Max are > 0

**Souls not being collected:**
- Verify Player has correct Layer set
- Check SoulPickup's Player Layer mask matches
- Ensure SphereCollider on soul is set to Is Trigger = true

**UI not updating:**
- Check SoulManager exists in scene
- Check UISoulCounter has reference to TextMeshProUGUI
- Look for errors in Console

**Souls flying weird:**
- Adjust ejectionSpeed, attractionSpeed in SoulPickup
- Tweak ejectionGravity for more/less arc
- Adjust attractionDelay for timing

---

## ✨ Next Steps

Once this works, you can:
1. **Weapon Upgrade Machine** - Use `SoulManager.TrySpendSouls()` for purchases
2. **Slot Machine** - Spend souls for random rewards
3. **Consumables Shop** - Buy buffs with souls
4. **Wave bonus souls** - Give extra souls at end of wave

The foundation is complete! 🎉
