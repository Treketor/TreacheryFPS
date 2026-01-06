using System;
using UnityEngine;
using ReloadBehaviors = Treachery.Weapons.Behaviors.Reload;
using Treachery.Weapons.Data;
using Treachery.Weapons.Interfaces;
using Treachery.Weapons.View;

namespace Treachery.Weapons.Runtime
{
    /// <summary>
    /// ScriptableObject-driven hitscan weapon runtime.
    /// No scene searches; all dependencies injected via WeaponContext.
    /// </summary>
    public class HitscanWeaponV2 : MonoBehaviour, IUpgradeableWeapon, IWeaponBloomProvider, IWeaponEventSource
    {
        [Header("Definition")]
        [SerializeField] WeaponDefinition definition;

        [Header("State")]
        [SerializeField] WeaponTier currentTier = WeaponTier.Common;

        [Header("View (runtime)")]
        [SerializeField] WeaponView view;

        [Header("Impact FX")]
        [SerializeField] bool enableImpactEffects = true;
        [SerializeField] bool enableEnemyImpactEffects = true;
        [SerializeField] GameObject enemyImpactEffectPrefab;
        [SerializeField] float enemyImpactEffectLifetime = 2f;
        [SerializeField] bool parentToHitObject = true;

        [Header("Bloom")]
        [SerializeField] float minBloom = 0.5f;
        [SerializeField] float maxBloom = 4.0f;
        [SerializeField] float bloomDecayRate = 3.0f;
        [SerializeField] float movementBloomRate = 2.0f;
        [SerializeField] float maxBloomADS = 2.0f;
        [SerializeField] float bloomDecayRateADS = 4.0f;

        [Header("Switch")]
        [SerializeField] float weaponSwitchDelay = 0.3f;

        float _cooldown;
        float _switchCooldown;
        bool _isAiming;

        int _inMag;
        int _reserve;

        float _currentBloom;
        bool _isPlayerMoving;

        float _damage;
        float _fireRate;
        int _magSize;
        float _spread;
        float _bulletForce;

        WeaponContext? _context;

        ReloadBehaviors.IWeaponReloadBehavior _reload;
        readonly ReloadBehaviors.MagazineReloadBehavior _magReload = new();
        readonly ReloadBehaviors.SingleBulletReloadBehavior _singleReload = new();

        public event Action<int, int> AmmoChanged;
        public event Action<string, string> TierChanged;

        public string DisplayName => definition != null ? definition.displayName : name;
        public string TierName => WeaponTierSystemV2.GetTierData(currentTier).tierName;
        public WeaponTier CurrentTier => currentTier;

        public int CurrentMag => _inMag;
        public int CurrentReserve => _reserve;

        public bool IsReloading => _reload?.IsReloading ?? false;
        public bool IsReady => !IsReloading && _cooldown <= 0f && _switchCooldown <= 0f;
        public bool IsAiming => _isAiming;

        public Sprite CrosshairSprite => definition != null ? definition.crosshairSprite : null;

        bool _initialized;

        void Awake()
        {
            if (view == null)
                view = GetComponentInChildren<WeaponView>(true);

            // Intentionally do not assume definition is assigned yet.
            // WeaponControllerV2 will call Initialize(definition) after instantiation.
        }

        public void Initialize(WeaponDefinition weaponDefinition)
        {
            definition = weaponDefinition;
            InitializeIfNeeded();
        }

        void InitializeIfNeeded()
        {
            if (_initialized || definition == null)
                return;

            _reserve = Mathf.Max(0, definition.startingReserveAmmo);

            ApplyDefinitionAndTier();

            _inMag = _magSize;
            _currentBloom = minBloom;

            ConfigureReloadBehavior();

            PushAmmoToView();

            _initialized = true;
        }

