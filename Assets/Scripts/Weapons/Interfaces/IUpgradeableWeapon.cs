using UnityEngine;

namespace Treachery.Weapons.Interfaces
{
    /// <summary>
    /// Optional extension interface for weapons that support tier upgrades.
    /// Used by upgrade machines and UI without depending on a concrete weapon class.
    /// </summary>
    public interface IUpgradeableWeapon : IWeapon
    {
        WeaponTier CurrentTier { get; }

        bool CanUpgrade();
        bool TryUpgradeTier();
        int GetUpgradeCost();
    }
}
