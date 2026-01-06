using UnityEngine;
using UnityEngine.UI;
using Treachery.Weapons.Interfaces;
using Treachery.Weapons.Runtime;

public class CrosshairManager : MonoBehaviour
{
    [Header("Crosshair Display")]
    [SerializeField] Image crosshairImage;
    [SerializeField] bool autoFindCrosshair = true;
    [Tooltip("Automatically find crosshair Image component")]
    
    [Header("Enemy Detection")]
    [SerializeField] Color enemyDetectionColor = Color.red;
    [SerializeField] LayerMask enemyLayers = (1 << 6) | (1 << 7); // Default to layer 6 (Enemy) and layer 7 (EnemyHeadshot)
    [SerializeField] float detectionRange = 100f;
    
    [Header("Default Crosshairs")]
    [SerializeField] Sprite defaultCrosshair;
    [Tooltip("Fallback crosshair when weapon has no specific crosshair")]
    
    [Header("Bloom Visualization")]
    [SerializeField] bool enableBloomScaling = true;
    [Tooltip("Whether crosshair should scale with weapon bloom")]
    [SerializeField] float minCrosshairScale = 0.8f;
    [Tooltip("Crosshair scale at minimum bloom (tightest)")]
    [SerializeField] float maxCrosshairScale = 2.5f;
    [Tooltip("Crosshair scale at maximum bloom (widest)")]
    [SerializeField] float scaleTransitionSpeed = 8f;
    [Tooltip("How fast crosshair scales to target size")]
    
    private IWeaponController weaponController;
    private Sprite currentCrosshair;
    private bool lastVisibleState = true;
    private Color originalColor;
    private bool isOverEnemy = false;
    private Camera playerCamera;
    
    // Bloom visualization
    private Vector3 originalCrosshairScale;
    private float targetCrosshairScale = 1f;
    private float currentCrosshairScale = 1f;
    
    // Singleton for easy access
    public static CrosshairManager Instance { get; private set; }
    
    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple CrosshairManager instances found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // Auto-find crosshair Image component
        if (crosshairImage == null && autoFindCrosshair)
        {
            // Look for common crosshair names first
            GameObject crosshairObj = GameObject.Find("Crosshair") ?? 
                                    GameObject.Find("crosshair") ??
                                    GameObject.Find("Reticle") ??
                                    GameObject.Find("reticle") ??
                                    GameObject.Find("CrossHair") ??
                                    GameObject.Find("UI_Crosshair");
            
            if (crosshairObj != null)
            {
                crosshairImage = crosshairObj.GetComponent<Image>();
                if (crosshairImage != null)
                {
                    Debug.Log($"CrosshairManager: Auto-found crosshair Image: {crosshairObj.name}");
                }
            }
            
            // Fallback: search all Images for crosshair-like names
            if (crosshairImage == null)
            {
                Image[] allImages = FindObjectsByType<Image>(FindObjectsSortMode.None);
                foreach (var image in allImages)
                {
                    if (image.name.ToLower().Contains("crosshair") || image.name.ToLower().Contains("reticle"))
                    {
                        crosshairImage = image;
                        Debug.Log($"CrosshairManager: Auto-found crosshair Image: {image.name}");
                        break;
                    }
                }
            }
        }
        
        // Find weapon controller
        var v2 = FindFirstObjectByType<WeaponControllerV2>();
        if (v2 != null)
            weaponController = v2;
        else
            weaponController = FindFirstObjectByType<WeaponController>();

        if (weaponController == null)
            Debug.LogWarning("CrosshairManager: No weapon controller found (WeaponControllerV2 or WeaponController)!");
        
        if (crosshairImage == null)
        {
            Debug.LogWarning("CrosshairManager: No crosshair Image component assigned or found!");
        }
        else
        {
            // Store original crosshair color and scale
            originalColor = crosshairImage.color;
            originalCrosshairScale = crosshairImage.transform.localScale;
            currentCrosshairScale = 1f;
        }
        
