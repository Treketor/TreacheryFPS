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
    [Tooltip("Base reload animation duration in seconds (should match your animation length)")]
    public float baseReloadTime = 1.2f;
    [Tooltip("Base spread/accuracy - this becomes the minimum bloom")]
    public float baseSpread = 1.5f;
    [Tooltip("Base bullet force applied to ragdolls on death")]
    public float baseBulletForce = 400f;
    
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
    float bulletForce;

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
    [SerializeField] GameObject[] muzzleFlashObjects;
    [Tooltip("Duration of muzzle flash effect in seconds")]
    [SerializeField] float muzzleFlashDuration = 0.05f;

    [Header("Bullet Impact Effects")]
    [SerializeField] bool enableImpactEffects = true;
    [Tooltip("Whether to spawn impact effects when bullets hit surfaces")]
    [SerializeField] bool enableEnemyImpactEffects = true;
    [Tooltip("Use separate impact effects for hitting enemies")]
    [SerializeField] GameObject enemyImpactEffectPrefab;
    [Tooltip("Custom impact effect prefab for hitting enemies")]
    [SerializeField] float enemyImpactEffectLifetime = 2f;
    [Tooltip("How long the enemy impact effect lasts before being destroyed")]
    [SerializeField] bool parentToHitObject = true;
    [Tooltip("Make the enemy impact effect a child of the hit object (follows movement)")]
    
    [Header("Pellet System (Shotgun)")]
    [SerializeField] bool usePelletSystem = false;
    [Tooltip("Enable to fire multiple pellets per shot (shotgun behavior)")]
    [SerializeField] int pelletsPerShot = 8;
    [Tooltip("Number of pellets fired per trigger pull")]
    [SerializeField] int bulletsPerShot = 1;
    [Tooltip("Number of bullets consumed from magazine per shot (for dual weapons, set to 2)")]
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
    // [SerializeField] string reloadTriggerName = "Reload"; // Unused - commented out to eliminate warning
    [SerializeField] string switchOutTriggerName = "Switch Out";
    [SerializeField] string currentAmmoParameterName = "Current Ammo";
    [SerializeField] bool autoFindAnimator = true;
    
    [Header("Reload System")]
    [Tooltip("Choose between Magazine (traditional) or SingleBullet (pump shotgun style) reload")]
    [SerializeField] ReloadType reloadType = ReloadType.Magazine;
    
    [Header("Magazine Reload Settings")]
    // [Tooltip("Extra time after reload animation completes before weapon is ready (ADS, shooting, etc.)")]
    // [SerializeField] float reloadDelayBuffer = 0.5f; // Unused - commented out to eliminate warning
    
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
    // [Tooltip("Spread multiplier when aiming (lower = more accurate)")]
    // [SerializeField] float adsSpreadMultiplier = 0.3f; // Unused - commented out to eliminate warning
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
    public float CurrentBulletForce => bulletForce;
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
        }
        
        // Initialize crosshair system
        InitializeCrosshair();
        
        // Initialize ADS system
        InitializeADS();
        
        // Initialize bloom system
        InitializeBloomSystem();
        
        // Initialize reload behavior system
        InitializeReloadBehavior();

        // Make sure all muzzle flash objects start disabled
        if (muzzleFlashObjects != null)
        {
            foreach (GameObject muzzleFlash in muzzleFlashObjects)
            {
                if (muzzleFlash != null)
                {
                    muzzleFlash.SetActive(false);
                }
            }
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

        // Check if we have enough bullets to fire
        if (_inMag < bulletsPerShot)
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
        _inMag -= bulletsPerShot; // Consume multiple bullets per shot
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
            }
        
        // Trigger muzzle flash
        if (muzzleFlashObjects != null && muzzleFlashObjects.Length > 0)
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

        // Check if we hit a head collider by looking for HeadshotZone in parent
        HeadshotZone headshotZone = hit.collider.GetComponentInParent<HeadshotZone>();
        if (headshotZone != null && headshotZone.IsHeadCollider(hit.collider))
        {
            isHeadshot = headshotZone.ProcessHeadshot(falloffDamage, hit.point, hit.normal, bulletForce, out finalDamage);
            
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
                // Try to use EnemyHealth overload with bullet force if available
                if (damageable is EnemyHealth enemyHealth)
                {
                    enemyHealth.TakeDamage(falloffDamage, hit.point, Vector3.zero, CurrentBulletForce);
                }
                else
                {
                    damageable.TakeDamage(falloffDamage, hit.point, Vector3.zero);
                }
            }
        }

        // Spawn appropriate impact effect
        SpawnImpactEffect(hit, isHeadshot);
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

        // Check if we hit a head collider by looking for HeadshotZone in parent
        HeadshotZone headshotZone = hit.collider.GetComponentInParent<HeadshotZone>();
        if (headshotZone != null && headshotZone.IsHeadCollider(hit.collider))
        {
            isHeadshot = headshotZone.ProcessHeadshot(falloffDamage, hit.point, hit.normal, CurrentBulletForce, out finalDamage);
            
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
                // Try to use EnemyHealth overload with bullet force if available
                if (damageable is EnemyHealth enemyHealthBullet)
                {
                    enemyHealthBullet.TakeDamage(falloffDamage, hit.point, hit.normal, CurrentBulletForce);
                }
                else
                {
                    damageable.TakeDamage(falloffDamage, hit.point, hit.normal);
                }
            }
        }

        // Spawn appropriate impact effect
        SpawnImpactEffect(hit, isHeadshot);
    }

    /// <summary>
    /// Spawn appropriate impact effect based on what was hit
    /// </summary>
    private void SpawnImpactEffect(RaycastHit hit, bool isHeadshot)
    {
        if (!enableImpactEffects) return;

        // Check if we hit an enemy by looking for IDamageable or HeadshotZone components
        bool hitEnemy = hit.collider.GetComponent<IDamageable>() != null || 
                       hit.collider.GetComponentInParent<IDamageable>() != null ||
                       hit.collider.GetComponent<HeadshotZone>() != null;
                       
        // Also check if we hit a ragdoll (former enemy)
        if (!hitEnemy)
        {
            ZombieRagdoll ragdoll = hit.collider.GetComponentInParent<ZombieRagdoll>();
            if (ragdoll != null && ragdoll.IsRagdoll)
            {
                hitEnemy = true;
            }
        }

        // Use enemy impact effect if hitting an enemy and it's enabled
        if (hitEnemy && enableEnemyImpactEffects && enemyImpactEffectPrefab != null)
        {
            // Spawn custom enemy impact effect
            GameObject impactEffect = Instantiate(enemyImpactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            
            // Parent to hit object if enabled
            if (parentToHitObject)
            {
                impactEffect.transform.SetParent(hit.collider.transform);
            }
            
            // Destroy after specified lifetime
            Destroy(impactEffect, enemyImpactEffectLifetime);
        }
        // Use standard impact effect for non-enemies or as fallback
        else if (BulletImpactManager.Instance != null)
        {
            BulletImpactManager.Instance.SpawnImpactEffect(hit.point, hit.normal, hit.collider);
        }
    }

    IEnumerator ShowMuzzleFlash()
    {
        // Activate all muzzle flash objects
        foreach (GameObject muzzleFlash in muzzleFlashObjects)
        {
            if (muzzleFlash != null)
            {
                muzzleFlash.SetActive(true);
            }
        }
        
        // Wait for brief duration
        yield return new WaitForSeconds(muzzleFlashDuration);
        
        // Deactivate all muzzle flash objects
        foreach (GameObject muzzleFlash in muzzleFlashObjects)
        {
            if (muzzleFlash != null)
            {
                muzzleFlash.SetActive(false);
            }
        }
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
        
    }

    /// <summary>
    /// Callback when reload is completed
    /// </summary>
    private void OnReloadComplete()
    {

    }

    /// <summary>
    /// Callback when reload is cancelled
    /// </summary>
    private void OnReloadCancelled()
    {

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

        // Don't interrupt reload if we don't have enough bullets to shoot
        if (_inMag < bulletsPerShot)
            return false;

        // Check if the single bullet reload can be safely interrupted
        var singleBulletBehavior = _reloadBehavior as SingleBulletReloadBehavior;
        if (singleBulletBehavior != null && singleBulletBehavior.CanBeInterrupted())
        {
            // Instead of cancelling, request finish reload (cocking animation)
            singleBulletBehavior.RequestFinishReload();
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
            SetAiming(false);
        }
        
        if (animator != null && !string.IsNullOrEmpty(switchOutTriggerName))
        {
            animator.SetTrigger(switchOutTriggerName);

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
            
        }
        else
        {
            // Use default crosshair
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
        bulletForce = baseBulletForce * tierData.damageMultiplier; // Scale force with damage
        
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

                break;
                
            case ReloadType.SingleBullet:
                singleBulletReload.Initialize(this, animator);
                _reloadBehavior = singleBulletReload;
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