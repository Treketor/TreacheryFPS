using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [Header("Weapon Slots")]
    [SerializeField] WeaponInstance_Hitscan[] weaponSlots = new WeaponInstance_Hitscan[2];
    [SerializeField] int activeWeaponIndex = 0;

    public System.Action<int, int> OnAmmoChanged; // in-mag, reserve
    public System.Action<string, string> OnWeaponChanged; // display name, tier name
    public System.Action<int, int> OnWeaponSlotChanged; // current slot, total slots

    // Properties for current active weapon
    public WeaponInstance_Hitscan CurrentWeapon => (activeWeaponIndex >= 0 && activeWeaponIndex < weaponSlots.Length) ? weaponSlots[activeWeaponIndex] : null;
    public int CurrentAmmoInMag => CurrentWeapon ? CurrentWeapon.CurrentMag : 0;
    public int CurrentReserveAmmo => CurrentWeapon ? CurrentWeapon.CurrentReserve : -1;
    public string CurrentWeaponDisplayName => CurrentWeapon ? CurrentWeapon.DisplayName : "—";
    public string CurrentTierName => CurrentWeapon ? CurrentWeapon.TierName : "—";
    public int ActiveWeaponIndex => activeWeaponIndex;
    public int WeaponSlotCount => weaponSlots.Length;
    public bool IsSwitching => _isSwitching;

    [Header("Input")]
    [SerializeField] InputActionAsset playerInput;
    [SerializeField] bool autoFindWeapons = true;
    
    [Header("Weapon Switching")]
    [SerializeField] bool allowSwitchDuringReload = true;
    
    private bool _isSwitching = false;
    
    InputAction _attackAction;
    InputAction _reloadAction;
    InputAction _switchWeaponAction;
    InputAction _aimAction;

    void Awake()
    {
        if (playerInput != null)
        {
            _attackAction = playerInput.FindAction("Attack");
            _reloadAction = playerInput.FindAction("Reload");
            _switchWeaponAction = playerInput.FindAction("SwitchWeapon");
            _aimAction = playerInput.FindAction("Aim");
            
            if (_attackAction == null)
                Debug.LogWarning("WeaponController: Attack action not found in InputActionAsset!");
            if (_reloadAction == null)
                Debug.LogWarning("WeaponController: Reload action not found in InputActionAsset!");
            if (_switchWeaponAction == null)
                Debug.LogWarning("WeaponController: SwitchWeapon action not found in InputActionAsset!");
            if (_aimAction == null)
                Debug.LogWarning("WeaponController: Aim action not found in InputActionAsset!");
        }
        else
        {
            Debug.LogWarning("WeaponController: No InputActionAsset assigned!");
        }

        // Auto-find weapons if slots are empty
        if (autoFindWeapons)
        {
            var foundWeapons = GetComponentsInChildren<WeaponInstance_Hitscan>();
            for (int i = 0; i < Mathf.Min(foundWeapons.Length, weaponSlots.Length); i++)
            {
                if (weaponSlots[i] == null)
                {
                    weaponSlots[i] = foundWeapons[i];
                    Debug.Log($"WeaponController: Auto-found weapon '{foundWeapons[i].DisplayName}' for slot {i}");
                }
            }
        }

        // Initialize weapon slots
        InitializeWeapons();
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
        _aimAction?.Enable();
    }

    void OnDisable()
    {
        // Disable input actions when this component is disabled
        _attackAction?.Disable();
        _reloadAction?.Disable();
        _aimAction?.Disable();
        
        if (playerInput != null)
        {
            playerInput.Disable();
        }
    }

    void Update()
    {
        if (CurrentWeapon == null) return;

        // Don't process input if game is paused
        if (PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused)
            return;

        // Don't allow any weapon actions during switching
        if (_isSwitching) return;

        if (_attackAction != null && _attackAction.IsPressed())
            CurrentWeapon.TryFire();

        if (_reloadAction != null && _reloadAction.WasPressedThisFrame())
            CurrentWeapon.TryReload();

        // Handle ADS input BEFORE weapon switching
        bool isAiming = _aimAction != null && _aimAction.IsPressed();
        bool switchPressed = _switchWeaponAction != null && _switchWeaponAction.WasPressedThisFrame();
        
        // If switching weapons, force ADS off
        if (switchPressed)
        {
            isAiming = false;
            if (CurrentWeapon != null)
            {
                Debug.Log($"WeaponController: Canceling ADS on {CurrentWeapon.DisplayName} due to weapon switch input");
            }
            SwitchToNextWeapon();
        }
        
        // Apply ADS state to current weapon
        if (CurrentWeapon != null)
            CurrentWeapon.SetAiming(isAiming);
    }

    void InitializeWeapons()
    {
        // Hide all weapons initially
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (weaponSlots[i] != null)
            {
                weaponSlots[i].gameObject.SetActive(false);
            }
        }

        // Find first available weapon and set as active (with proper activation delay)
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (weaponSlots[i] != null)
            {
                // Set active weapon index to invalid value first, then switch to ensure activation
                activeWeaponIndex = -1;
                SwitchToWeapon(i, true); // true = apply switch delay even on first weapon
                break;
            }
        }
    }

    public void SwitchToWeapon(int weaponIndex, bool applySwitchDelay = true)
    {
        if (weaponIndex < 0 || weaponIndex >= weaponSlots.Length || weaponSlots[weaponIndex] == null)
            return;

        // Prevent switching if already switching or if current weapon index is the same
        if (_isSwitching || weaponIndex == activeWeaponIndex)
            return;

        // Prevent switching if current weapon is not ready (still in switch cooldown)
        if (CurrentWeapon != null && !CurrentWeapon.IsReady)
        {
            Debug.Log("Cannot switch weapons while current weapon is not ready");
            return;
        }

        // Check if switching during reload is allowed
        if (!allowSwitchDuringReload && CurrentWeapon != null && CurrentWeapon.IsReloading)
        {
            Debug.Log("Cannot switch weapons while reloading");
            return;
        }

        // Start the switch coroutine
        StartCoroutine(SwitchWeaponCoroutine(weaponIndex, applySwitchDelay));
    }

    private System.Collections.IEnumerator SwitchWeaponCoroutine(int weaponIndex, bool applySwitchDelay)
    {
        _isSwitching = true;

        // If we have a current weapon, trigger its switch-out animation first
        if (CurrentWeapon != null)
        {
            // Cancel reload if weapon is currently reloading
            if (CurrentWeapon.IsReloading && allowSwitchDuringReload)
            {
                CurrentWeapon.CancelReload();
                Debug.Log($"Cancelled reload on {CurrentWeapon.DisplayName} due to weapon switch");
            }

            // Trigger switch-out animation and wait for it
            float switchOutDelay = CurrentWeapon.TriggerSwitchOutAnimation();
            yield return new WaitForSeconds(switchOutDelay);

            UnsubscribeWeapon(CurrentWeapon);
            CurrentWeapon.gameObject.SetActive(false);
        }

        // Switch to new weapon
        activeWeaponIndex = weaponIndex;
        CurrentWeapon.gameObject.SetActive(true);
        SubscribeWeapon(CurrentWeapon);

        // Start switch delay on newly activated weapon (if enabled)
        if (applySwitchDelay)
        {
            CurrentWeapon.OnWeaponActivated();
        }

        // Notify UI
        OnWeaponChanged?.Invoke(CurrentWeaponDisplayName, CurrentTierName);
        OnAmmoChanged?.Invoke(CurrentAmmoInMag, CurrentReserveAmmo);
        OnWeaponSlotChanged?.Invoke(activeWeaponIndex, weaponSlots.Length);
        
        // Update crosshair for new weapon
        if (CrosshairManager.Instance != null)
        {
            CrosshairManager.Instance.UpdateCrosshair();
        }

        Debug.Log($"Switched to weapon slot {weaponIndex}: {CurrentWeaponDisplayName}");

        _isSwitching = false;
    }

    public void SwitchToNextWeapon()
    {
        // Find next available weapon slot
        int startIndex = activeWeaponIndex;
        int nextIndex = (activeWeaponIndex + 1) % weaponSlots.Length;
        
        while (nextIndex != startIndex)
        {
            if (weaponSlots[nextIndex] != null)
            {
                SwitchToWeapon(nextIndex);
                return;
            }
            nextIndex = (nextIndex + 1) % weaponSlots.Length;
        }
    }

    public void AddWeaponToSlot(WeaponInstance_Hitscan weapon, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= weaponSlots.Length)
            return;

        weaponSlots[slotIndex] = weapon;
        weapon.gameObject.SetActive(false); // Hide by default

        Debug.Log($"Added weapon '{weapon.DisplayName}' to slot {slotIndex}");
    }

    public WeaponInstance_Hitscan GetWeaponInSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= weaponSlots.Length)
            return null;

        return weaponSlots[slotIndex];
    }

    public bool HasWeaponInSlot(int slotIndex)
    {
        return GetWeaponInSlot(slotIndex) != null;
    }

    // Legacy method for compatibility (now sets weapon in slot 0)
    public void SetCurrentWeapon(WeaponInstance_Hitscan newWpn)
    {
        AddWeaponToSlot(newWpn, 0);
        SwitchToWeapon(0);
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
        return CurrentWeapon;
    }
}