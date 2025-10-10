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

    private float rotationY = 0f;

    void Start()
    {
        lookAction = playerInput.FindAction("Look");
        lookAction.Enable();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        // Apply sensitivity and frame rate independence
        float deltaX = lookInput.x * sensitivity * Time.deltaTime;
        float deltaY = lookInput.y * sensitivity * Time.deltaTime;

        // Horizontal rotation (player body)
        transform.Rotate(Vector3.up, deltaX);

        // Vertical rotation (camera)
        rotationY -= deltaY;
        rotationY = Mathf.Clamp(rotationY, -maxYAngle, maxYAngle);

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(rotationY, 0f, 0f);
        }
    }
}