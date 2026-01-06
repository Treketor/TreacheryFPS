using UnityEngine;
using UnityEngine.InputSystem;
using Treachery.Weapons.Data;
using Treachery.Weapons.Interfaces;
using Treachery.Weapons.View;

namespace Treachery.Weapons.Runtime
{
    /// <summary>
    /// V2 weapon controller: spawns weapons from WeaponDefinition assets and drives them via IWeapon.
    /// </summary>
    public class WeaponControllerV2 : MonoBehaviour, IWeaponController
    {
        [Header("Loadout")]
        [SerializeField] WeaponLoadout loadout;
        [SerializeField] Transform weaponAnchor;
        [SerializeField] int activeWeaponIndex;

        [Header("Runtime Dependencies")]
        [SerializeField] WeaponPresentationController presentation;
        [SerializeField] PlayerMovement playerMovement;
        [Tooltip("Where hitscan rays originate (usually the player camera).")]
        [SerializeField] Transform shootOrigin;
        [SerializeField] bool autoFindRuntimeDependencies = true;

        [Header("Input")]
        [SerializeField] InputActionAsset playerInput;

        [Header("Weapon Switching")]
        [SerializeField] bool allowSwitchDuringReload = true;

        public System.Action<int, int> OnAmmoChanged;
        public System.Action<string, string> OnWeaponChanged;
        public System.Action<int, int> OnWeaponSlotChanged;

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

        public int ActiveWeaponIndex => activeWeaponIndex;
        public int WeaponSlotCount => loadout != null ? loadout.SlotCount : 0;

        public IWeapon CurrentWeapon => GetWeaponRuntime(activeWeaponIndex);
        public int CurrentAmmoInMag => CurrentWeapon != null ? CurrentWeapon.CurrentMag : 0;
        public int CurrentReserveAmmo => CurrentWeapon != null ? CurrentWeapon.CurrentReserve : -1;
        public string CurrentWeaponDisplayName => CurrentWeapon != null ? CurrentWeapon.DisplayName : "—";
        public string CurrentTierName => CurrentWeapon != null ? CurrentWeapon.TierName : "—";

        readonly GameObject[] _spawned = new GameObject[8];
        readonly IWeapon[] _weapons = new IWeapon[8];

        bool _isSwitching;

        InputAction _attackAction;
        InputAction _reloadAction;
        InputAction _switchWeaponAction;
        InputAction _aimAction;
        InputAction _scrollWheelAction;
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

                if (shootOrigin == null)
                {
                    var cam = Camera.main;
                    if (cam != null)
                        shootOrigin = cam.transform;
                }

                if (weaponAnchor == null)
                    weaponAnchor = transform;
            }

            if (playerInput != null)
            {
                _attackAction = playerInput.FindAction("Attack");
                _reloadAction = playerInput.FindAction("Reload");
                _switchWeaponAction = playerInput.FindAction("SwitchWeapon");
                _aimAction = playerInput.FindAction("Aim");
                _scrollWheelAction = playerInput.FindAction("ScrollWheel");
                _weapon1Action = playerInput.FindAction("Weapon1");
                _weapon2Action = playerInput.FindAction("Weapon2");
                _weapon3Action = playerInput.FindAction("Weapon3");
                _weapon4Action = playerInput.FindAction("Weapon4");
            }

