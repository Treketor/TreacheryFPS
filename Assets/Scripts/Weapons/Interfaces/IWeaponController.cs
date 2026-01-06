using System;

namespace Treachery.Weapons.Interfaces
{
    /// <summary>
    /// Shared API for weapon controllers (Legacy and V2) so UI and gameplay systems
    /// can depend on a stable surface while the implementation migrates.
    /// </summary>
    public interface IWeaponController
    {
        event Action<int, int> AmmoChanged; // in-mag, reserve
        event Action<string, string> WeaponChanged; // display name, tier name
        event Action<int, int> WeaponSlotChanged; // active slot, total slots

        IWeapon CurrentWeapon { get; }

        int CurrentAmmoInMag { get; }
        int CurrentReserveAmmo { get; }
        string CurrentWeaponDisplayName { get; }
        string CurrentTierName { get; }

        int ActiveWeaponIndex { get; }
        int WeaponSlotCount { get; }

        IWeapon GetWeaponInSlot(int slotIndex);
    }
}
