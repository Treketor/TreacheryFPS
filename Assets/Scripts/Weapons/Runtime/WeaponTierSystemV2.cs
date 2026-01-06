using UnityEngine;

namespace Treachery.Weapons.Runtime
{
    /// <summary>
    /// Tier data for V2 (ScriptableObject-driven) weapons.
    /// Mirrors the legacy tier system but is namespaced for the new weapon runtime.
    /// </summary>
    public static class WeaponTierSystemV2
    {
        public static WeaponTierData GetTierData(WeaponTier tier)
        {
            // Reuse the existing data struct + colors used in UI.
            return WeaponTierSystem.GetTierData(tier);
        }

        public static bool CanUpgrade(WeaponTier tier) => WeaponTierSystem.CanUpgrade(tier);

        public static WeaponTier? GetNextTier(WeaponTier tier) => WeaponTierSystem.GetNextTier(tier);

        public static int GetUpgradeCost(WeaponTier tier) => WeaponTierSystem.GetUpgradeCost(tier);
    }
}
