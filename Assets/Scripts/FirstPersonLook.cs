using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonLook : MonoBehaviour
{
    [Header("Look Settings")]
    public float sensitivity = 2f;
    public Transform cameraTransform;
    private float maxYAngle = 90f;

    [Header("Input")]
    [SerializeField] private InputActionAsset playerInput;
    private InputAction lookAction;

    [Header("Recoil")]
    [SerializeField] private WeaponRecoil weaponRecoil;
    [SerializeField] private bool autoFindRecoil = true;

    private float rotationY = 0f;

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
    }

    void Update()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        // Apply sensitivity (mouse delta is already frame-based)
        float deltaX = lookInput.x * sensitivity;
        float deltaY = lookInput.y * sensitivity;

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