            SpawnLoadout();
            InitializeWeapons();
        }

        void OnEnable()
        {
            playerInput?.Enable();
            _attackAction?.Enable();
            _reloadAction?.Enable();
            _aimAction?.Enable();
        }

        void OnDisable()
        {
            _attackAction?.Disable();
            _reloadAction?.Disable();
            _aimAction?.Disable();
            playerInput?.Disable();
        }

        void Update()
        {
            var weapon = CurrentWeapon;
            if (weapon == null) return;

            if (PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused)
                return;

            if (_isSwitching) return;

            if (_attackAction != null && _attackAction.IsPressed())
                weapon.TryFire();

            if (_reloadAction != null && _reloadAction.WasPressedThisFrame())
                weapon.TryReload();

            bool isAiming = _aimAction != null && _aimAction.IsPressed();
            bool switchPressed = _switchWeaponAction != null && _switchWeaponAction.WasPressedThisFrame();

            float scrollValue = 0f;
            if (_scrollWheelAction != null)
            {
                Vector2 scrollInput = _scrollWheelAction.ReadValue<Vector2>();
                scrollValue = scrollInput.y;
            }

            int directWeaponIndex = -1;
            if (_weapon1Action != null && _weapon1Action.WasPressedThisFrame()) directWeaponIndex = 0;
            else if (_weapon2Action != null && _weapon2Action.WasPressedThisFrame()) directWeaponIndex = 1;
            else if (_weapon3Action != null && _weapon3Action.WasPressedThisFrame()) directWeaponIndex = 2;
            else if (_weapon4Action != null && _weapon4Action.WasPressedThisFrame()) directWeaponIndex = 3;

            if (directWeaponIndex >= 0)
            {
                isAiming = false;
                SwitchToWeapon(directWeaponIndex);
            }
            else if (switchPressed)
            {
                isAiming = false;
                SwitchToNextWeapon();
            }
            else if (Mathf.Abs(scrollValue) > 0.1f)
            {
                isAiming = false;
                if (scrollValue > 0) SwitchToPreviousWeapon();
                else SwitchToNextWeapon();
            }

            weapon.SetAiming(isAiming);
            weapon.Tick(Time.deltaTime);
        }

        void SpawnLoadout()
        {
            int slotCount = WeaponSlotCount;
            for (int i = 0; i < _spawned.Length; i++)
            {
                _spawned[i] = null;
                _weapons[i] = null;
            }

            for (int i = 0; i < slotCount && i < _spawned.Length; i++)
            {
                WeaponDefinition def = loadout.Get(i);
                if (def == null || def.viewPrefab == null)
                    continue;

                GameObject instance = Instantiate(def.viewPrefab, weaponAnchor);
                instance.name = def.displayName;

                // Ensure there is an IWeapon on the spawned object.
                var weapon = instance.GetComponent<IWeapon>();
                if (weapon == null)
                {
                    // Most prefabs should have HitscanWeaponV2 (or other IWeapon) already.
                    // If not, try adding the default hitscan runtime.
                    var hitscan = instance.AddComponent<HitscanWeaponV2>();
                    hitscan.Initialize(def);
                    weapon = hitscan;
                }
                else if (weapon is HitscanWeaponV2 hw)
                {
                    hw.Initialize(def);
                }

                _spawned[i] = instance;
                _weapons[i] = weapon;

                // Start disabled; InitializeWeapons() will enable the first one.
                instance.SetActive(false);

                SubscribeWeapon(weapon);
            }
        }

        void InitializeWeapons()
        {
            // Hide all
            for (int i = 0; i < WeaponSlotCount; i++)
            {
                var go = _spawned[i];
                if (go != null) go.SetActive(false);
            }

            // Find first available
            activeWeaponIndex = -1;
            for (int i = 0; i < WeaponSlotCount; i++)
            {
                if (_weapons[i] != null)
                {
                    SwitchToWeapon(i, true);
                    break;
                }
            }
        }

        public void SwitchToWeapon(int weaponIndex, bool applySwitchDelay = true)
        {
            if (weaponIndex < 0 || weaponIndex >= WeaponSlotCount)
                return;

            if (_isSwitching || weaponIndex == activeWeaponIndex)
                return;

            if (_weapons[weaponIndex] == null)
                return;

            if (CurrentWeapon != null && !CurrentWeapon.IsReady)
                return;

            if (!allowSwitchDuringReload && CurrentWeapon != null && CurrentWeapon.IsReloading)
                return;

            StartCoroutine(SwitchWeaponCoroutine(weaponIndex, applySwitchDelay));
        }

        System.Collections.IEnumerator SwitchWeaponCoroutine(int weaponIndex, bool applySwitchDelay)
        {
            _isSwitching = true;

            if (CurrentWeapon != null)
            {
                if (CurrentWeapon.IsReloading && allowSwitchDuringReload)
                    CurrentWeapon.CancelReload();

                float outDelay = CurrentWeapon.TriggerSwitchOutAnimation();
                yield return new WaitForSeconds(outDelay);

                CurrentWeapon.Unequip();

                var oldGo = _spawned[activeWeaponIndex];
                if (oldGo != null) oldGo.SetActive(false);
            }

            activeWeaponIndex = weaponIndex;

            var newGo = _spawned[activeWeaponIndex];
            if (newGo != null) newGo.SetActive(true);

            var newWeapon = CurrentWeapon;
            if (newWeapon != null)
            {
                var context = new WeaponContext(presentation, playerMovement, shootOrigin);
                newWeapon.Equip(context);
                if (applySwitchDelay)
                    newWeapon.OnWeaponActivated();
            }

            OnWeaponChanged?.Invoke(CurrentWeaponDisplayName, CurrentTierName);
            OnAmmoChanged?.Invoke(CurrentAmmoInMag, CurrentReserveAmmo);
            OnWeaponSlotChanged?.Invoke(activeWeaponIndex, WeaponSlotCount);

            if (CrosshairManager.Instance != null)
                CrosshairManager.Instance.UpdateCrosshair();

            _isSwitching = false;
        }

        public void SwitchToNextWeapon()
        {
            if (WeaponSlotCount <= 0) return;

            int startIndex = activeWeaponIndex;
            int nextIndex = (activeWeaponIndex + 1 + WeaponSlotCount) % WeaponSlotCount;

            while (nextIndex != startIndex)
            {
                if (_weapons[nextIndex] != null)
                {
                    SwitchToWeapon(nextIndex);
                    return;
                }
                nextIndex = (nextIndex + 1) % WeaponSlotCount;
            }
        }

        public void SwitchToPreviousWeapon()
        {
            if (WeaponSlotCount <= 0) return;

            int startIndex = activeWeaponIndex;
            int prevIndex = (activeWeaponIndex - 1 + WeaponSlotCount) % WeaponSlotCount;

            while (prevIndex != startIndex)
            {
                if (_weapons[prevIndex] != null)
                {
                    SwitchToWeapon(prevIndex);
                    return;
                }
                prevIndex = (prevIndex - 1 + WeaponSlotCount) % WeaponSlotCount;
            }
        }

        public IWeapon GetWeaponInSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= WeaponSlotCount)
                return null;
            return _weapons[slotIndex];
        }

        void SubscribeWeapon(IWeapon weapon)
        {
            if (weapon is IWeaponEventSource events)
            {
                events.AmmoChanged += RelayAmmo;
                events.TierChanged += RelayTier;
            }
        }

        void RelayAmmo(int inMag, int reserve) => OnAmmoChanged?.Invoke(inMag, reserve);

        void RelayTier(string displayName, string tierName)
        {
            OnWeaponChanged?.Invoke(displayName, tierName);
            OnAmmoChanged?.Invoke(CurrentAmmoInMag, CurrentReserveAmmo);
        }

        IWeapon GetWeaponRuntime(int slot) => slot >= 0 && slot < _weapons.Length ? _weapons[slot] : null;
    }
}
