using UnityEngine;

/// <summary>
/// Defines weapon tiers for the upgrade system.
/// </summary>
public enum WeaponTier
{
    Common = 0,
    Rare = 1,
    Epic = 2,
    Legendary = 3
}

/// <summary>
/// Data structure for weapon tier upgrade information.
/// Contains the stat multipliers and costs for each tier.
/// </summary>
[System.Serializable]
public class WeaponTierData
{
    public WeaponTier tier;
    public string tierName;
    public Color tierColor;
    public int upgradeCost;
    
    [Header("Stat Multipliers")]
    [Tooltip("Multiplier for weapon damage")]
    public float damageMultiplier = 1f;
    [Tooltip("Multiplier for fire rate")]
    public float fireRateMultiplier = 1f;
    [Tooltip("Multiplier for magazine size")]
    public float magSizeMultiplier = 1f;
    [Tooltip("Multiplier for reload time (lower is better)")]
    public float reloadTimeMultiplier = 1f;
    [Tooltip("Multiplier for spread/accuracy (lower is better)")]
    public float spreadMultiplier = 1f;

    public WeaponTierData(WeaponTier tier, string name, Color color, int cost, 
        float damage, float fireRate, float magSize, float reload, float spread)
    {
        this.tier = tier;
        this.tierName = name;
        this.tierColor = color;
        this.upgradeCost = cost;
        this.damageMultiplier = damage;
        this.fireRateMultiplier = fireRate;
        this.magSizeMultiplier = magSize;
        this.reloadTimeMultiplier = reload;
        this.spreadMultiplier = spread;
    }
}

/// <summary>
/// Static utility for weapon tier information and calculations.
/// </summary>
public static class WeaponTierSystem
{
    // Default tier configurations based on GDD
    public static readonly WeaponTierData[] TierData = new WeaponTierData[]
    {
        // Common (base stats)
        new WeaponTierData(
            WeaponTier.Common,
            "Common",
            new Color(0.8f, 0.8f, 0.8f), // Gray
            100,  // Cost to upgrade to Rare
            1.0f, // damage
            1.0f, // fireRate
            1.0f, // magSize
            1.0f, // reloadTime
            1.0f  // spread
        ),
        
        // Rare
        new WeaponTierData(
            WeaponTier.Rare,
            "Rare",
            new Color(0.3f, 0.5f, 1.0f), // Blue
            250,  // Cost to upgrade to Epic
            1.5f,  // +50% damage
            1.2f,  // +20% fire rate
            1.3f,  // +30% mag size
            0.85f, // -15% reload time
            0.9f   // -10% spread (better accuracy)
        ),
        
        // Epic
        new WeaponTierData(
            WeaponTier.Epic,
            "Epic",
            new Color(0.8f, 0.2f, 0.8f), // Purple
            500,  // Cost to upgrade to Legendary
            2.5f,  // +150% damage
            1.5f,  // +50% fire rate
            1.6f,  // +60% mag size
            0.7f,  // -30% reload time
            0.7f   // -30% spread
        ),
        
        // Legendary
        new WeaponTierData(
            WeaponTier.Legendary,
            "Legendary",
            new Color(1.0f, 0.8f, 0.0f), // Gold
            0,     // No further upgrades
            4.0f,  // +300% damage
            2.0f,  // +100% fire rate
            2.0f,  // +100% mag size
            0.5f,  // -50% reload time
            0.5f   // -50% spread
        )
    };

    /// <summary>
    /// Get tier data for a specific tier.
    /// </summary>
    public static WeaponTierData GetTierData(WeaponTier tier)
    {
        return TierData[(int)tier];
    }

    /// <summary>
    /// Get the next tier, or null if already at max tier.
    /// </summary>
    public static WeaponTier? GetNextTier(WeaponTier currentTier)
    {
        if (currentTier == WeaponTier.Legendary)
            return null;
        
        return (WeaponTier)((int)currentTier + 1);
    }

    /// <summary>
    /// Check if a tier can be upgraded further.
    /// </summary>
    public static bool CanUpgrade(WeaponTier tier)
    {
        return tier != WeaponTier.Legendary;
    }

    /// <summary>
    /// Get the cost to upgrade from current tier to next tier.
    /// </summary>
    public static int GetUpgradeCost(WeaponTier currentTier)
    {
        if (currentTier == WeaponTier.Legendary)
            return 0;
        
        return TierData[(int)currentTier].upgradeCost;
    }

    /// <summary>
    /// Get tier color for UI display.
    /// </summary>
    public static Color GetTierColor(WeaponTier tier)
    {
        return TierData[(int)tier].tierColor;
    }

    /// <summary>
    /// Parse a tier name string to WeaponTier enum.
    /// </summary>
    public static WeaponTier ParseTierName(string tierName)
    {
        for (int i = 0; i < TierData.Length; i++)
        {
            if (TierData[i].tierName.Equals(tierName, System.StringComparison.OrdinalIgnoreCase))
                return TierData[i].tier;
        }
        
        return WeaponTier.Common; // Default to common
    }
}
