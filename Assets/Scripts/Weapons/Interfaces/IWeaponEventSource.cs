using System;

namespace Treachery.Weapons.Interfaces
{
    /// <summary>
    /// Optional extension interface for weapons that publish ammo/tier changes.
    /// WeaponController can subscribe to these to drive UI.
    /// </summary>
    public interface IWeaponEventSource
    {
        event Action<int, int> AmmoChanged;
        event Action<string, string> TierChanged;
    }
}