        // Get player camera reference
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindFirstObjectByType<Camera>();
        }
        
        // Set initial crosshair
        UpdateCrosshair();
    }
    
    void Update()
    {
        // Check if weapon changed, crosshair changed, or ADS state changed
        if (weaponController != null && weaponController.CurrentWeapon != null)
        {
            Sprite weaponCrosshair = weaponController.CurrentWeapon.CrosshairSprite;
            bool isAiming = weaponController.CurrentWeapon.IsAiming;
            bool currentlyVisible = crosshairImage != null && crosshairImage.gameObject.activeInHierarchy;
            bool shouldBeVisible = (weaponCrosshair != null || defaultCrosshair != null) && !isAiming;
            
            // Update if crosshair changed or visibility state should change
            if (weaponCrosshair != currentCrosshair || currentlyVisible != shouldBeVisible)
            {
                UpdateCrosshair();
            }
            
            if (currentlyVisible != lastVisibleState)
            {
                lastVisibleState = currentlyVisible;
            }
        }
        
        // Check for enemy detection
        CheckEnemyDetection();
        
        // Update crosshair bloom scaling
        UpdateBloomScaling();
    }
    
    /// <summary>
    /// Updates the crosshair based on current weapon
    /// </summary>
    public void UpdateCrosshair()
    {
        if (crosshairImage == null) return;
        
        Sprite newCrosshair = defaultCrosshair; // Always start with default
        bool isAiming = false;
        
        // Get crosshair from current weapon and check ADS state
        if (weaponController != null && weaponController.CurrentWeapon != null)
        {
            isAiming = weaponController.CurrentWeapon.IsAiming;
            
            // Use weapon-specific crosshair if available, otherwise use default
            Sprite weaponCrosshair = weaponController.CurrentWeapon.CrosshairSprite;
            if (weaponCrosshair != null)
            {
                newCrosshair = weaponCrosshair;
            }
            // If no weapon crosshair and no default, hide crosshair
            else if (defaultCrosshair == null)
            {
                newCrosshair = null;
            }
        }
        
        // Update crosshair sprite if changed
        if (newCrosshair != currentCrosshair)
        {
            currentCrosshair = newCrosshair;
            crosshairImage.sprite = currentCrosshair;
            
            // Update original color when crosshair changes
            if (crosshairImage.sprite != null && !isOverEnemy)
            {
                originalColor = crosshairImage.color;
            }
            
            if (weaponController != null && weaponController.CurrentWeapon != null)
            {
                string crosshairType = (currentCrosshair == defaultCrosshair) ? "default" : "weapon-specific";
                Debug.Log($"CrosshairManager: Updated Image to {crosshairType} crosshair for {weaponController.CurrentWeapon.DisplayName}");
            }
        }
        
        // Hide crosshair during ADS or if no crosshair available
        bool shouldShowCrosshair = currentCrosshair != null && !isAiming;
        crosshairImage.gameObject.SetActive(shouldShowCrosshair);
    }
    
    /// <summary>
    /// Manually set crosshair sprite
    /// </summary>
    public void SetCrosshair(Sprite crosshair)
    {
        if (crosshairImage == null) return;
        
        currentCrosshair = crosshair;
        crosshairImage.sprite = crosshair;
        crosshairImage.gameObject.SetActive(crosshair != null);
        
        // Update original color when crosshair changes
        if (crosshair != null && !isOverEnemy)
        {
            originalColor = crosshairImage.color;
        }
    }
    
    /// <summary>
    /// Show or hide crosshair (for ADS integration)
    /// </summary>
    public void SetCrosshairVisible(bool visible)
    {
        if (crosshairImage != null)
        {
            bool shouldShow = visible && currentCrosshair != null;
            crosshairImage.gameObject.SetActive(shouldShow);
        }
    }
    
    /// <summary>
    /// Called when weapon is changed to update crosshair
    /// </summary>
    void OnWeaponChanged()
    {
        UpdateCrosshair();
    }
    
    /// <summary>
    /// Check if crosshair is over an enemy using raycast
    /// </summary>
    void CheckEnemyDetection()
    {
        if (crosshairImage == null || playerCamera == null) return;
        
        // Raycast from center of screen
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        RaycastHit hit;
        
        bool enemyDetected = false;
        
        if (Physics.Raycast(ray, out hit, detectionRange, enemyLayers))
        {
            enemyDetected = true;
        }
        
        // Update crosshair color based on enemy detection
        if (enemyDetected != isOverEnemy)
        {
            isOverEnemy = enemyDetected;
            
            if (isOverEnemy)
            {
                crosshairImage.color = enemyDetectionColor;
            }
            else
            {
                crosshairImage.color = originalColor;
            }
        }
    }
    
    /// <summary>
    /// Manually set the enemy detection color
    /// </summary>
    public void SetEnemyDetectionColor(Color color)
    {
        enemyDetectionColor = color;
        
        // Update immediately if currently over enemy
        if (isOverEnemy && crosshairImage != null)
        {
            crosshairImage.color = enemyDetectionColor;
        }
    }
    
    /// <summary>
    /// Get current enemy detection state
    /// </summary>
    public bool IsOverEnemy()
    {
        return isOverEnemy;
    }
    
    #region Bloom Visualization
    
    /// <summary>
    /// Update crosshair scaling based on current weapon bloom
    /// </summary>
    void UpdateBloomScaling()
    {
        if (!enableBloomScaling || crosshairImage == null) return;
        
        // Get current bloom from active weapon
        float bloomPercentage = 0f;
        if (weaponController != null && weaponController.CurrentWeapon != null)
        {
            if (weaponController.CurrentWeapon is IWeaponBloomProvider bloomProvider)
                bloomPercentage = bloomProvider.GetBloomPercentage();
        }
        
        // Calculate target scale based on bloom percentage
        targetCrosshairScale = Mathf.Lerp(minCrosshairScale, maxCrosshairScale, bloomPercentage);
        
        // Smoothly transition to target scale
        currentCrosshairScale = Mathf.Lerp(currentCrosshairScale, targetCrosshairScale, scaleTransitionSpeed * Time.deltaTime);
        
        // Apply scale to crosshair
        Vector3 newScale = originalCrosshairScale * currentCrosshairScale;
        crosshairImage.transform.localScale = newScale;
    }
    
    /// <summary>
    /// Manually set crosshair bloom scaling settings
    /// </summary>
    /// <param name="enabled">Whether bloom scaling is enabled</param>
    /// <param name="minScale">Scale at minimum bloom</param>
    /// <param name="maxScale">Scale at maximum bloom</param>
    public void SetBloomScaling(bool enabled, float minScale = 0.8f, float maxScale = 2.5f)
    {
        enableBloomScaling = enabled;
        minCrosshairScale = minScale;
        maxCrosshairScale = maxScale;
    }
    
    /// <summary>
    /// Get current crosshair scale for debugging
    /// </summary>
    public float GetCurrentCrosshairScale()
    {
        return currentCrosshairScale;
    }
    
    #endregion
}