        public void Equip(WeaponContext context)
        {
            InitializeIfNeeded();
            _context = context;

            // Bind presentation (ADS FOV + offsets)
            if (context.Presentation != null)
            {
                var profile = new WeaponPresentationProfile(
                    definition != null && definition.supportsADS,
                    view != null ? view.WeaponGfx : transform,
                    definition != null ? definition.adsFOV : 40f,
                    definition != null ? definition.adsTransitionSpeed : 8f,
                    definition != null ? definition.adsPosition : Vector3.zero,
                    definition != null ? definition.adsPositionSpeed : 12f);

                context.Presentation.Bind(in profile);
                context.Presentation.SetAiming(false);
            }
        }

        public void Unequip()
        {
            if (_context.HasValue && _context.Value.Presentation != null)
                _context.Value.Presentation.Unbind();

            _context = null;
        }

        public void Tick(float deltaTime)
        {
            InitializeIfNeeded();
            if (_cooldown > 0f) _cooldown -= deltaTime;
            if (_switchCooldown > 0f) _switchCooldown -= deltaTime;

            _reload?.Tick(deltaTime);

            UpdateBloom(deltaTime);
        }

        public void SetAiming(bool aiming)
        {
            InitializeIfNeeded();
            if (definition != null && !definition.supportsADS)
                aiming = false;

            // Prevent aiming while switching/reloading.
            if (aiming && (IsReloading || _switchCooldown > 0f))
                return;

            _isAiming = aiming;

            if (_context.HasValue && _context.Value.Presentation != null)
                _context.Value.Presentation.SetAiming(_isAiming);

            if (view != null)
                view.SetAiming(_isAiming);
        }

        public void TryFire()
        {
            InitializeIfNeeded();
            if (definition == null)
                return;

            // Never fire while reloading.
            // For single-bullet reloads we allow an interrupt request so the player can shoot after reload exits.
            if (IsReloading)
            {
                if (definition.reloadType == ReloadType.SingleBullet
                    && _reload is ReloadBehaviors.SingleBulletReloadBehavior s
                    && s.CanBeInterrupted()
                    && _inMag >= Mathf.Max(1, definition.bulletsPerShot))
                {
                    // Cancel reload immediately (only cancels during the interruptable phase).
                    CancelReload();
                }

                return;
            }

            // Interrupt single-bullet reload if possible
            if (IsReloading && definition.reloadType == ReloadType.SingleBullet)
            {
                if (_inMag >= Mathf.Max(1, definition.bulletsPerShot) && _reload is ReloadBehaviors.SingleBulletReloadBehavior s && s.CanBeInterrupted())
                {
                    // request finish animation; don’t shoot immediately
                    s.RequestFinishReload();
                    return;
                }

                return;
            }

            if (_cooldown > 0f || _switchCooldown > 0f)
                return;

            int bulletsPerShot = Mathf.Max(1, definition.bulletsPerShot);
            if (_inMag < bulletsPerShot)
            {
                if (!IsReloading)
                    TryReload();
                return;
            }

            _cooldown = 1f / Mathf.Max(0.01f, _fireRate);
            _inMag -= bulletsPerShot;

            AmmoChanged?.Invoke(_inMag, _reserve);
            PushAmmoToView();

            view?.PlayShoot();

            // Recoil through presentation
            float recoilMultiplier = definition.recoilMultiplier;
            if (_isAiming)
                recoilMultiplier *= definition.adsRecoilMultiplier;

            if (_context.HasValue && _context.Value.Presentation != null)
                _context.Value.Presentation.ApplyRecoil(recoilMultiplier);

            float bloomSpread = _currentBloom;
            AddShootingBloom();

            if (definition.usePelletSystem)
                FirePellets(bloomSpread);
            else
                FireSingle(bloomSpread);
        }

        public void TryReload()
        {
            InitializeIfNeeded();
            if (definition == null || _reload == null)
                return;

            if (_switchCooldown > 0f)
                return;

            if (!_reload.CanReload(_inMag, _magSize, _reserve))
                return;

            // Cancel ADS when starting reload
            if (_isAiming)
                SetAiming(false);

            _reload.StartReload(_inMag, _magSize, _reserve, OnAmmoAdded, OnReloadComplete, OnReloadCancelled);
        }

