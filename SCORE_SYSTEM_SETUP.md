# Score System - Setup Guide

## ✅ Scripts Created

1. **ScoreManager.cs** - Singleton that tracks all stats and calculates score
2. **HeadshotZone.cs** - Component for enemy head hitboxes
3. **EnemyHealth.cs** (Modified) - Integrated with score tracking
4. **WeaponInstance_Hitscan.cs** (Modified) - Detects headshots
5. **WaveManager.cs** (Modified) - Reports wave completion to score
6. **UIScoreDisplay.cs** - Live score display during gameplay
7. **UIStatsDisplay.cs** - Live stats (kills, headshots, accuracy)
8. **UIRunSummary.cs** - End-of-run score breakdown screen

---

## 🎮 Unity Editor Setup Steps

### 1. Create ScoreManager in Scene

1. **Create Empty GameObject**:
   - Right-click Hierarchy → Create Empty
   - Name: "Score Manager"
   - Add **ScoreManager** component

2. **Configure Score Values**:
   - Points Per Kill: **100**
   - Points Per Headshot: **50** (bonus on top of kill)
   - Points Per Boss Kill: **500**
   - Points Per Wave Complete: **200**
   - Multiplayer Score Multiplier: **1.25**

---

### 2. Setup Headshot Detection on Enemies

1. **Open Enemy Prefab** (`Assets/Prefabs/Enemies/Enemy Basic.prefab`)

2. **Create Head Hitbox**:
   - Right-click enemy root → Create Empty
   - Name: "Head"
   - Position it where the head should be (top of enemy)
   - Add **Sphere Collider** (or Box Collider)
     - Radius/Size: Small enough to be head-sized
     - **Is Trigger: TRUE**
   - Add **HeadshotZone** component:
     - Enemy Root: Drag the enemy root GameObject
     - Headshot Damage Multiplier: **2.0** (double damage)

3. **Set Layer** (recommended):
   - Create a layer called "EnemyHeadshot" or use existing enemy layer
   - Assign head GameObject to this layer

4. **Update EnemyHealth** (already done in code):
   - The `isBoss` field is now available - set to `false` for normal enemies
   - Create boss variants and set to `true`

---

### 3. Add Score Display to HUD

1. **Open your UI Canvas**

2. **Create Score Counter**:
   - UI → Text - TextMeshPro
   - Name: "Score Display"
   - Position: Top-right corner (or wherever you prefer)
   - Font Size: **36-48**
   - Color: **Gold/Yellow**
   - Text: "Score: 0" (placeholder)
   - Add **UIScoreDisplay** component:
     - Score Text: Drag TextMeshProUGUI component
     - Prefix: "Score: "
     - Pulse On Score Gain: **true**
     - Score Gain Color: Bright yellow/gold

---

### 4. Add Stats Display (Optional)

1. **In Canvas, create Stats Panel**:
   - UI → Panel (optional background)
   - Name: "Stats Display"
   - Position: Bottom-left or top-left

2. **Add Stat Text Elements** (children):
   - **Kills Text**: "Kills: 0"
   - **Headshots Text**: "Headshots: 0"
   - **Accuracy Text**: "Accuracy: 0.0%"

3. **Add UIStatsDisplay Component** to panel:
   - Assign all three text fields
   - Configure formats as desired

---

### 5. Create End-Run Summary Screen

1. **In Canvas, create Summary Panel**:
   - UI → Panel
   - Name: "Run Summary Panel"
   - **Make it fill the entire screen** (stretch anchor)
   - Add semi-transparent black background
   - **Set Active: FALSE** (hidden by default)

2. **Add Summary UI Elements** (all children of panel):

   **a) Title Text:**
   - TextMeshProUGUI
   - Name: "Title"
   - Text: "YOU DIED" (placeholder)
   - Font Size: **72**, Bold
   - Color: Red/White
   - Position: Top-center

   **b) Stats Container:**
   - Create Empty as child
   - Name: "Stats Container"
   - Position: Center
   - Add Vertical Layout Group (optional)

   **c) Individual Stat Lines** (in container):
   - **Kills**: "Kills: 0"
   - **Headshots**: "Headshots: 0"
   - **Accuracy**: "Headshot Accuracy: 0.0%"
   - **Boss Kills**: "Boss Kills: 0"
   - **Waves**: "Waves Completed: 0"
   - **Highest Wave**: "Highest Wave: 0"
   - **Base Score**: "Base Score: 0"
   - **Final Score**: "FINAL SCORE: 0" (larger, bold)

   **d) Continue Button:**
   - UI → Button
   - Text: "Continue" or "Return to Menu"
   - Position: Bottom-center

