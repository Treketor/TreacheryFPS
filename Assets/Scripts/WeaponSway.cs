using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Weapon sway system that responds to camera movement for dynamic weapon positioning
/// Attaches to weapon anchor (parent of weapons) and disables during ADS
/// </summary>
public class WeaponSway : MonoBehaviour
{
    [Header("Sway Settings")]
    [SerializeField] float swayAmount = 0.02f;
    [Tooltip("How much the weapon sways based on mouse movement")]
    
    [SerializeField] float maxSwayAmount = 0.06f;
    [Tooltip("Maximum sway distance in any direction")]
    
    [SerializeField] float swaySpeed = 6f;
    [Tooltip("How fast the weapon follows mouse movement")]
    
    [SerializeField] float returnSpeed = 8f;
    [Tooltip("How fast the weapon returns to center when mouse stops moving")]
    
    [Header("Sway Axes")]
    [SerializeField] bool enableHorizontalSway = true;
    [Tooltip("Enable left/right sway based on mouse X movement")]
    
    [SerializeField] bool enableVerticalSway = true;
    [Tooltip("Enable up/down sway based on mouse Y movement")]
    
    [Header("ADS Integration")]
    [SerializeField] bool disableSwayDuringADS = true;
    [Tooltip("Disable all sway when aiming down sights")]
    
    [SerializeField] float adsTransitionSpeed = 10f;
    [Tooltip("How fast sway transitions when entering/exiting ADS")]
    
    // Private variables
    private Vector3 initialPosition;
    private Vector3 targetSwayPosition;
    private Vector3 currentSwayPosition;
    private Vector2 mouseInput;
    
    // Input System
    private Mouse mouse;
    
    // Component references
    private WeaponController weaponController;
    
    // ADS state
    private bool isAiming;
    private float adsSwayMultiplier = 1f;
    
    void Start()
    {
        // Store initial position
        initialPosition = transform.localPosition;
        
        // Find required components
        weaponController = FindFirstObjectByType<WeaponController>();
        
        if (weaponController == null)
        {
            Debug.LogWarning("WeaponSway: WeaponController not found! ADS detection will not work.");
        }
        
        // Initialize positions
        currentSwayPosition = Vector3.zero;
        targetSwayPosition = Vector3.zero;
        
        // Initialize Input System
        mouse = Mouse.current;
    }
    
    void Update()
    {
        // Update ADS state
        UpdateADSState();
        
        // Calculate mouse input
        CalculateMouseInput();
        
        // Calculate target sway position
        CalculateTargetSwayPosition();
        
        // Apply sway to weapon anchor
        ApplySway();
    }
    
    void UpdateADSState()
    {
        isAiming = false;
        
        // Check if any weapon is currently aiming
        if (weaponController != null && weaponController.CurrentWeapon != null)
        {
            isAiming = weaponController.CurrentWeapon.IsAiming;
        }
        
        // Update ADS sway multiplier
        float targetMultiplier = (disableSwayDuringADS && isAiming) ? 0f : 1f;
        adsSwayMultiplier = Mathf.Lerp(adsSwayMultiplier, targetMultiplier, adsTransitionSpeed * Time.deltaTime);
    }
    

    
    void CalculateMouseInput()
    {
        // Get mouse delta using new Input System
        if (mouse != null)
        {
            mouseInput = mouse.delta.ReadValue() * Time.deltaTime;
        }
        else
        {
            mouseInput = Vector2.zero;
        }
    }
    
    void CalculateTargetSwayPosition()
    {
        Vector3 swayPosition = Vector3.zero;
        
        // Mouse-based sway
        if (enableHorizontalSway)
        {
            swayPosition.x = -mouseInput.x * swayAmount;
        }
        
        if (enableVerticalSway)
        {
            swayPosition.y = -mouseInput.y * swayAmount;
        }
        

        
        // Apply ADS multiplier
        swayPosition *= adsSwayMultiplier;
        
        // Clamp to maximum sway amount
        swayPosition.x = Mathf.Clamp(swayPosition.x, -maxSwayAmount, maxSwayAmount);
        swayPosition.y = Mathf.Clamp(swayPosition.y, -maxSwayAmount, maxSwayAmount);
        swayPosition.z = Mathf.Clamp(swayPosition.z, -maxSwayAmount * 0.5f, maxSwayAmount * 0.5f);
        
        targetSwayPosition = swayPosition;
    }
    
    void ApplySway()
    {
        // Determine interpolation speed based on mouse input
        float currentSpeed = (mouseInput.magnitude > 0.01f) ? swaySpeed : returnSpeed;
        
        // Smoothly interpolate to target position
        currentSwayPosition = Vector3.Lerp(currentSwayPosition, targetSwayPosition, currentSpeed * Time.deltaTime);
        
        // Apply final position
        transform.localPosition = initialPosition + currentSwayPosition;
    }
    
    /// <summary>
    /// Manually set sway settings at runtime
    /// </summary>
    public void SetSwaySettings(float amount, float maxAmount, float speed, float returnSpd)
    {
        swayAmount = amount;
        maxSwayAmount = maxAmount;
        swaySpeed = speed;
        returnSpeed = returnSpd;
    }
    
    /// <summary>
    /// Enable or disable sway completely
    /// </summary>
    public void SetSwayEnabled(bool enabled)
    {
        this.enabled = enabled;
        
        if (!enabled)
        {
            // Return to initial position immediately
            transform.localPosition = initialPosition;
        }
    }
    
    /// <summary>
    /// Get current sway offset for debugging
    /// </summary>
    public Vector3 GetCurrentSwayOffset()
    {
        return currentSwayPosition;
    }
    
    /// <summary>
    /// Reset weapon to center position
    /// </summary>
    public void ResetToCenter()
    {
        targetSwayPosition = Vector3.zero;
        currentSwayPosition = Vector3.zero;
        transform.localPosition = initialPosition;
    }
    

}