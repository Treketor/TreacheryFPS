using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float movementMultiplier = 1f;
    public float jumpForce = 5f;
    public float gravity = -9.81f;

    private Vector3 horizontalVelocity = Vector3.zero;
    private float verticalVelocity = 0f;
    private CharacterController controller;

    [Header("Input")]
    [SerializeField] private InputActionAsset playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        moveAction = playerInput.FindAction("Move");
        moveAction.Enable();

        jumpAction = playerInput.FindAction("Jump");
        jumpAction.Enable();
    }

    void Update()
    {
        float inputX = moveAction.ReadValue<Vector2>().x;
        float inputZ = moveAction.ReadValue<Vector2>().y;

        Vector3 inputDir = new(inputX, 0f, inputZ);
        if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();
        horizontalVelocity = inputDir;

        // Handle jumping
        if (jumpAction.WasPressedThisFrame() && controller.isGrounded)
        {
            verticalVelocity = jumpForce;
        }

        // Apply gravity
        if (!controller.isGrounded)
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
        else if (verticalVelocity < 0)
        {
            verticalVelocity = -2f; // Small downward force to keep grounded
        }

        // Combine velocities
        Vector3 motion = movementMultiplier * moveSpeed * horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(motion * Time.deltaTime);
    }
}
