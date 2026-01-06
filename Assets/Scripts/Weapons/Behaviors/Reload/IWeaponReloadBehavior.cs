using System;

namespace Treachery.Weapons.Behaviors.Reload
{
    public interface IWeaponReloadBehavior
    {
        bool IsReloading { get; }

        bool CanReload(int currentAmmo, int maxAmmo, int reserveAmmo);

        void StartReload(
            int currentAmmo,
            int maxAmmo,
            int reserveAmmo,
            Action<int> onAmmoAdded,
            Action onReloadComplete,
            Action onReloadCancelled);

        void CancelReload();

        void Tick(float deltaTime);
    }
}
