using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum ReloadType
{
    Magazine,
    SingleBullet
}

public class WeaponInstance_Hitscan : MonoBehaviour
{
    [Header("Base Stats")]
    public string displayName = "Pistol";
    
    [Header("Base Stats (These get multiplied by tier)")]
    [Tooltip("Base damage per shot")]
    public float baseDamage = 20f;
    [Tooltip("Base fire rate (shots per second)")]
    public float baseFireRate = 5f;
    [Tooltip("Base magazine size")]
    public int baseMagSize = 12;
    [Tooltip("Base reload time in seconds")]
    public float baseReloadTime = 1.2f;
    [Tooltip("Base spread/accuracy - this becomes the minimum bloom")]
    public float baseSpread = 1.5f;
    
    [Header("Bloom System")]
    [Tooltip("Tightest and most accurate bloom value")]
    [SerializeField] float minBloom = 0.5f;
    [Tooltip("Widest and least accurate bloom value")]
    [SerializeField] float maxBloom = 4.0f;
    [Tooltip("How quickly bloom decays to minimum (normal)")]
    [SerializeField] float bloomDecayRate = 3.0f;
    [Tooltip("How quickly bloom increases when moving")]
    [SerializeField] float movementBloomRate = 2.0f;
    [Tooltip("Maximum bloom value when aiming down sights")]
    [SerializeField] float maxBloomADS = 2.0f;
    [Tooltip("How quickly bloom decays when aiming down sights")]
    [SerializeField] float bloomDecayRateADS = 4.0f;
    
    [Header("Current Tier")]
    [SerializeField] WeaponTier currentTier = WeaponTier.Common;
    
    // Current stats (calculated from base * tier multipliers)
    float damage;
    float fireRate;
    int magSize;
    float reloadTime;
    float spread;

    [Header("Ammo Pools")]
    [Tooltip("Initial reserve ammo when weapon is created")]
    public int startingReserve = 60;

    [Header("References")]
    public WeaponRaycaster raycaster;
    
    [Header("Recoil")]
    [SerializeField] WeaponRecoil weaponRecoil;
    [Tooltip("Multiplier for recoil intensity")]
    [SerializeField] float recoilMultiplier = 1f;
    [Tooltip("Auto-find WeaponRecoil component if not assigned")]
    [SerializeField] bool autoFindRecoil = true;

    [Header("Muzzle Flash")]
    [SerializeField] GameObject muzzleFlashObject;
    [Tooltip("Duration of muzzle flash effect in seconds")]
    [SerializeField] float muzzleFlashDuration = 0.05f;

    [Header("Bullet Impact Effects")]
    [SerializeField] bool enableImpactEffects = true;
    [Tooltip("Whether to spawn impact effects when bullets hit surfaces")]
    
    [Header("Pellet System (Shotgun)")]
    [SerializeField] bool usePelletSystem = false;
    [Tooltip("Enable to fire multiple pellets per shot (shotgun behavior)")]
    [SerializeField] int pelletsPerShot = 8;
    [Tooltip("Number of pellets fired per trigger pull")]
    [SerializeField] float pelletSpreadMultiplier = 3.0f;
    [Tooltip("Multiplier for pellet spread relative to base weapon spread")]
    [SerializeField] float pelletDamageMultiplier = 1.0f;
    [Tooltip("Damage multiplier for distributed pellet damage (1.0 = base damage distributed evenly)")]