        public void CancelReload() => _reload?.CancelReload();

        public void OnWeaponActivated() => _switchCooldown = weaponSwitchDelay;

        public float TriggerSwitchOutAnimation() => view != null ? view.PlaySwitchOut() : 0.5f;

        public bool CanUpgrade() => WeaponTierSystemV2.CanUpgrade(currentTier);

        public bool TryUpgradeTier()
        {
            if (!WeaponTierSystemV2.CanUpgrade(currentTier))
                return false;

            var next = WeaponTierSystemV2.GetNextTier(currentTier);
            if (!next.HasValue)
                return false;

            currentTier = next.Value;
            ApplyDefinitionAndTier();

            _inMag = _magSize;
            AmmoChanged?.Invoke(_inMag, _reserve);
            TierChanged?.Invoke(DisplayName, TierName);
            PushAmmoToView();

            return true;
        }

        public int GetUpgradeCost() => WeaponTierSystemV2.GetUpgradeCost(currentTier);

        public float GetBloomPercentage()
        {
            float max = _isAiming ? maxBloomADS : maxBloom;
            return Mathf.InverseLerp(minBloom, max, _currentBloom);
        }

        void ApplyDefinitionAndTier()
        {
            if (definition == null)
                return;

            WeaponTierData tierData = WeaponTierSystemV2.GetTierData(currentTier);

            _damage = definition.baseDamage * tierData.damageMultiplier;
            _fireRate = definition.baseFireRate;
            _magSize = Mathf.RoundToInt(definition.baseMagSize * tierData.magSizeMultiplier);
            _spread = definition.baseSpread * tierData.spreadMultiplier;
            _bulletForce = definition.baseBulletForce * tierData.damageMultiplier;

            // Keep reload duration data-driven
            _magReload.SetReloadDuration(definition.baseReloadTime);
            _singleReload.ConfigureTimings(definition.startReloadDuration, definition.singleBulletReloadDuration, definition.finishReloadDuration);
        }

        void ConfigureReloadBehavior()
        {
            // Bind reload animators if present
            Animator animator = view != null ? view.GetComponentInChildren<Animator>(true) : null;

            _magReload.Initialize(this, animator);
            _singleReload.Initialize(this, animator);

            if (definition != null && definition.reloadType == ReloadType.SingleBullet)
                _reload = _singleReload;
            else
                _reload = _magReload;
        }

        void OnAmmoAdded(int amountAdded)
        {
            int actual = Mathf.Min(amountAdded, _reserve);
            actual = Mathf.Min(actual, _magSize - _inMag);

            _inMag += actual;
            _reserve -= actual;

            AmmoChanged?.Invoke(_inMag, _reserve);
            PushAmmoToView();
        }

        void OnReloadComplete() { }
        void OnReloadCancelled() { }

        void PushAmmoToView()
        {
            view?.SetAmmoInMag(_inMag);
        }

        void UpdateBloom(float deltaTime)
        {
            bool wasMoving = _isPlayerMoving;
            _isPlayerMoving = IsPlayerMoving();

            float currentMax = _isAiming ? maxBloomADS : maxBloom;
            float currentDecay = _isAiming ? bloomDecayRateADS : bloomDecayRate;

            if (_isPlayerMoving)
                _currentBloom = Mathf.MoveTowards(_currentBloom, currentMax, movementBloomRate * deltaTime);
            else
                _currentBloom = Mathf.MoveTowards(_currentBloom, minBloom, currentDecay * deltaTime);

            _currentBloom = Mathf.Clamp(_currentBloom, minBloom, currentMax);
        }

