using UnityEngine;
using UnityEngine.InputSystem;
using Treachery.Weapons.Interfaces;
using Treachery.Weapons.Runtime;

/// <summary>
/// Weapon sway system that responds to camera movement for dynamic weapon positioning
/// Attaches to weapon anchor (parent of weapons) and disables during ADS
/// </summary>
public class WeaponSway : MonoBehaviour
{
    [Header("Sway Settings")]
    [Tooltip("How much the weapon sways based on mouse movement")]
    [SerializeField] float swayAmount = 0.02f;

    [Tooltip("Maximum sway distance in any direction")]
    [SerializeField] float maxSwayAmount = 0.06f;

    [Tooltip("How fast the weapon follows mouse movement")]
    [SerializeField] float swaySpeed = 6f;

    [Tooltip("How fast the weapon returns to center when mouse stops moving")]
    [SerializeField] float returnSpeed = 8f;
    
    [Header("Sway Axes")]
    [Tooltip("Enable left/right sway based on mouse X movement")]
    [SerializeField] bool enableHorizontalSway = true;

    [Tooltip("Enable up/down sway based on mouse Y movement")]
    [SerializeField] bool enableVerticalSway = true;
    
    [Header("ADS Integration")]
    [Tooltip("Disable all sway when aiming down sights")]
    [SerializeField] bool disableSwayDuringADS = true;

    [Tooltip("How fast sway transitions when entering/exiting ADS")]
    [SerializeField] float adsTransitionSpeed = 10f;

    [Header("Input")]
    [Tooltip("Optional: assign the PlayerInput component to source actions from.")]
    [SerializeField] PlayerInput playerInputComponent;
    [Tooltip("Auto-find a PlayerInput component if not assigned.")]
    [SerializeField] bool autoFindPlayerInput = true;
    [Tooltip("Optional: assign the InputActionAsset directly if you don't use PlayerInput.")]
    [SerializeField] InputActionAsset playerInput;
    [Tooltip("Name of the look delta action (expected: 'Look').")]
    [SerializeField] string lookActionName = "Look";
    
    // Private variables
    private Vector3 initialPosition;
    private Vector3 targetSwayPosition;
    private Vector3 currentSwayPosition;
    private Vector2 mouseInput;

    InputAction _lookAction;
    
    // Component references
    [SerializeField] MonoBehaviour weaponController;
    [SerializeField] bool autoFindWeaponController = true;

    IWeaponController _weaponController;
    
    // ADS state
    private bool isAiming;
    private float adsSwayMultiplier = 1f;

    bool _hasExternalAiming;
    bool _externalIsAiming;
    
    void Start()
    {
        // Store initial position
        initialPosition = transform.localPosition;

        if (autoFindWeaponController && weaponController == null)
        {
            var v2 = GetComponentInParent<WeaponControllerV2>();
            if (v2 != null)
                weaponController = v2;
            else
                weaponController = GetComponentInParent<WeaponController>();
        }

        _weaponController = weaponController as IWeaponController;
        
        // Initialize positions
        currentSwayPosition = Vector3.zero;
        targetSwayPosition = Vector3.zero;

        // Initialize Input System action
        if (autoFindPlayerInput && playerInputComponent == null)
        {
            playerInputComponent = GetComponentInParent<PlayerInput>();
            if (playerInputComponent == null)
                playerInputComponent = FindFirstObjectByType<PlayerInput>();
        }

        var actions = playerInputComponent != null ? playerInputComponent.actions : playerInput;
        if (actions != null)
        {
            _lookAction = actions.FindAction(lookActionName);
            if (_lookAction == null)
                Debug.LogWarning($"WeaponSway: Look action '{lookActionName}' not found in InputActionAsset.");
        }
        else
        {
            Debug.LogWarning("WeaponSway: No PlayerInput or InputActionAsset assigned; sway input will be zero.");
        }
    }

    void OnEnable()
    {
        _lookAction?.Enable();
    }

    void OnDisable()
    {
        _lookAction?.Disable();
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
        if (_hasExternalAiming)
        {
            isAiming = _externalIsAiming;
        }
        else
        {
            isAiming = false;
            
            // Check if any weapon is currently aiming
            if (_weaponController != null && _weaponController.CurrentWeapon != null)
            {
                isAiming = _weaponController.CurrentWeapon.IsAiming;
            }
        }
        
        // Update ADS sway multiplier
        float targetMultiplier = (disableSwayDuringADS && isAiming) ? 0f : 1f;
        adsSwayMultiplier = Mathf.Lerp(adsSwayMultiplier, targetMultiplier, adsTransitionSpeed * Time.deltaTime);
    }

    public void SetExternalIsAiming(bool aiming)
    {
        _hasExternalAiming = true;
        _externalIsAiming = aiming;
    }
    

    
    void CalculateMouseInput()
    {
        // Get look delta using Input System action
        if (_lookAction != null)
            mouseInput = _lookAction.ReadValue<Vector2>() * Time.deltaTime;
        else
            mouseInput = Vector2.zero;
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