    [Header("Damage Falloff")]
    [SerializeField] bool enableDamageFalloff = false;
    [Tooltip("Enable distance-based damage falloff")]
    [SerializeField] AnimationCurve damageFalloffCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.5f);
    [Tooltip("Damage multiplier curve based on distance (X: normalized distance 0-1, Y: damage multiplier)")]
    [SerializeField] float maxDamageRange = 50f;
    [Tooltip("Maximum effective range for damage calculations (units)")]

    [Header("Animation")]
    [SerializeField] Animator animator;
    [SerializeField] string shootTriggerName = "Shoot";
    [SerializeField] string reloadTriggerName = "Reload";
    [SerializeField] string switchOutTriggerName = "Switch Out";
    [SerializeField] string currentAmmoParameterName = "Current Ammo";
    [SerializeField] bool autoFindAnimator = true;
    
    [Header("Reload System")]
    [Tooltip("Choose between Magazine (traditional) or SingleBullet (pump shotgun style) reload")]
    [SerializeField] ReloadType reloadType = ReloadType.Magazine;
    
    [Header("Magazine Reload Settings")]
    [Tooltip("Base duration of reload animation at normal speed")]
    [SerializeField] float baseReloadAnimationDuration = 2.0f;
    [Tooltip("Extra time after animation completes before weapon is ready")]
    [SerializeField] float reloadDelayBuffer = 0.5f;
    
    [Header("Single Bullet Reload Settings")]
    [Tooltip("Settings for single bullet reload behavior (pump shotgun style)")]
    [SerializeField] SingleBulletReloadBehavior singleBulletReload = new SingleBulletReloadBehavior();
    
    [Header("Weapon Switch Timing")]
    [Tooltip("Time after switching to this weapon before it can be used")]
    [SerializeField] float weaponSwitchDelay = 0.3f;
    [Tooltip("Time to wait after triggering Switch Out animation before hiding weapon")]
    [SerializeField] float switchOutAnimationDelay = 0.5f;
    
    [Header("Aim Down Sights")]
    [Tooltip("Whether this weapon supports aiming down sights")]
    [SerializeField] bool supportsADS = true;
    [Tooltip("Transform for weapon graphics (auto-found if not assigned)")]
    [SerializeField] Transform weaponGFX;
    [Tooltip("Auto-find weapon GFX object by common names")]
    [SerializeField] bool autoFindGFX = true;
    [Tooltip("Field of view when aiming down sights")]
    [SerializeField] float adsFOV = 40f;
    [Tooltip("Speed of camera FOV transition to ADS")]
    [SerializeField] float adsTransitionSpeed = 8f;
    [Tooltip("Local position offset for weapon when aiming")]
    [SerializeField] Vector3 adsPosition = new Vector3(0f, 0f, 0.2f);
    [Tooltip("Speed of weapon position transition to ADS")]
    [SerializeField] float adsPositionSpeed = 12f;
    [Tooltip("Spread multiplier when aiming (lower = more accurate)")]
    [SerializeField] float adsSpreadMultiplier = 0.3f;
    [Tooltip("Recoil multiplier when aiming (lower = less recoil)")]
    [SerializeField] float adsRecoilMultiplier = 0.5f;
    [Tooltip("Animator bool parameter name for ADS state")]
    [SerializeField] string adsAnimationBool = "IsAiming";
    
    [Header("Crosshair")]
    [Tooltip("Custom crosshair for this weapon")]
    [SerializeField] Sprite crosshairSprite;

    float _cooldown;
    float _switchCooldown;
    int _inMag;
    
    // Bloom system variables
    float _currentBloom;
    bool _isPlayerMoving;
    PlayerMovement _playerMovement;
    int _reserve;
    
    // Reload behavior system
    IWeaponReloadBehavior _reloadBehavior;
    MagazineReloadBehavior _magazineReloadBehavior = new MagazineReloadBehavior();
    
    // ADS variables
    bool _isAiming = false;
    bool _wasAiming = false;
    Vector3 _originalGFXPosition;
    float _originalFOV;
    Camera _mainCamera;

    public System.Action<int, int> OnAmmoChanged;
    public System.Action<string, string> OnTierChanged;
    public System.Action<WeaponTier> OnTierUpgraded;

    public string DisplayName => displayName;
    public string TierName => WeaponTierSystem.GetTierData(currentTier).tierName;
    public WeaponTier CurrentTier => currentTier;
    public int CurrentMag => _inMag;
    public int CurrentReserve => _reserve;
    public float CurrentDamage => damage;
    public float CurrentFireRate => fireRate;
    public bool IsReloading => _reloadBehavior?.IsReloading ?? false;
    public bool IsSwitching => _switchCooldown > 0f;
    public bool IsReady => !IsReloading && _cooldown <= 0f && _switchCooldown <= 0f;
    public bool IsAiming => _isAiming;
    public Sprite CrosshairSprite => crosshairSprite;
    public bool UsesPelletSystem => usePelletSystem;
    public int PelletsPerShot => pelletsPerShot;
    public float PelletDamagePerPellet => (damage / pelletsPerShot) * pelletDamageMultiplier;

    void Awake()
    {
        // Calculate initial stats based on tier
        RecalculateStats();
        _inMag = magSize;
        _reserve = Mathf.Max(0, startingReserve);

        // Auto-find animator if not assigned
        if (!animator && autoFindAnimator)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator)
                Debug.Log($"WeaponInstance_Hitscan ({displayName}): Auto-found Animator in children");
        }

        // Auto-find recoil component if not assigned
        if (!weaponRecoil && autoFindRecoil)
        {
            weaponRecoil = FindFirstObjectByType<WeaponRecoil>();
            if (weaponRecoil)
                Debug.Log($"WeaponInstance_Hitscan ({displayName}): Auto-found WeaponRecoil component");
        }
        
        // Initialize crosshair system
        InitializeCrosshair();
        
        // Initialize ADS system
        InitializeADS();
        
        // Initialize bloom system
        InitializeBloomSystem();
        
        // Initialize reload behavior system
        InitializeReloadBehavior();

        // Make sure muzzle flash starts disabled
        if (muzzleFlashObject != null)
        {
            muzzleFlashObject.SetActive(false);
        }

        // Validate raycaster reference
        if (raycaster == null)
        {
            Debug.LogError($"WeaponInstance_Hitscan ({displayName}): Raycaster is not assigned! Weapon will not be able to shoot.", this);
        }

        // Initialize animator parameter
        UpdateAnimatorAmmo();
    }

    void Update()
    {
        if (_cooldown > 0f) _cooldown -= Time.deltaTime;
        if (_switchCooldown > 0f) _switchCooldown -= Time.deltaTime;
        
        // Update reload behavior
        _reloadBehavior?.Update();
        
        // Update bloom system
        UpdateBloomSystem();
        
        // Update ADS
        UpdateADS();
    }

    /// <summary>
    /// Updates the animator's "Current Ammo" parameter to track magazine state
    /// </summary>
    void UpdateAnimatorAmmo()
    {
        if (animator != null && !string.IsNullOrEmpty(currentAmmoParameterName))
        {
            animator.SetInteger(currentAmmoParameterName, _inMag);
        }
    }

    public void TryFire()
    {
        // Allow interrupting single bullet reload to shoot with partial ammo
        if (IsReloading && !TryInterruptReloadForShooting()) return;
        if (_cooldown > 0f || _switchCooldown > 0f) return;

        // Auto-reload if magazine is empty
        if (_inMag <= 0)
        {
            // Only try to reload if not already reloading to prevent spam
            if (!IsReloading)
            {
                TryReload();
                // Add a small cooldown to prevent immediate re-firing before reload adds bullets
                _cooldown = 0.1f; // Small delay to let reload system take control
            }
            return;
        }

        _cooldown = 1f / fireRate;
        _inMag--;
        OnAmmoChanged?.Invoke(_inMag, _reserve);
        UpdateAnimatorAmmo(); // Update animator parameter after ammo changes

        // Trigger shoot animation at normal speed (no fire rate scaling)
        if (animator != null)
        {
            // Trigger shoot animation
            if (!string.IsNullOrEmpty(shootTriggerName))
            {
                animator.SetTrigger(shootTriggerName);
            }
        }

            // Apply recoil with ADS multiplier if aiming
            if (weaponRecoil != null)
            {
                float finalRecoilMultiplier = recoilMultiplier;
                if (_isAiming)
                {
                    finalRecoilMultiplier *= adsRecoilMultiplier;
                }
                weaponRecoil.ApplyRecoil(finalRecoilMultiplier);
            }        // Trigger muzzle flash
        if (muzzleFlashObject != null)
        {
            StartCoroutine(ShowMuzzleFlash());
        }

        // Register shot fired for accuracy tracking
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.RegisterShotFired();
        }

        // Check if raycaster is assigned
        if (raycaster == null)
        {
            Debug.LogError($"WeaponInstance_Hitscan ({displayName}): Cannot shoot - raycaster is null!", this);
            return;
        }

        // Calculate current spread using bloom system
        float bloomSpread = CalculateCurrentSpread();
        
        // Apply shooting bloom increase
        AddShootingBloom();
        
        // Use pellet system if enabled (shotgun behavior)
        if (usePelletSystem)
        {
            FirePellets(bloomSpread);
        }
        else
        {
            FireSingleBullet(bloomSpread);
        }
    }

    /// <summary>
    /// Fire multiple pellets for shotgun behavior
    /// </summary>
    private void FirePellets(float baseSpread)
    {
        if (raycaster == null)
        {
            Debug.LogError($"WeaponInstance_Hitscan ({displayName}): Cannot shoot pellets - raycaster is null!", this);
            return;
        }

        float pelletSpread = baseSpread * pelletSpreadMultiplier;
        float pelletDamage = (damage / pelletsPerShot) * pelletDamageMultiplier; // Distribute base damage across all pellets
        
        for (int i = 0; i < pelletsPerShot; i++)
        {
            if (raycaster.TryShoot(out var hit, pelletSpread))
            {
                ProcessPelletHit(hit, pelletDamage);
            }
        }
    }

    /// <summary>
    /// Fire a single bullet for traditional weapons
    /// </summary>
    private void FireSingleBullet(float spread)
    {
        if (raycaster == null)
        {
            Debug.LogError($"WeaponInstance_Hitscan ({displayName}): Cannot shoot - raycaster is null!", this);
            return;
        }

        if (raycaster.TryShoot(out var hit, spread))
        {
            ProcessBulletHit(hit, damage);
        }
    }

    /// <summary>
    /// Calculate damage with distance-based falloff
    /// </summary>
    private float CalculateDamageWithFalloff(float baseDamage, float distance)
    {
        if (!enableDamageFalloff || maxDamageRange <= 0f)
            return baseDamage;

        // Normalize distance to 0-1 range based on max damage range
        float normalizedDistance = Mathf.Clamp01(distance / maxDamageRange);
        
        // Evaluate the curve to get damage multiplier
        float damageMultiplier = damageFalloffCurve.Evaluate(normalizedDistance);
        
        return baseDamage * damageMultiplier;
    }

    /// <summary>
    /// Process hit for a single pellet
    /// </summary>
    private void ProcessPelletHit(RaycastHit hit, float pelletDamage)
    {
        // Apply distance-based damage falloff
        float distance = Vector3.Distance(transform.position, hit.point);
        float falloffDamage = CalculateDamageWithFalloff(pelletDamage, distance);
        
        bool isHeadshot = false;
        float finalDamage = falloffDamage;

        // Check for headshot zone first
        if (hit.collider.TryGetComponent<HeadshotZone>(out var headshotZone))
        {
            isHeadshot = headshotZone.ProcessHeadshot(falloffDamage, hit.point, hit.normal, out finalDamage);
            
            // Register headshot for accuracy tracking (only once per shot, not per pellet)
            if (isHeadshot && ScoreManager.Instance != null)
            {
                ScoreManager.Instance.RegisterHeadshot();
            }
        }
        // Otherwise check for regular IDamageable on the hit object or its parent
        else 
        {
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable == null)
                damageable = hit.collider.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(pelletDamage, hit.point, hit.normal);
            }
        }

        // Spawn impact effect at hit point
        if (enableImpactEffects && BulletImpactManager.Instance != null)
        {
            BulletImpactManager.Instance.SpawnImpactEffect(hit.point, hit.normal, hit.collider);
        }
    }

    /// <summary>
    /// Process hit for a single bullet
    /// </summary>
    private void ProcessBulletHit(RaycastHit hit, float bulletDamage)
    {
        // Apply distance-based damage falloff
        float distance = Vector3.Distance(transform.position, hit.point);
        float falloffDamage = CalculateDamageWithFalloff(bulletDamage, distance);
        
        bool isHeadshot = false;
        float finalDamage = falloffDamage;

        // Check for headshot zone first
        if (hit.collider.TryGetComponent<HeadshotZone>(out var headshotZone))
        {
            isHeadshot = headshotZone.ProcessHeadshot(falloffDamage, hit.point, hit.normal, out finalDamage);
            
            // Register headshot for accuracy tracking
            if (isHeadshot && ScoreManager.Instance != null)
            {
                ScoreManager.Instance.RegisterHeadshot();
            }
        }
        // Otherwise check for regular IDamageable on the hit object or its parent
        else 
        {
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable == null)
                damageable = hit.collider.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(bulletDamage, hit.point, hit.normal);
            }
        }

        // Spawn impact effect at hit point
        if (enableImpactEffects && BulletImpactManager.Instance != null)
        {
            BulletImpactManager.Instance.SpawnImpactEffect(hit.point, hit.normal, hit.collider);
        }
    }

    IEnumerator ShowMuzzleFlash()
    {
        // Activate muzzle flash
        muzzleFlashObject.SetActive(true);
        
        // Wait for brief duration
        yield return new WaitForSeconds(muzzleFlashDuration);
        
        // Deactivate muzzle flash
        muzzleFlashObject.SetActive(false);
    }



    public void TryReload()
    {
        if (_reloadBehavior == null || _switchCooldown > 0f) return;
        if (!_reloadBehavior.CanReload(_inMag, magSize, _reserve)) return;

        // Cancel ADS when starting reload
        if (_isAiming)
        {
            SetAiming(false);
        }

        _reloadBehavior.StartReload(_inMag, magSize, _reserve, OnAmmoAdded, OnReloadComplete, OnReloadCancelled);
    }

    /// <summary>
    /// Callback when ammo is added during reload
    /// </summary>
    private void OnAmmoAdded(int amountAdded)
    {
        // Calculate actual ammo to add from reserve
        int actualAmountAdded = Mathf.Min(amountAdded, _reserve);
        actualAmountAdded = Mathf.Min(actualAmountAdded, magSize - _inMag);
        
        _inMag += actualAmountAdded;
        _reserve -= actualAmountAdded;
        
        OnAmmoChanged?.Invoke(_inMag, _reserve);
        UpdateAnimatorAmmo();
        
        Debug.Log($"{displayName}: Added {actualAmountAdded} ammo, now {_inMag}/{magSize}");
    }

    /// <summary>
    /// Callback when reload is completed
    /// </summary>
    private void OnReloadComplete()
    {
        Debug.Log($"{displayName}: Reload completed");
    }

    /// <summary>
    /// Callback when reload is cancelled
    /// </summary>
    private void OnReloadCancelled()
    {
        Debug.Log($"{displayName}: Reload cancelled");
    }

    /// <summary>
    /// Cancel/interrupt the current reload. Used when switching weapons during reload.
    /// </summary>
    public void CancelReload()
    {
        _reloadBehavior?.CancelReload();
    }

    /// <summary>
    /// Try to interrupt single bullet reload to allow shooting with partial ammo.
    /// Only works if using single bullet reload and it can be safely interrupted.
    /// </summary>
    public bool TryInterruptReloadForShooting()
    {
        if (!IsReloading || reloadType != ReloadType.SingleBullet) 
            return false;

        // Don't interrupt reload if magazine is empty - need at least 1 bullet to shoot
        if (_inMag <= 0)
            return false;

        // Check if the single bullet reload can be safely interrupted
        var singleBulletBehavior = _reloadBehavior as SingleBulletReloadBehavior;
        if (singleBulletBehavior != null && singleBulletBehavior.CanBeInterrupted())
        {
            // Instead of cancelling, request finish reload (cocking animation)
            singleBulletBehavior.RequestFinishReload();
            Debug.Log($"{displayName}: Requesting finish reload animation before shooting");
            return false; // Return false to prevent immediate shooting
        }

        return false;
    }

    /// <summary>
    /// Called when this weapon becomes active. Starts the switch cooldown.
    /// </summary>
    public void OnWeaponActivated()
    {
        _switchCooldown = weaponSwitchDelay;
        Debug.Log($"{displayName} activated - switch cooldown: {weaponSwitchDelay}s");
    }

    /// <summary>
    /// Triggers switch out animation and returns the delay before weapon should be hidden.
    /// </summary>
    /// <returns>Delay in seconds before weapon should be deactivated</returns>
    public float TriggerSwitchOutAnimation()
    {
        // Cancel ADS when switching weapons
        if (_isAiming)
        {
            Debug.Log($"{displayName}: Canceling ADS due to weapon switch");
            SetAiming(false);
        }
        
        if (animator != null && !string.IsNullOrEmpty(switchOutTriggerName))
        {
            animator.SetTrigger(switchOutTriggerName);
            Debug.Log($"{displayName} triggered switch out animation - delay: {switchOutAnimationDelay}s");
        }
        return switchOutAnimationDelay;
    }
    
    /// <summary>
    /// Sets the aiming state for this weapon
    /// </summary>
    public void SetAiming(bool aiming)
    {
        if (!supportsADS) return;
        
        // Prevent ADS during weapon state changes (but allow during shooting/cooldown)
        if (aiming && (IsReloading || IsSwitching))
        {
            return;
        }
        
        _isAiming = aiming;
        
        // Trigger animation if state changed
        if (_isAiming != _wasAiming && animator != null)
        {
            if (!string.IsNullOrEmpty(adsAnimationBool))
                animator.SetBool(adsAnimationBool, _isAiming);
                
            // ADS trigger removed - only using bool parameter
        }
        
        _wasAiming = _isAiming;
    }
    
    /// <summary>
    /// Initializes crosshair sprite for this weapon
    /// </summary>
    void InitializeCrosshair()
    {
        // Crosshair sprites are assigned manually in the inspector
        // If no crosshair is assigned, the CrosshairManager will use the default crosshair
        if (crosshairSprite != null)
        {
            Debug.Log($"WeaponInstance_Hitscan ({displayName}): Using assigned crosshair sprite");
        }
        else
        {
            Debug.Log($"WeaponInstance_Hitscan ({displayName}): No crosshair assigned, will use default crosshair");
        }
    }
    
    /// <summary>
    /// Initializes the ADS system components
    /// </summary>
    void InitializeADS()
    {
        // Find main camera
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            _mainCamera = FindFirstObjectByType<Camera>();
        }
        
        // Store original FOV
        if (_mainCamera != null)
        {
            _originalFOV = _mainCamera.fieldOfView;
        }
        
        // Auto-find weapon GFX if not assigned
        if (weaponGFX == null && autoFindGFX)
        {
            // Look for common GFX object names
            Transform gfxChild = transform.Find("GFX") ?? 
                                transform.Find("Model") ?? 
                                transform.Find("Mesh") ?? 
                                transform.Find("Visual");
            
            if (gfxChild != null)
            {
                weaponGFX = gfxChild;
                Debug.Log($"WeaponInstance_Hitscan ({displayName}): Auto-found weapon GFX: {weaponGFX.name}");
            }
            else if (transform.childCount > 0)
            {
                // Fallback to first child if no common names found
                weaponGFX = transform.GetChild(0);
                Debug.Log($"WeaponInstance_Hitscan ({displayName}): Using first child as weapon GFX: {weaponGFX.name}");
            }
        }
        
        // Store original GFX position
        if (weaponGFX != null)
        {
            _originalGFXPosition = weaponGFX.localPosition;
        }
        else
        {
            Debug.LogWarning($"WeaponInstance_Hitscan ({displayName}): No weapon GFX object assigned! ADS positioning will not work.");
        }
    }
    
    /// <summary>
    /// Updates ADS camera and weapon GFX positioning
    /// </summary>
    void UpdateADS()
    {
        if (!supportsADS || _mainCamera == null) return;
        
        // Don't update ADS if weapon is not active
        if (!gameObject.activeInHierarchy) return;
        
        // Smoothly transition camera FOV
        float targetFOV = _isAiming ? adsFOV : _originalFOV;
        _mainCamera.fieldOfView = Mathf.Lerp(_mainCamera.fieldOfView, targetFOV, adsTransitionSpeed * Time.deltaTime);
        
        // Smoothly transition weapon GFX position
        if (weaponGFX != null)
        {
            Vector3 targetPosition = _isAiming ? _originalGFXPosition + adsPosition : _originalGFXPosition;
            
            weaponGFX.localPosition = Vector3.Lerp(weaponGFX.localPosition, targetPosition, adsPositionSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Recalculate all stats based on current tier.
    /// </summary>
    void RecalculateStats()
    {
        WeaponTierData tierData = WeaponTierSystem.GetTierData(currentTier);
        
        damage = baseDamage * tierData.damageMultiplier;
        fireRate = baseFireRate; // No speed upgrades
        magSize = Mathf.RoundToInt(baseMagSize * tierData.magSizeMultiplier);
        reloadTime = baseReloadTime; // No speed upgrades
        spread = baseSpread * tierData.spreadMultiplier;
        
        // Reload behaviors use base timing (no speed upgrades)
        if (_magazineReloadBehavior != null)
        {
            _magazineReloadBehavior.SetReloadDuration(baseReloadTime);
        }
        
        // Single bullet reload uses Inspector timing values directly
    }

    /// <summary>
    /// Upgrade weapon to the next tier.
    /// </summary>
    public bool TryUpgradeTier()
    {
        if (!WeaponTierSystem.CanUpgrade(currentTier))
        {
            Debug.Log($"{displayName} is already at max tier (Legendary)!");
            return false;
        }

        WeaponTier? nextTier = WeaponTierSystem.GetNextTier(currentTier);
        if (nextTier.HasValue)
        {
            currentTier = nextTier.Value;
            RecalculateStats();
            
            // Refill magazine on upgrade
            _inMag = magSize;
            
            OnTierChanged?.Invoke(displayName, TierName);
            OnTierUpgraded?.Invoke(currentTier);
            OnAmmoChanged?.Invoke(_inMag, _reserve);
            UpdateAnimatorAmmo(); // Update animator parameter after tier upgrade
            
            Debug.Log($"{displayName} upgraded to {TierName}!");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Get the cost to upgrade this weapon to the next tier.
    /// </summary>
    public int GetUpgradeCost()
    {
        return WeaponTierSystem.GetUpgradeCost(currentTier);
    }

    /// <summary>
    /// Check if this weapon can be upgraded.
    /// </summary>
    public bool CanUpgrade()
    {
        return WeaponTierSystem.CanUpgrade(currentTier);
    }

    // Legacy method for compatibility (deprecated)
    [System.Obsolete("Use TryUpgradeTier() instead")]
    public void ApplyTier(string newTierName, int newMagSize, float newDamage, float newReloadTime, float newSpread)
    {
        currentTier = WeaponTierSystem.ParseTierName(newTierName);
        RecalculateStats();
        _inMag = Mathf.Min(_inMag, magSize);
        OnTierChanged?.Invoke(displayName, TierName);
        OnAmmoChanged?.Invoke(_inMag, _reserve);
        UpdateAnimatorAmmo(); // Update animator parameter
    }

    public void AddReserve(int amount)
    {
        _reserve = Mathf.Max(0, _reserve + amount);
        OnAmmoChanged?.Invoke(_inMag, _reserve);
        // Note: Reserve ammo doesn't affect animator parameter (only magazine does)
    }
    
    /// <summary>
    /// Initialize the reload behavior system based on selected reload type
    /// </summary>
    void InitializeReloadBehavior()
    {
        switch (reloadType)
        {
            case ReloadType.Magazine:
                _magazineReloadBehavior.Initialize(this, animator);
                _magazineReloadBehavior.SetReloadDuration(baseReloadTime);
                _reloadBehavior = _magazineReloadBehavior;
                Debug.Log($"WeaponInstance_Hitscan ({displayName}): Using Magazine reload behavior");
                break;
                
            case ReloadType.SingleBullet:
                singleBulletReload.Initialize(this, animator);
                _reloadBehavior = singleBulletReload;
                Debug.Log($"WeaponInstance_Hitscan ({displayName}): Using Single Bullet reload behavior");
                break;
        }
    }
    
    #region Bloom System
    
    void InitializeBloomSystem()
    {
        // Initialize bloom to minimum value
        _currentBloom = minBloom;
        
        // Find PlayerMovement component for movement detection
        _playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (_playerMovement == null)
        {
            Debug.LogWarning($"WeaponInstance_Hitscan ({displayName}): PlayerMovement not found! Movement bloom will not work.");
        }
    }
    
    void UpdateBloomSystem()
    {
        // Check if player is moving
        bool wasMoving = _isPlayerMoving;
        _isPlayerMoving = IsPlayerMoving();
        
        float deltaTime = Time.deltaTime;
        
        // Determine max bloom and decay rate based on ADS state
        float currentMaxBloom = _isAiming ? maxBloomADS : maxBloom;
        float currentDecayRate = _isAiming ? bloomDecayRateADS : bloomDecayRate;
        
        if (_isPlayerMoving)
        {
            // Increase bloom when moving to current max bloom
            _currentBloom = Mathf.MoveTowards(_currentBloom, currentMaxBloom, movementBloomRate * deltaTime);
        }
        else
        {
            // Decrease bloom when not moving
            _currentBloom = Mathf.MoveTowards(_currentBloom, minBloom, currentDecayRate * deltaTime);
        }
        
        // Clamp bloom to valid range
        _currentBloom = Mathf.Clamp(_currentBloom, minBloom, currentMaxBloom);
    }
    
    bool IsPlayerMoving()
    {
        if (_playerMovement == null) return false;
        
        // Check if player has horizontal movement input
        // This checks actual velocity magnitude
        CharacterController controller = _playerMovement.GetComponent<CharacterController>();
        if (controller != null)
        {
            Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
            return horizontalVelocity.magnitude > 0.1f;
        }
        
        return false;
    }
    
    float CalculateCurrentSpread()
    {
        // Return current bloom value (ADS is already handled in bloom system)
        return _currentBloom;
    }
    
    void AddShootingBloom()
    {
        // Instantly set bloom to maximum when shooting (ADS-aware)
        _currentBloom = _isAiming ? maxBloomADS : maxBloom;
    }
    
    /// <summary>
    /// Get current bloom value for debugging or UI display
    /// </summary>
    public float GetCurrentBloom()
    {
        return _currentBloom;
    }
    
    /// <summary>
    /// Get bloom as a percentage (0-1) where 0 is min bloom and 1 is max bloom
    /// </summary>
    public float GetBloomPercentage()
    {
        return Mathf.InverseLerp(minBloom, maxBloom, _currentBloom);
    }
    
    #endregion
}