using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float movementMultiplier = 1f;
    public float groundAcceleration = 25f;
    public float airAcceleration = 8f;
    public float maxAirSpeed = 4f;
    public float minSprintSpeed = 2f;
    public float jumpHeight = 1.5f;
    public float gravity = -20f;

    [Header("Crouch Settings")]
    public float crouchHeight = 1f;
    public float crouchSpeed = 2.5f;
    public float crouchTransitionTime = 0.2f;
    public bool crouchToggle = true;
    public Transform cameraTransform;
    public float cameraHeightOffset = 0.2f;

    private Vector3 currentHorizontalVelocity = Vector3.zero;
    private float verticalVelocity = 0f;
    private bool isSprinting = false;
    private bool isCrouching = false;
    private float standingHeight;
    private Vector3 standingCenter;
    private float currentHeight;
    private float targetHeight;
    private CharacterController controller;

    [Header("Input")]
    [SerializeField] private InputActionAsset playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction crouchAction;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        standingHeight = controller.height;
        standingCenter = controller.center;
        currentHeight = standingHeight;
        targetHeight = standingHeight;
        SetInput();
        UpdateCameraPosition();
    }

    void Update()
    {
        float inputX = moveAction.ReadValue<Vector2>().x;
        float inputZ = moveAction.ReadValue<Vector2>().y;

        Vector3 inputDir = transform.TransformDirection(new Vector3(inputX, 0f, inputZ));
        if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

        HandleCrouch();
        HandleSprint(inputZ);
        UpdateHorizontalVelocity(inputDir);
        HandleJump();
        ApplyGravity();
        MoveCharacter();
    }

    private void SetInput()
    {
        moveAction = playerInput.FindAction("Move");
        moveAction.Enable();

        jumpAction = playerInput.FindAction("Jump");
        jumpAction.Enable();

        sprintAction = playerInput.FindAction("Sprint");
        sprintAction.Enable();

        crouchAction = playerInput.FindAction("Crouch");
        crouchAction.Enable();
    }

    private void HandleCrouch()
    {
        if (controller.isGrounded)
        {
            if (crouchToggle)
            {
                // Toggle crouch
                if (crouchAction.WasPressedThisFrame())
                {
                    isCrouching = !isCrouching;
                }
            }
            else
            {
                // Hold to crouch
                isCrouching = crouchAction.IsPressed();
            }
        }

        // Smoothly interpolate height
        targetHeight = isCrouching ? crouchHeight : standingHeight;
        if (Mathf.Abs(currentHeight - targetHeight) > 0.01f)
        {
            float transitionSpeed = Mathf.Abs(standingHeight - crouchHeight) / crouchTransitionTime;
            currentHeight = Mathf.MoveTowards(currentHeight, targetHeight, transitionSpeed * Time.deltaTime);
            
            float heightDifference = standingHeight - currentHeight;
            controller.height = currentHeight;
            controller.center = standingCenter - Vector3.up * (heightDifference / 2f);
        }

        UpdateCameraPosition();
    }

    private void HandleSprint(float inputZ)
    {
        // Cancel sprint when crouching
        if (isCrouching)
        {
            isSprinting = false;
            return;
        }

        // Handle sprint toggle - only allow sprinting when moving primarily forward
        if (sprintAction.WasPressedThisFrame() && controller.isGrounded && inputZ > 0.5f)
        {
            isSprinting = true;
        }

        // Cancel sprint if not moving forward, no input, or velocity too low
        if (isSprinting && controller.isGrounded && (inputZ <= 0.5f || currentHorizontalVelocity.magnitude < minSprintSpeed))
        {
            isSprinting = false;
        }
    }

    private void UpdateHorizontalVelocity(Vector3 inputDir)
    {
        // Update horizontal velocity: accelerate on ground, maintain momentum in air
        if (controller.isGrounded)
        {
            float currentSpeed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : moveSpeed);
            Vector3 targetVelocity = movementMultiplier * currentSpeed * inputDir;
            currentHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, targetVelocity, groundAcceleration * Time.deltaTime);
        }
        else
        {
            // Maintain sprint momentum in air, but allow limited directional adjustments
            if (!isSprinting)
            {
                currentHorizontalVelocity += airAcceleration * Time.deltaTime * inputDir;
                if (currentHorizontalVelocity.magnitude > maxAirSpeed)
                {
                    currentHorizontalVelocity = currentHorizontalVelocity.normalized * maxAirSpeed;
                }
            }
            else
            {
                // Sprinting in air: maintain velocity with minimal air control
                currentHorizontalVelocity += airAcceleration * 0.3f * Time.deltaTime * inputDir;
            }
        }
    }

    private void HandleJump()
    {
        // Handle jumping - if crouching, uncrouch instead of jumping
        if (jumpAction.WasPressedThisFrame() && controller.isGrounded)
        {
            if (isCrouching)
            {
                isCrouching = false;
            }
            else
            {
                verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight); // Calculate initial jump velocity
            }
        }
    }

    private void ApplyGravity()
    {
        // Apply gravity
        if (!controller.isGrounded)
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
        else if (verticalVelocity < 0f)
        {
            verticalVelocity = -2f; // Small downward force to keep grounded
        }
    }

    private void MoveCharacter()
    {
        // Combine velocities and apply slope compensation
        Vector3 motion = currentHorizontalVelocity + Vector3.up * verticalVelocity;

        // Apply additional downward force on slopes to prevent slow movement
        if (controller.isGrounded && motion.y < 0)
        {
            motion.y = -8f;
        }

        controller.Move(motion * Time.deltaTime);
    }

    private void UpdateCameraPosition()
    {
        // Smoothly update camera position based on controller height
        if (cameraTransform != null)
        {
            float targetCameraHeight = currentHeight - cameraHeightOffset;
            float currentCameraY = cameraTransform.localPosition.y;
            
            if (Mathf.Abs(currentCameraY - targetCameraHeight) > 0.01f)
            {
                float transitionSpeed = Mathf.Abs(standingHeight - crouchHeight) / crouchTransitionTime;
                float newCameraY = Mathf.MoveTowards(currentCameraY, targetCameraHeight, transitionSpeed * Time.deltaTime);
                cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, newCameraY, cameraTransform.localPosition.z);
            }
        }
    }
}
