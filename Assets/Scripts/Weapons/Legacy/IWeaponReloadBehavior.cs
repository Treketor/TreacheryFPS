using UnityEngine;

/// <summary>
/// Interface for different weapon reload behaviors
/// </summary>
public interface IWeaponReloadBehavior
{
    bool IsReloading { get; }
    bool CanReload(int currentAmmo, int maxAmmo, int reserveAmmo);
    void StartReload(int currentAmmo, int maxAmmo, int reserveAmmo, System.Action<int> onAmmoAdded, System.Action onReloadComplete, System.Action onReloadCancelled);
    void CancelReload();
    void Update();
}