using UnityEngine;
using UnityEngine.InputSystem;
using Treachery.Weapons.Interfaces;
using Treachery.Weapons.Runtime;
using Treachery.Weapons.View;

public class WeaponController : MonoBehaviour, IWeaponController
{
    [Header("Weapon Slots")]
    [SerializeField] MonoBehaviour[] weaponSlots = new MonoBehaviour[2];
    [SerializeField] int activeWeaponIndex = 0;

    public System.Action<int, int> OnAmmoChanged; // in-mag, reserve
    public System.Action<string, string> OnWeaponChanged; // display name, tier name
    public System.Action<int, int> OnWeaponSlotChanged; // current slot, total slots

    event System.Action<int, int> IWeaponController.AmmoChanged
    {
        add => OnAmmoChanged += value;
        remove => OnAmmoChanged -= value;
    }

    event System.Action<string, string> IWeaponController.WeaponChanged
    {
        add => OnWeaponChanged += value;
        remove => OnWeaponChanged -= value;
    }

    event System.Action<int, int> IWeaponController.WeaponSlotChanged
    {
        add => OnWeaponSlotChanged += value;
        remove => OnWeaponSlotChanged -= value;
    }

    // Properties for current active weapon
    public IWeapon CurrentWeapon => GetWeaponRuntime(activeWeaponIndex);
    public int CurrentAmmoInMag => CurrentWeapon != null ? CurrentWeapon.CurrentMag : 0;
    public int CurrentReserveAmmo => CurrentWeapon != null ? CurrentWeapon.CurrentReserve : -1;
    public string CurrentWeaponDisplayName => CurrentWeapon != null ? CurrentWeapon.DisplayName : "—";
    public string CurrentTierName => CurrentWeapon != null ? CurrentWeapon.TierName : "—";
    public int ActiveWeaponIndex => activeWeaponIndex;
    public int WeaponSlotCount => weaponSlots.Length;
    public bool IsSwitching => _isSwitching;

    [Header("Input")]
    [SerializeField] InputActionAsset playerInput;
    [SerializeField] bool autoFindWeapons = true;
    
    [Header("Weapon Switching")]
    [SerializeField] bool allowSwitchDuringReload = true;

    [Header("Runtime Dependencies (Refactor)")]
    [SerializeField] WeaponPresentationController presentation;
    [SerializeField] PlayerMovement playerMovement;
    [SerializeField] bool autoFindRuntimeDependencies = true;
    
    private bool _isSwitching = false;
    
    InputAction _attackAction;
    InputAction _reloadAction;
    InputAction _switchWeaponAction;
    InputAction _aimAction;
    InputAction _scrollWheelAction;
    
    // Number key actions for direct weapon selection
    InputAction _weapon1Action;
    InputAction _weapon2Action;
    InputAction _weapon3Action;
    InputAction _weapon4Action;

