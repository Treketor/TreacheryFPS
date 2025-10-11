using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    public WeaponInstance_Hitscan currentWeapon;

    public System.Action<int, int> OnAmmoChanged; // in-mag, reserve
    public System.Action<string, string> OnWeaponChanged; // display name, tier name

    public int CurrentAmmoInMag => currentWeapon ? currentWeapon.CurrentMag : 0;
    public int CurrentReserveAmmo => currentWeapon ? currentWeapon.CurrentReserve : -1;
    public string CurrentWeaponDisplayName => currentWeapon ? currentWeapon.DisplayName : "—";
    public string CurrentTierName => currentWeapon ? currentWeapon.TierName : "—";

    [SerializeField] InputActionAsset playerInput;
    InputAction _attackAction;
    InputAction _reloadAction;

    void Awake()
    {
        if (playerInput != null)
        {
            _attackAction = playerInput.FindAction("Attack");
            _reloadAction = playerInput.FindAction("Reload");
        }

        if (currentWeapon) SubscribeWeapon(currentWeapon);
    }

    void Update()
    {
        if (!currentWeapon) return;

        if (_attackAction != null && _attackAction.IsPressed())
            currentWeapon.TryFire();

        if (_reloadAction != null && _reloadAction.WasPressedThisFrame())
            currentWeapon.TryReload();
    }

    public void SetCurrentWeapon(WeaponInstance_Hitscan newWpn)
    {
        if (currentWeapon) UnsubscribeWeapon(currentWeapon);
        currentWeapon = newWpn;
        if (currentWeapon) SubscribeWeapon(currentWeapon);
        OnWeaponChanged?.Invoke(CurrentWeaponDisplayName, CurrentTierName);
        OnAmmoChanged?.Invoke(CurrentAmmoInMag, CurrentReserveAmmo);
    }

    void SubscribeWeapon(WeaponInstance_Hitscan w)
    {
        w.OnAmmoChanged += RelayAmmo;
        w.OnTierChanged += RelayTier;
    }

    void UnsubscribeWeapon(WeaponInstance_Hitscan w)
    {
        w.OnAmmoChanged -= RelayAmmo;
        w.OnTierChanged -= RelayTier;
    }
    
    void RelayAmmo(int inMag, int reserve) => OnAmmoChanged?.Invoke(inMag, reserve);
    void RelayTier(string displayName, string tierName)
    {
        OnWeaponChanged?.Invoke(displayName, tierName);
        OnAmmoChanged?.Invoke(CurrentAmmoInMag, CurrentReserveAmmo);
    }
}