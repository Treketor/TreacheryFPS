using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float sprintSpeed = 10f;
    [SerializeField] float movementMultiplier = 1f;
    [SerializeField] float groundAcceleration = 25f;
    
    [Header("ADS Movement")]
    [SerializeField] float adsSpeedMultiplier = 0.4f;
    [Tooltip("Universal speed multiplier when aiming down sights")]
    [SerializeField] float airAcceleration = 8f;
    [SerializeField] float maxAirSpeed = 4f;
    [SerializeField] float minSprintSpeed = 2f;
    [SerializeField] float jumpHeight = 1.5f;
    [SerializeField] float gravity = -30f;

    [Header("Crouch Settings")]
    [SerializeField] float crouchHeight =1.251f;
    [SerializeField] float crouchSpeed = 2.5f;
    [SerializeField] float crouchTransitionTime = 0.075f;
    [SerializeField] bool crouchToggle = true;
    [SerializeField] Transform cameraTransform;
    [SerializeField] float cameraHeightOffset = 0.2f;

    private Vector3 currentHorizontalVelocity = Vector3.zero;
    private float verticalVelocity = 0f;
    private bool isSprinting = false;
    private bool isCrouching = false;
    private float standingHeight;
    private Vector3 standingCenter;
    private float currentHeight;
    private float targetHeight;
    private CharacterController controller;
    private WeaponController weaponController;

    [Header("Input")]
    [SerializeField] InputActionAsset playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction crouchAction;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        weaponController = GetComponent<WeaponController>();
        if (weaponController == null)
        {
            Debug.LogWarning("PlayerMovement: WeaponController not found on same GameObject!");
        }
        
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
                // Toggle crouch - check if there's space to stand
                if (crouchAction.WasPressedThisFrame())
                {
                    if (isCrouching && !CanStandUp())
                    {
                        // Can't stand up, blocked by ceiling
                        return;
                    }
                    isCrouching = !isCrouching;
                }
            }
            else
            {
                // Hold to crouch - check if there's space to stand when releasing
                bool wantsToCrouch = crouchAction.IsPressed();
                if (isCrouching && !wantsToCrouch && !CanStandUp())
                {
                    // Force crouch if blocked by ceiling
                    isCrouching = true;
                }
                else
                {
                    isCrouching = wantsToCrouch;
                }
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

    private bool CanStandUp()
    {
        // Check if there's enough space above the player to stand up
        float checkHeight = standingHeight - crouchHeight;
        Vector3 checkStart = transform.position + Vector3.up * (crouchHeight / 2f);
        float checkRadius = controller.radius * 0.9f;
        
        return !Physics.SphereCast(checkStart, checkRadius, Vector3.up, out _, checkHeight);
    }

    private void HandleSprint(float inputZ)
    {
        // Check if player is aiming down sights
        bool isAiming = weaponController != null && weaponController.CurrentWeapon != null && weaponController.CurrentWeapon.IsAiming;
        
        // Cancel sprint when crouching or aiming
        if (isCrouching || isAiming)
        {
            isSprinting = false;
            return;
        }

        // Handle sprint toggle - only allow sprinting when moving primarily forward and not aiming
        if (sprintAction.WasPressedThisFrame() && controller.isGrounded && inputZ > 0.5f && !isAiming)
        {
            isSprinting = true;
        }

        // Cancel sprint if not moving forward, no input, velocity too low, or started aiming
        if (isSprinting && controller.isGrounded && (inputZ <= 0.5f || currentHorizontalVelocity.magnitude < minSprintSpeed || isAiming))
        {
            isSprinting = false;
        }
    }

    private void UpdateHorizontalVelocity(Vector3 inputDir)
    {
        // Update horizontal velocity: accelerate on ground, maintain momentum in air
        if (controller.isGrounded)
        {
            // Check if player is aiming down sights
            bool isAiming = weaponController != null && weaponController.CurrentWeapon != null && weaponController.CurrentWeapon.IsAiming;
            
            float currentSpeed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : moveSpeed);
            
            // Apply ADS speed reduction
            if (isAiming)
            {
                currentSpeed *= adsSpeedMultiplier;
            }
            
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