    void Awake()
    {
        if (autoFindRuntimeDependencies)
        {
            if (presentation == null)
                presentation = FindFirstObjectByType<WeaponPresentationController>();
            if (playerMovement == null)
                playerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        if (playerInput != null)
        {
            _attackAction = playerInput.FindAction("Attack");
            _reloadAction = playerInput.FindAction("Reload");
            _switchWeaponAction = playerInput.FindAction("SwitchWeapon");
            _aimAction = playerInput.FindAction("Aim");
            _scrollWheelAction = playerInput.FindAction("ScrollWheel");
            
            // Try to find number key actions (may not exist in input asset)
            _weapon1Action = playerInput.FindAction("Weapon1");
            _weapon2Action = playerInput.FindAction("Weapon2");
            _weapon3Action = playerInput.FindAction("Weapon3");
            _weapon4Action = playerInput.FindAction("Weapon4");
            
            if (_attackAction == null)
                Debug.LogWarning("WeaponController: Attack action not found in InputActionAsset!");
            if (_reloadAction == null)
                Debug.LogWarning("WeaponController: Reload action not found in InputActionAsset!");
            if (_switchWeaponAction == null)
                Debug.LogWarning("WeaponController: SwitchWeapon action not found in InputActionAsset!");
            if (_aimAction == null)
                Debug.LogWarning("WeaponController: Aim action not found in InputActionAsset!");
            if (_scrollWheelAction == null)
                Debug.LogWarning("WeaponController: ScrollWheel action not found in InputActionAsset!");
        }
        else
        {
            Debug.LogWarning("WeaponController: No InputActionAsset assigned!");
        }

        // Auto-find weapons if slots are empty
        if (autoFindWeapons)
        {
            var foundBehaviours = GetComponentsInChildren<MonoBehaviour>(true);
            int writeIndex = 0;
            for (int i = 0; i < foundBehaviours.Length && writeIndex < weaponSlots.Length; i++)
            {
                if (foundBehaviours[i] is not IWeapon weapon)
                    continue;

                // Fill only empty slots.
                while (writeIndex < weaponSlots.Length && weaponSlots[writeIndex] != null)
                    writeIndex++;

                if (writeIndex >= weaponSlots.Length)
                    break;

                weaponSlots[writeIndex] = foundBehaviours[i];
                Debug.Log($"WeaponController: Auto-found weapon '{weapon.DisplayName}' for slot {writeIndex}");
                writeIndex++;
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
        
        // Handle scroll wheel weapon switching
        float scrollValue = 0f;
        if (_scrollWheelAction != null)
        {
            Vector2 scrollInput = _scrollWheelAction.ReadValue<Vector2>();
            scrollValue = scrollInput.y; // Y component is the scroll wheel
        }
        
        // Handle number key weapon switching
        int directWeaponIndex = -1;
        if (_weapon1Action != null && _weapon1Action.WasPressedThisFrame()) directWeaponIndex = 0;
        else if (_weapon2Action != null && _weapon2Action.WasPressedThisFrame()) directWeaponIndex = 1;
        else if (_weapon3Action != null && _weapon3Action.WasPressedThisFrame()) directWeaponIndex = 2;
        else if (_weapon4Action != null && _weapon4Action.WasPressedThisFrame()) directWeaponIndex = 3;
        
        // If switching weapons, force ADS off
        if (directWeaponIndex >= 0) // Number key pressed
        {
            isAiming = false;
            
            // Switch directly to the specified weapon slot if it has a weapon
            if (directWeaponIndex < weaponSlots.Length && weaponSlots[directWeaponIndex] != null)
            {
                SwitchToWeapon(directWeaponIndex);
            }
        }
        else if (switchPressed)
        {
            isAiming = false;
            SwitchToNextWeapon();
        }
        else if (Mathf.Abs(scrollValue) > 0.1f) // Scroll wheel threshold
        {
            isAiming = false;
            
            // Scroll up = previous weapon, scroll down = next weapon
            if (scrollValue > 0)
            {
                SwitchToPreviousWeapon();
            }
            else
            {
                SwitchToNextWeapon();
            }
        }
        
        // Apply ADS state to current weapon
        if (CurrentWeapon != null)
            CurrentWeapon.SetAiming(isAiming);

        // Let the weapon update internal systems (reload, bloom, etc.) via the interface.
        CurrentWeapon?.Tick(Time.deltaTime);
    }

    void InitializeWeapons()
    {
        // Hide all weapons initially
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            var comp = weaponSlots[i];
            if (comp != null)
                comp.gameObject.SetActive(false);
        }

        // Find first available weapon and set as active (with proper activation delay)
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (GetWeaponRuntime(i) != null)
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
        if (weaponIndex < 0 || weaponIndex >= weaponSlots.Length || GetWeaponRuntime(weaponIndex) == null)
            return;

        // Prevent switching if already switching or if current weapon index is the same
        if (_isSwitching || weaponIndex == activeWeaponIndex)
            return;

        // Prevent switching if current weapon is not ready (still in switch cooldown)
        if (CurrentWeapon != null && !CurrentWeapon.IsReady)
        {
            return;
        }

        // Check if switching during reload is allowed
        if (!allowSwitchDuringReload && CurrentWeapon != null && CurrentWeapon.IsReloading)
        {
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
            }

            // Trigger switch-out animation and wait for it
            float switchOutDelay = CurrentWeapon.TriggerSwitchOutAnimation();
            yield return new WaitForSeconds(switchOutDelay);

            var oldComponent = GetWeaponComponent(activeWeaponIndex);
            var oldWeapon = GetWeaponRuntime(activeWeaponIndex);
            if (oldWeapon != null)
            {
                UnsubscribeWeapon(oldWeapon);
                oldWeapon.Unequip();
            }
            if (oldComponent != null)
                oldComponent.gameObject.SetActive(false);
        }

        // Switch to new weapon
        activeWeaponIndex = weaponIndex;
        var newComponent = GetWeaponComponent(activeWeaponIndex);
        var newWeapon = GetWeaponRuntime(activeWeaponIndex);
        if (newComponent != null)
            newComponent.gameObject.SetActive(true);
        if (newWeapon != null)
            SubscribeWeapon(newWeapon);

        // Inject runtime context into newly equipped weapon
        var context = new WeaponContext(presentation, playerMovement);
        newWeapon?.Equip(context);

        // Start switch delay on newly activated weapon (if enabled)
        if (applySwitchDelay)
        {
            newWeapon?.OnWeaponActivated();
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
    
    public void SwitchToPreviousWeapon()
    {
        // Find previous available weapon slot
        int startIndex = activeWeaponIndex;
        int prevIndex = (activeWeaponIndex - 1 + weaponSlots.Length) % weaponSlots.Length;
        
        while (prevIndex != startIndex)
        {
            if (weaponSlots[prevIndex] != null)
            {
                SwitchToWeapon(prevIndex);
                return;
            }
            prevIndex = (prevIndex - 1 + weaponSlots.Length) % weaponSlots.Length;
        }
    }

    public void AddWeaponToSlot(MonoBehaviour weapon, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= weaponSlots.Length)
            return;

        weaponSlots[slotIndex] = weapon;
        if (weapon != null)
            weapon.gameObject.SetActive(false); // Hide by default
    }

    public IWeapon GetWeaponInSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= weaponSlots.Length)
            return null;

        return GetWeaponRuntime(slotIndex);
    }

    public bool HasWeaponInSlot(int slotIndex)
    {
        return GetWeaponInSlot(slotIndex) != null;
    }

    // Legacy method for compatibility (now sets weapon in slot 0)
    public void SetCurrentWeapon(MonoBehaviour newWpn)
    {
        AddWeaponToSlot(newWpn, 0);
        SwitchToWeapon(0);
    }

    void SubscribeWeapon(IWeapon w)
    {
        if (w is IWeaponEventSource events)
        {
            events.AmmoChanged += RelayAmmo;
            events.TierChanged += RelayTier;
        }
    }

    void UnsubscribeWeapon(IWeapon w)
    {
        if (w is IWeaponEventSource events)
        {
            events.AmmoChanged -= RelayAmmo;
            events.TierChanged -= RelayTier;
        }
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
    public IWeapon GetCurrentWeapon()
    {
        return CurrentWeapon;
    }

    MonoBehaviour GetWeaponComponent(int index)
    {
        if (index < 0 || index >= weaponSlots.Length)
            return null;
        return weaponSlots[index];
    }

    IWeapon GetWeaponRuntime(int index)
    {
        var comp = GetWeaponComponent(index);
        return comp as IWeapon;
    }
}