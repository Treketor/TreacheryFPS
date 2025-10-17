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
    [SerializeField] bool autoFindWeapon = true;
    
    InputAction _attackAction;
    InputAction _reloadAction;

    void Awake()
    {
        if (playerInput != null)
        {
            _attackAction = playerInput.FindAction("Attack");
            _reloadAction = playerInput.FindAction("Reload");
            
            if (_attackAction == null)
                Debug.LogWarning("WeaponController: Attack action not found in InputActionAsset!");
            if (_reloadAction == null)
                Debug.LogWarning("WeaponController: Reload action not found in InputActionAsset!");
        }
        else
        {
            Debug.LogWarning("WeaponController: No InputActionAsset assigned!");
        }

        // Auto-find weapon if not assigned
        if (!currentWeapon && autoFindWeapon)
        {
            currentWeapon = GetComponentInChildren<WeaponInstance_Hitscan>();
            if (currentWeapon)
                Debug.Log($"WeaponController: Auto-found weapon '{currentWeapon.DisplayName}' in children");
        }

        if (currentWeapon)
        {
            SubscribeWeapon(currentWeapon);
            Debug.Log($"WeaponController: Current weapon set to {currentWeapon.DisplayName}");
        }
        else
        {
            Debug.LogWarning("WeaponController: No weapon assigned to currentWeapon field and none found in children!");
        }
    }

    void OnEnable()
    {
        // Enable input actions when this component is enabled
        if (playerInput != null)
        {
            playerInput.Enable();
        }
        
        _attackAction?.Enable();
        _reloadAction?.Enable();
    }

    void OnDisable()
    {
        // Disable input actions when this component is disabled
        _attackAction?.Disable();
        _reloadAction?.Disable();
        
        if (playerInput != null)
        {
            playerInput.Disable();
        }
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

    /// <summary>
    /// Get the currently equipped weapon.
    /// </summary>
    public WeaponInstance_Hitscan GetCurrentWeapon()
    {
        return currentWeapon;
    }
}