3. **Add UIRunSummary Component** to panel:
   - Summary Panel: Drag the panel itself
   - Assign ALL text fields
   - Title Success: "RUN COMPLETE!"
   - Title Death: "YOU DIED"
   - Animate Numbers: **true**
   - Number Animation Duration: **1.0**

4. **Hook up Continue Button**:
   - Select button
   - In Inspector, find Button component
   - On Click() → Add UIRunSummary.OnContinue

---

### 6. Connect to Player Death

You need to call the summary screen when the player dies:

1. **Open Player GameObject** in Hierarchy
2. **Find PlayerHealth component**
3. **In OnDeath event**, add a call to show summary:
   - Click **+** to add event listener
   - Drag **UIRunSummary** GameObject
   - Select function: `UIRunSummary.ShowSummary(bool)`
   - Leave checkbox unchecked (= `false` for death)

**OR** add this code to `PlayerHealth.cs`:

```csharp
void Die()
{
    IsDead = true;
    OnDeath?.Invoke();
    BroadcastHealth();

    // Show run summary
    var summary = FindFirstObjectByType<UIRunSummary>();
    if (summary) summary.ShowSummary(false);

    if (destroyOnDeath) Destroy(gameObject);
}
```

---

## 💰 Score Breakdown

### Points Awarded:
- **Kill**: 100 points
- **Headshot Kill**: 150 points (100 + 50 bonus)
- **Boss Kill**: 500 points
- **Wave Complete**: 200 + (wave# × 10) points

### Final Score Bonuses:
- **Survival Bonus**: highestWave × 100
- **Accuracy Bonus**: +1000 if headshot accuracy ≥ 50%
- **Boss Slayer**: bossKills × 250

### Example Run:
```
- 50 kills = 5,000
- 25 headshots = 1,250 bonus
- 1 boss = 500
- 5 waves = 1,000 + (wave bonuses)
= Base: 7,750 points

Final bonuses:
+ Survival: 500 (wave 5)
+ Accuracy: 1,000 (50%+)
= FINAL: 9,250 points
```

---

## 🎯 Headshot Detection System

### How It Works:

1. **Weapon fires raycast** → Hits HeadshotZone collider
2. **HeadshotZone** multiplies damage (2x) and marks enemy
3. **EnemyHealth** detects marked headshot flag
4. **On death**, registers kill with headshot = true
5. **ScoreManager** awards bonus points

### Important Notes:
- Head hitboxes should be **triggers**
- Headshot multiplier applies damage boost
- Works with any weapon using IDamageable
- Visual feedback: Add blood/particle effects on headshot

---

## 🧪 Testing

1. **Start game with ScoreManager in scene**
2. **Kill enemies normally** → See +100 score
3. **Kill with headshot** → See +150 score (100 + 50)
4. **Complete wave** → See wave bonus
5. **Die** → Summary screen appears after 2 seconds
6. **Check stats** → Kills, headshots, accuracy calculated

### Debug Commands (right-click ScoreManager):
- "Debug: Register Kill"
- "Debug: Register Headshot"
- "Debug: Register Boss Kill"
- "Debug: Complete Wave"
- "Debug: Print Summary"

---

## 🔧 Common Issues

**No score increasing:**
- Check ScoreManager exists in scene
- Look for errors in Console
- Verify EnemyHealth has ScoreManager reference

**Headshots not detected:**
- Ensure head collider is **trigger**
- Check HeadshotZone has enemy root assigned
- Verify weapon is hitting head hitbox
- Use gizmos (select HeadshotZone to see red wireframe)

**Summary not showing:**
- Check panel is inactive at start
- Verify UIRunSummary has all references assigned
- Hook up player death event
- Check delay before show (default 2 seconds)

**Stats not updating UI:**
- Verify text fields are assigned in components
- Check ScoreManager OnScoreChanged events
- Look for null reference errors in Console

---

## 🎨 Visual Enhancements

### Headshot Feedback:
- Different hit sound for headshots
- Blood particle effects (red burst)
- Screen shake on headshot
- "+150" floating damage number

### Score Feedback:
- Screen flash on wave complete
- Sound effect for milestone scores
- Combo system for rapid kills
- Score multiplier display

---

## ✨ Next Steps & Enhancements

1. **Combo System** - Bonus for rapid consecutive kills
2. **Score Multipliers** - Build up multiplier for sustained performance
3. **Leaderboards** - Save and display high scores
4. **Challenges** - Bonus objectives for extra points
5. **Score-Based Unlocks** - Unlock weapons/characters with high scores
6. **Damage Numbers** - Floating text showing damage dealt
7. **Kill Feed** - List recent kills in UI

The foundation is complete! 🎉
