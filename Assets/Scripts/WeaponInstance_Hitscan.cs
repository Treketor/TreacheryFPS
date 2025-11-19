using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] float minBloom = 0.5f;
    [Tooltip("Tightest and most accurate bloom value")]
    [SerializeField] float maxBloom = 4.0f;
    [Tooltip("Widest and least accurate bloom value")]
    [SerializeField] float bloomDecayRate = 3.0f;
    [Tooltip("How quick the bloom goes to minimum")]
    [SerializeField] float movementBloomRate = 2.0f;
    [Tooltip("How quick the bloom goes to maximum when moving")]
    [SerializeField] float maxBloomADS = 2.0f;
    [Tooltip("Maximum bloom value when ADS")]
    [SerializeField] float bloomDecayRateADS = 4.0f;
    [Tooltip("How quick the bloom goes to minimum when ADS")]
    
    [Header("Current Tier")]
    [SerializeField] WeaponTier currentTier = WeaponTier.Common;
    
    // Current stats (calculated from base * tier multipliers)
    float damage;
    float fireRate;
    int magSize;
    float reloadTime;
    float spread;

    [Header("Ammo Pools")]
    public int startingReserve = 60;

    [Header("References")]
    public WeaponRaycaster raycaster;
    public LayerMask hitMask;
    
    [Header("Recoil")]
    [SerializeField] WeaponRecoil weaponRecoil;
    [SerializeField] float recoilMultiplier = 1f;
    [SerializeField] bool autoFindRecoil = true;

    [Header("Muzzle Flash")]
    [SerializeField] GameObject muzzleFlashObject;
    [SerializeField] float muzzleFlashDuration = 0.05f;

    [Header("Bullet Impact Effects")]
    [SerializeField] bool enableImpactEffects = true;
    [Tooltip("Whether to spawn impact effects when bullets hit surfaces")]

    [Header("Animation")]
    [SerializeField] Animator animator;
    [SerializeField] string shootTriggerName = "Shoot";
    [SerializeField] string reloadTriggerName = "Reload";
    [SerializeField] string switchOutTriggerName = "Switch Out";
    [SerializeField] string currentAmmoParameterName = "Current Ammo";
    [SerializeField] bool autoFindAnimator = true;
    
    [Header("Shoot Animation Timing")]
    [SerializeField] float baseShootAnimationDuration = 0.2f;
    [Tooltip("Base fire rate (shots per second) for normal animation speed")]
    [SerializeField] float baseFireRateForAnimation = 5f;
    
    [Header("Reload Animation Timing")]
    [SerializeField] float baseReloadAnimationDuration = 2.0f;
    [Tooltip("Extra time after animation completes before weapon is ready (in seconds)")]
    [SerializeField] float reloadDelayBuffer = 0.5f;
    
    [Header("Weapon Switch Timing")]
    [SerializeField] float weaponSwitchDelay = 0.3f;
    [Tooltip("Time after switching to this weapon before it can be used")]
    [SerializeField] float switchOutAnimationDelay = 0.5f;
    [Tooltip("Time to wait after triggering Switch Out animation before hiding weapon")]
    
    [Header("Aim Down Sights")]
    [SerializeField] bool supportsADS = true;
    [SerializeField] Transform weaponGFX;
    [Tooltip("Auto-find weapon GFX object by name")]
    [SerializeField] bool autoFindGFX = true;
    [Tooltip("FOV when aiming down sights")]
    [SerializeField] float adsFOV = 40f;
    [Tooltip("How fast the camera transitions to ADS FOV")]
    [SerializeField] float adsTransitionSpeed = 8f;
    [Tooltip("Weapon GFX position when aiming down sights")]
    [SerializeField] Vector3 adsPosition = new Vector3(0f, 0f, 0.2f);
    [Tooltip("How fast weapon GFX moves to ADS position")]
    [SerializeField] float adsPositionSpeed = 12f;
    [Tooltip("Spread reduction when aiming (multiplier)")]
    [SerializeField] float adsSpreadMultiplier = 0.3f;
    [Tooltip("Recoil reduction when aiming (multiplier)")]
    [SerializeField] float adsRecoilMultiplier = 0.5f;
    [Tooltip("Animation bool parameter name for ADS state")]
    [SerializeField] string adsAnimationBool = "IsAiming";
    
    [Header("Crosshair")]
    [SerializeField] Sprite crosshairSprite;
    [Tooltip("Custom crosshair for this weapon")]

    float _cooldown;
    float _switchCooldown;
    int _inMag;
    
    // Bloom system variables
    float _currentBloom;
    bool _isPlayerMoving;
    PlayerMovement _playerMovement;
    int _reserve;
    bool _reloading;
    
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
    public bool IsReloading => _reloading;
    public bool IsSwitching => _switchCooldown > 0f;
    public bool IsReady => !_reloading && _cooldown <= 0f && _switchCooldown <= 0f;
    public bool IsAiming => _isAiming;
    public Sprite CrosshairSprite => crosshairSprite;

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
        if (_reloading || _cooldown > 0f || _switchCooldown > 0f) return;

        // Auto-reload if magazine is empty
        if (_inMag <= 0)
        {
            TryReload();
            return;
        }

        _cooldown = 1f / fireRate;
        _inMag--;
        OnAmmoChanged?.Invoke(_inMag, _reserve);
        UpdateAnimatorAmmo(); // Update animator parameter after ammo changes

        // Calculate and apply shoot animation speed
        // Formula: animSpeed = (fireRate / baseFireRate)
        // This makes animation faster for higher fire rates
        // Example: baseFireRate = 5/s, fireRate = 5/s → speed = 1.0x (normal)
        // Example: baseFireRate = 5/s, fireRate = 10/s → speed = 2.0x (twice as fast)
        if (animator != null)
        {
            float animationSpeed = fireRate / baseFireRateForAnimation;
            animator.speed = animationSpeed;
            
            // Trigger shoot animation
            if (!string.IsNullOrEmpty(shootTriggerName))
            {
                animator.SetTrigger(shootTriggerName);
            }
            
            // Reset animator speed after shoot animation completes
            StartCoroutine(ResetAnimatorSpeedAfterShoot(baseShootAnimationDuration / animationSpeed));
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
        
        if (raycaster.TryShoot(out var hit, bloomSpread))
        {
            bool isHeadshot = false;
            float finalDamage = damage;

            // Check for headshot zone first
            if (hit.collider.TryGetComponent<HeadshotZone>(out var headshotZone))
            {
                isHeadshot = headshotZone.ProcessHeadshot(damage, hit.point, hit.normal, out finalDamage);
                
                // Register headshot for accuracy tracking
                if (isHeadshot && ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.RegisterHeadshot();
                }
            }
            // Otherwise check for regular IDamageable on the hit object or its parent
            else 
            {
                // Try to find IDamageable on hit object or parent hierarchy
                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable == null)
                    damageable = hit.collider.GetComponentInParent<IDamageable>();

                if (damageable != null)
                {
                    damageable.TakeDamage(damage, hit.point, hit.normal);
                }
            }

            // Spawn impact effect at hit point
            if (enableImpactEffects && BulletImpactManager.Instance != null)
            {
                BulletImpactManager.Instance.SpawnImpactEffect(hit.point, hit.normal, hit.collider);
            }
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

    IEnumerator ResetAnimatorSpeedAfterShoot(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (animator != null && !_reloading) // Don't reset if reloading (reload manages its own speed)
        {
            animator.speed = 1.0f;
        }
    }

    public void TryReload()
    {
        if (_reloading || _switchCooldown > 0f) return;
        if (_inMag >= magSize) return; // already full
        if (_reserve <= 0) return; // no reserve ammo

        // Cancel ADS when starting reload
        if (_isAiming)
        {
            SetAiming(false);
        }

        StartCoroutine(ReloadCoroutine());
    }

    IEnumerator ReloadCoroutine()
    {
        _reloading = true;
        
        // Calculate animation speed based on reload time
        // Formula: animSpeed = baseAnimDuration / (reloadTime - buffer)
        // Example: baseAnimDuration = 2.0s, reloadTime = 2.5s (base), buffer = 0.5s
        //          animSpeed = 2.0 / (2.5 - 0.5) = 2.0 / 2.0 = 1.0x (normal speed)
        // Example: reloadTime = 2.0s (upgraded), buffer = 0.5s
        //          animSpeed = 2.0 / (2.0 - 0.5) = 2.0 / 1.5 = 1.33x (faster)
        
        float targetAnimationDuration = Mathf.Max(0.1f, reloadTime - reloadDelayBuffer);
        float animationSpeed = baseReloadAnimationDuration / targetAnimationDuration;
        
        // Apply animation speed
        if (animator != null)
        {
            animator.speed = animationSpeed;
            
            // Trigger reload animation
            if (!string.IsNullOrEmpty(reloadTriggerName))
            {
                animator.SetTrigger(reloadTriggerName);
            }
        }
        
        yield return new WaitForSeconds(reloadTime);

        // Reset animator speed to normal
        if (animator != null)
        {
            animator.speed = 1.0f;
        }

        int needed = magSize - _inMag;
        int taken = Mathf.Min(needed, _reserve);
        _inMag += taken;
        _reserve -= taken;

        _reloading = false;
        OnAmmoChanged?.Invoke(_inMag, _reserve);
        UpdateAnimatorAmmo(); // Update animator parameter after reload
    }

    /// <summary>
    /// Cancel/interrupt the current reload. Used when switching weapons during reload.
    /// </summary>
    public void CancelReload()
    {
        if (_reloading)
        {
            StopAllCoroutines(); // Stop the reload coroutine
            _reloading = false;
            
            // Reset animator speed to normal if it was changed during reload
            if (animator != null)
            {
                animator.speed = 1.0f;
            }
            
            Debug.Log($"Reload cancelled for {displayName}");
        }
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
        fireRate = baseFireRate * tierData.fireRateMultiplier;
        magSize = Mathf.RoundToInt(baseMagSize * tierData.magSizeMultiplier);
        reloadTime = baseReloadTime * tierData.reloadTimeMultiplier;
        spread = baseSpread * tierData.spreadMultiplier;
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