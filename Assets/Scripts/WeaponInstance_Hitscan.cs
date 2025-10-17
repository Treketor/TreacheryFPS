using System.Collections;
using UnityEngine;

public class WeaponInstance_Hitscan : MonoBehaviour
{
    [Header("Base Stats")]
    public string displayName = "Pistol";
    
    [Header("Base Stats (These get multiplied by tier)")]
    [Tooltip("Base damage per shot")]
    public float baseDamage = 20f;
    [Tooltip("Base fire rate (shots per second)")]
    public float baseFireRate = 5f;
    [Tooltip("Base magazine size")]
    public int baseMagSize = 12;
    [Tooltip("Base reload time in seconds")]
    public float baseReloadTime = 1.2f;
    [Tooltip("Base spread/accuracy")]
    public float baseSpread = 1.5f;
    
    [Header("Current Tier")]
    [SerializeField] WeaponTier currentTier = WeaponTier.Common;
    
    // Current stats (calculated from base * tier multipliers)
    float damage;
    float fireRate;
    int magSize;
    float reloadTime;
    float spread;

    [Header("Ammo Pools")]
    public int startingReserve = 60;

    [Header("References")]
    public WeaponRaycaster raycaster;
    public LayerMask hitMask;

    float _cooldown;
    int _inMag;
    int _reserve;
    bool _reloading;

    public System.Action<int, int> OnAmmoChanged;
    public System.Action<string, string> OnTierChanged;
    public System.Action<WeaponTier> OnTierUpgraded;

    public string DisplayName => displayName;
    public string TierName => WeaponTierSystem.GetTierData(currentTier).tierName;
    public WeaponTier CurrentTier => currentTier;
    public int CurrentMag => _inMag;
    public int CurrentReserve => _reserve;
    public float CurrentDamage => damage;
    public float CurrentFireRate => fireRate;

    void Awake()
    {
        // Calculate initial stats based on tier
        RecalculateStats();
        _inMag = magSize;
        _reserve = Mathf.Max(0, startingReserve);

        // Validate raycaster reference
        if (raycaster == null)
        {
            Debug.LogError($"WeaponInstance_Hitscan ({displayName}): Raycaster is not assigned! Weapon will not be able to shoot.", this);
        }
    }

    void Update()
    {
        if (_cooldown > 0f) _cooldown -= Time.deltaTime;
    }

    public void TryFire()
    {
        if (_reloading || _cooldown > 0f) return;

        // Auto-reload if magazine is empty
        if (_inMag <= 0)
        {
            TryReload();
            return;
        }

        _cooldown = 1f / fireRate;
        _inMag--;
        OnAmmoChanged?.Invoke(_inMag, _reserve);

        // Register shot fired for accuracy tracking
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.RegisterShotFired();
        }

        // Check if raycaster is assigned
        if (raycaster == null)
        {
            Debug.LogError($"WeaponInstance_Hitscan ({displayName}): Cannot shoot - raycaster is null!", this);
            return;
        }

        if (raycaster.TryShoot(out var hit, spread))
        {
            bool isHeadshot = false;
            float finalDamage = damage;

            // Check for headshot zone first
            if (hit.collider.TryGetComponent<HeadshotZone>(out var headshotZone))
            {
                isHeadshot = headshotZone.ProcessHeadshot(damage, hit.point, hit.normal, out finalDamage);
                
                // Register headshot for accuracy tracking
                if (isHeadshot && ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.RegisterHeadshot();
                }
            }
            // Otherwise check for regular IDamageable on the hit object or its parent
            else 
            {
                // Try to find IDamageable on hit object or parent hierarchy
                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable == null)
                    damageable = hit.collider.GetComponentInParent<IDamageable>();

                if (damageable != null)
                {
                    damageable.TakeDamage(damage, hit.point, hit.normal);
                }
            }

            // TODO: spawn impact FX at hit.point (different FX for headshot?)
        }
        // TODO: spawn muzzle flash FX and sound effect
    }

    public void TryReload()
    {
        if (_reloading) return;
        if (_inMag >= magSize) return; // already full
        if (_reserve <= 0) return; // no reserve ammo

        StartCoroutine(ReloadCoroutine());
    }

    IEnumerator ReloadCoroutine()
    {
        _reloading = true;
        // play reload animation/sound here
        yield return new WaitForSeconds(reloadTime);

        int needed = magSize - _inMag;
        int taken = Mathf.Min(needed, _reserve);
        _inMag += taken;
        _reserve -= taken;

        _reloading = false;
        OnAmmoChanged?.Invoke(_inMag, _reserve);
    }

    /// <summary>
    /// Recalculate all stats based on current tier.
    /// </summary>
    void RecalculateStats()
    {
        WeaponTierData tierData = WeaponTierSystem.GetTierData(currentTier);
        
        damage = baseDamage * tierData.damageMultiplier;
        fireRate = baseFireRate * tierData.fireRateMultiplier;
        magSize = Mathf.RoundToInt(baseMagSize * tierData.magSizeMultiplier);
        reloadTime = baseReloadTime * tierData.reloadTimeMultiplier;
        spread = baseSpread * tierData.spreadMultiplier;
    }

    /// <summary>
    /// Upgrade weapon to the next tier.
    /// </summary>
    public bool TryUpgradeTier()
    {
        if (!WeaponTierSystem.CanUpgrade(currentTier))
        {
            Debug.Log($"{displayName} is already at max tier (Legendary)!");
            return false;
        }

        WeaponTier? nextTier = WeaponTierSystem.GetNextTier(currentTier);
        if (nextTier.HasValue)
        {
            currentTier = nextTier.Value;
            RecalculateStats();
            
            // Refill magazine on upgrade
            _inMag = magSize;
            
            OnTierChanged?.Invoke(displayName, TierName);
            OnTierUpgraded?.Invoke(currentTier);
            OnAmmoChanged?.Invoke(_inMag, _reserve);
            
            Debug.Log($"{displayName} upgraded to {TierName}!");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Get the cost to upgrade this weapon to the next tier.
    /// </summary>
    public int GetUpgradeCost()
    {
        return WeaponTierSystem.GetUpgradeCost(currentTier);
    }

    /// <summary>
    /// Check if this weapon can be upgraded.
    /// </summary>
    public bool CanUpgrade()
    {
        return WeaponTierSystem.CanUpgrade(currentTier);
    }

    // Legacy method for compatibility (deprecated)
    [System.Obsolete("Use TryUpgradeTier() instead")]
    public void ApplyTier(string newTierName, int newMagSize, float newDamage, float newReloadTime, float newSpread)
    {
        currentTier = WeaponTierSystem.ParseTierName(newTierName);
        RecalculateStats();
        _inMag = Mathf.Min(_inMag, magSize);
        OnTierChanged?.Invoke(displayName, TierName);
        OnAmmoChanged?.Invoke(_inMag, _reserve);
    }

    public void AddReserve(int amount)
    {
        _reserve = Mathf.Max(0, _reserve + amount);
        OnAmmoChanged?.Invoke(_inMag, _reserve);
    }
}