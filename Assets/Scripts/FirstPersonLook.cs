using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonLook : MonoBehaviour
{
    [Header("Look Settings")]
    public float sensitivity = 2f;
    public Transform cameraTransform;
    private float maxYAngle = 90f;
    
    [Header("ADS Sensitivity")]
    [SerializeField] float adsSensitivityMultiplier = 0.6f;
    [Tooltip("Mouse sensitivity multiplier when aiming down sights")]

    [Header("Input")]
    [SerializeField] private InputActionAsset playerInput;
    private InputAction lookAction;

    [Header("Recoil")]
    [SerializeField] private WeaponRecoil weaponRecoil;
    [SerializeField] private bool autoFindRecoil = true;

    private float rotationY = 0f;
    private WeaponController weaponController;

    void Start()
    {
        lookAction = playerInput.FindAction("Look");
        lookAction.Enable();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Auto-find weapon recoil component
        if (!weaponRecoil && autoFindRecoil)
        {
            weaponRecoil = FindFirstObjectByType<WeaponRecoil>();
        }
        
        // Find weapon controller for ADS sensitivity
        weaponController = GetComponent<WeaponController>();
        if (weaponController == null)
        {
            Debug.LogWarning("FirstPersonLook: WeaponController not found on same GameObject!");
        }
    }

    void Update()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        // Check if player is aiming down sights
        bool isAiming = weaponController != null && weaponController.CurrentWeapon != null && weaponController.CurrentWeapon.IsAiming;
        
        // Apply sensitivity with ADS scaling
        float currentSensitivity = isAiming ? sensitivity * adsSensitivityMultiplier : sensitivity;
        float deltaX = lookInput.x * currentSensitivity;
        float deltaY = lookInput.y * currentSensitivity;

        // Horizontal rotation (player body)
        transform.Rotate(Vector3.up, deltaX);

        // Vertical rotation (camera)
        rotationY -= deltaY;
        rotationY = Mathf.Clamp(rotationY, -maxYAngle, maxYAngle);

        if (cameraTransform != null)
        {
            // Get recoil offset
            Vector3 recoilOffset = weaponRecoil != null ? weaponRecoil.RecoilRotation : Vector3.zero;
            
            // Apply look rotation + recoil
            // Recoil X is negative (kicks up), so add it to rotationY
            // Recoil Y is horizontal offset
            // Recoil Z is roll
            cameraTransform.localRotation = Quaternion.Euler(
                rotationY + recoilOffset.x,  // Pitch (vertical look + recoil)
                recoilOffset.y,               // Yaw (recoil only, player body handles look)
                recoilOffset.z                // Roll (recoil only)
            );
        }
    }
}