        bool IsPlayerMoving()
        {
            if (!_context.HasValue || _context.Value.PlayerMovement == null)
                return false;

            var controller = _context.Value.PlayerMovement.GetComponent<CharacterController>();
            if (controller == null)
                return false;

            Vector3 hv = new(controller.velocity.x, 0, controller.velocity.z);
            return hv.magnitude > 0.1f;
        }

        void AddShootingBloom()
        {
            _currentBloom = _isAiming ? maxBloomADS : maxBloom;
        }

        void FireSingle(float spread)
        {
            if (!TryRaycast(spread, out var hit))
                return;

            DispatchHit(hit, _damage, _bulletForce);
            SpawnImpactEffect(hit);
        }

        void FirePellets(float baseSpread)
        {
            float pelletSpread = baseSpread * definition.pelletSpreadMultiplier;
            float pelletDamage = (_damage / Mathf.Max(1, definition.pelletsPerShot)) * definition.pelletDamageMultiplier;

            for (int i = 0; i < Mathf.Max(1, definition.pelletsPerShot); i++)
            {
                if (!TryRaycast(pelletSpread, out var hit))
                    continue;

                DispatchHit(hit, pelletDamage, _bulletForce);
                SpawnImpactEffect(hit);
            }
        }

        bool TryRaycast(float spreadDeg, out RaycastHit hit)
        {
            hit = default;
            if (definition == null)
                return false;

            Transform origin = transform;
            if (_context.HasValue && _context.Value.ShootOrigin != null)
                origin = _context.Value.ShootOrigin;

            Vector3 dir = origin.forward;
            if (spreadDeg > 0f)
            {
                var r = UnityEngine.Random.insideUnitCircle * Mathf.Tan(spreadDeg * Mathf.Deg2Rad);
                dir = (origin.forward + origin.right * r.x + origin.up * r.y).normalized;
            }

            var effectiveMask = definition.hitMask & ~definition.ignoreLayerMask;
            return Physics.Raycast(origin.position, dir, out hit, definition.maxRange, effectiveMask, QueryTriggerInteraction.Collide);
        }

        void DispatchHit(RaycastHit hit, float damageAmount, float bulletForce, Vector3 legacyNormal = default)
        {
            if (hit.collider == null)
                return;

            IHitReceiver receiver = hit.collider.GetComponent<IHitReceiver>() ?? hit.collider.GetComponentInParent<IHitReceiver>();
            if (receiver != null)
            {
                var payload = new HitPayload(
                    damage: damageAmount,
                    bulletForce: bulletForce,
                    point: hit.point,
                    normal: legacyNormal,
                    hitCollider: hit.collider,
                    source: gameObject);

                receiver.ReceiveHit(in payload);
                return;
            }

            // Temporary fallback until everything implements IHitReceiver
            IDamageable damageable = hit.collider.GetComponent<IDamageable>() ?? hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damageAmount, hit.point, legacyNormal);
            }
        }

        void SpawnImpactEffect(RaycastHit hit)
        {
            if (!enableImpactEffects)
                return;

            bool hitEnemy = false;
            if (hit.collider != null)
            {
                var receiver = hit.collider.GetComponent<IHitReceiver>() ?? hit.collider.GetComponentInParent<IHitReceiver>();
                if (receiver != null)
                    hitEnemy = receiver.CountsAsEnemyHit;
                else
                    hitEnemy = hit.collider.GetComponent<IDamageable>() != null || hit.collider.GetComponentInParent<IDamageable>() != null;
            }

            if (hitEnemy && enableEnemyImpactEffects && enemyImpactEffectPrefab != null)
            {
                GameObject fx = Instantiate(enemyImpactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                if (parentToHitObject && hit.collider != null)
                    fx.transform.SetParent(hit.collider.transform);
                Destroy(fx, enemyImpactEffectLifetime);
                return;
            }

            if (BulletImpactManager.Instance != null)
                BulletImpactManager.Instance.SpawnImpactEffect(hit.point, hit.normal, hit.collider);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (definition != null)
                ApplyDefinitionAndTier();
        }
#endif
    }
}
