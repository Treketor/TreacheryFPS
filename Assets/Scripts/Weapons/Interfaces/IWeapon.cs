using UnityEngine;

namespace Treachery.Weapons.Interfaces
{
    /// <summary>
    /// Minimal runtime-facing weapon API.
    /// WeaponController should only talk to weapons through this interface.
    /// </summary>
    public interface IWeapon
    {
        string DisplayName { get; }
        string TierName { get; }

        int CurrentMag { get; }
        int CurrentReserve { get; }

        bool IsReloading { get; }
        bool IsReady { get; }
        bool IsAiming { get; }

        Sprite CrosshairSprite { get; }

        void Equip(Weapons.Runtime.WeaponContext context);
        void Unequip();

        void Tick(float deltaTime);

        void SetAiming(bool aiming);
        void TryFire();
        void TryReload();

        void CancelReload();
        void OnWeaponActivated();
        float TriggerSwitchOutAnimation();
    }
}
