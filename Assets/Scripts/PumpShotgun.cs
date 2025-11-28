using UnityEngine;

/// <summary>
/// Example pump shotgun weapon that demonstrates single bullet reloading.
/// This script shows how to configure a weapon to use the single bullet reload behavior.
/// </summary>
public class PumpShotgun : MonoBehaviour
{
    [Header("Pump Shotgun Setup Instructions")]
    [TextArea(10, 15)]
    public string setupInstructions = @"PUMP SHOTGUN SETUP INSTRUCTIONS:

1. WEAPON CONFIGURATION:
   - Set Reload Type to 'SingleBullet' in WeaponInstance_Hitscan
   - Configure the Single Bullet Reload Settings section
   - Enable 'Use Pellet System' for shotgun behavior
   - Set Pellets Per Shot (8-12 recommended for shotgun)
   - Set Pellet Spread Multiplier (2.0-4.0 for wide spread)
   - Set Pellet Damage Multiplier (1.0 for normal distributed damage)

2. ANIMATION SETUP:
   - Create 3 animation clips in your Animator:
     * StartReload - Weapon lifts up (0.8s recommended)
     * ReloadSingleBullet - One bullet loaded (0.6s recommended)  
     * FinishReload - Weapon cocks/pumps (0.5s recommended)

3. ANIMATOR TRIGGERS:
   - Add these trigger parameters to your Animator Controller:
     * StartReload
     * ReloadSingleBullet
     * FinishReload

4. BEHAVIOR:
   - Player can reload multiple bullets one at a time
   - Can interrupt reload by shooting with partial ammo
   - Auto-finishes reload when magazine is full
   - Suitable for shotguns, revolvers, and similar weapons

5. RECOMMENDED SETTINGS:
   - Start Reload Duration: 0.8s
   - Single Bullet Reload Duration: 0.6s
   - Finish Reload Duration: 0.5s
   - Auto Finish When Full: Enabled
   - Auto Finish Delay: 0.3s
   - Use Pellet System: Enabled
   - Pellets Per Shot: 8-10 pellets
   - Pellet Spread Multiplier: 3.0x
   - Pellet Damage Multiplier: 1.0x (distributed damage)";

    void Start()
    {
        // Get the weapon component
        var weapon = GetComponent<WeaponInstance_Hitscan>();
        if (weapon != null)
        {
            Debug.Log($"Pump Shotgun '{weapon.DisplayName}' initialized with {(weapon.IsReloading ? "Single Bullet" : "Magazine")} reload behavior");
        }
        else
        {
            Debug.LogError("PumpShotgun: No WeaponInstance_Hitscan component found! Please add this script to a weapon object.");
        }
    }

    [ContextMenu("Test Single Bullet Reload")]
    void TestSingleBulletReload()
    {
        var weapon = GetComponent<WeaponInstance_Hitscan>();
        if (weapon != null)
        {
            weapon.TryReload();
            Debug.Log($"Started single bullet reload for {weapon.DisplayName}");
        }
    }

    [ContextMenu("Test Interrupt Reload")]
    void TestInterruptReload()
    {
        var weapon = GetComponent<WeaponInstance_Hitscan>();
        if (weapon != null)
        {
            bool interrupted = weapon.TryInterruptReloadForShooting();
            Debug.Log($"Interrupt reload result: {interrupted}");
        }
    }

    [ContextMenu("Show Pellet Info")]
    void ShowPelletInfo()
    {
        var weapon = GetComponent<WeaponInstance_Hitscan>();
        if (weapon != null)
        {
            Debug.Log($"=== {weapon.DisplayName} Pellet System Info ===");
            Debug.Log($"Uses Pellet System: {weapon.UsesPelletSystem}");
            Debug.Log($"Pellets Per Shot: {weapon.PelletsPerShot}");
            Debug.Log($"Damage Per Pellet: {weapon.PelletDamagePerPellet:F1}");
            Debug.Log($"Total Potential Damage: {weapon.PelletDamagePerPellet * weapon.PelletsPerShot:F1}");
        }
    }

    [ContextMenu("Test Fire Pellets")]
    void TestFirePellets()
    {
        var weapon = GetComponent<WeaponInstance_Hitscan>();
        if (weapon != null)
        {
            weapon.TryFire();
            Debug.Log($"Fired {weapon.PelletsPerShot} pellets from {weapon.DisplayName}");
        }
    }
}