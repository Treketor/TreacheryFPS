using UnityEngine;
using UnityEngine.InputSystem;
using Treachery.Weapons.Interfaces;
using Treachery.Weapons.Runtime;

/// <summary>
/// Interactable machine that upgrades player's current weapon for souls.
/// Similar to COD Zombies "Pack-a-Punch" machine.
/// </summary>
public class WeaponUpgradeMachine : MonoBehaviour
{
    [Header("Interaction")]
    [Tooltip("Distance player must be within to interact")]
    [SerializeField] float interactionRange = 3f;
    
    [Header("References")]
    [Tooltip("The player transform")]
    [SerializeField] Transform player;
    [Tooltip("Reference to WeaponController to get current weapon")]
    [SerializeField] MonoBehaviour weaponController;
    [Tooltip("Input Action Asset containing Interact action")]
    [SerializeField] InputActionAsset playerInput;

    [Header("Visual Feedback")]
    [Tooltip("Optional glow/highlight object to enable when in range")]
    [SerializeField] GameObject highlightObject;
    [Tooltip("Optional particle effect to play on successful upgrade")]
    [SerializeField] ParticleSystem upgradeEffect;

    [Header("Audio")]
    [Tooltip("Sound to play on successful upgrade")]
    [SerializeField] AudioClip upgradeSound;
    [Tooltip("Sound to play when can't afford")]
    [SerializeField] AudioClip deniedSound;

    bool _playerInRange = false;
    AudioSource _audioSource;
    InputAction _interactAction;

    public System.Action<IUpgradeableWeapon, WeaponTier> OnWeaponUpgraded;

    IWeaponController _weaponController;

    void Start()
    {
        // Auto-find player if not assigned
        if (!player)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj) player = playerObj.transform;
        }

        // Auto-find weapon controller if not assigned
        if (!weaponController && player)
        {
            var v2 = player.GetComponentInChildren<WeaponControllerV2>();
            if (v2 != null)
                weaponController = v2;
            else
                weaponController = player.GetComponentInChildren<WeaponController>();
        }

        _weaponController = weaponController as IWeaponController;

        // Setup input action
        if (playerInput != null)
        {
            _interactAction = playerInput.FindAction("Interact");
            if (_interactAction == null)
            {
                Debug.LogWarning("WeaponUpgradeMachine: 'Interact' action not found in Input Action Asset. Please create one.");
            }
        }
        else
        {
            Debug.LogWarning("WeaponUpgradeMachine: No Input Action Asset assigned. Please assign the player's input actions.");
        }

        // Setup audio source
        _audioSource = GetComponent<AudioSource>();
        if (!_audioSource && (upgradeSound || deniedSound))
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0.5f; // Semi-3D sound
        }

        if (highlightObject)
            highlightObject.SetActive(false);
    }

    void Update()
    {
        if (!player) return;

        // Check if player is in range
        float distance = Vector3.Distance(transform.position, player.position);
        _playerInRange = distance <= interactionRange;

        // Update highlight
        if (highlightObject)
            highlightObject.SetActive(_playerInRange);

        // Handle interaction input
        if (_playerInRange && _interactAction != null && _interactAction.WasPressedThisFrame())
        {
            TryUpgradeWeapon();
        }
    }

    void TryUpgradeWeapon()
    {
        // Validate setup
        if (_weaponController == null)
        {
            Debug.LogWarning("WeaponUpgradeMachine: No WeaponController assigned!");
            return;
        }

        if (SoulManager.Instance == null)
        {
            Debug.LogWarning("WeaponUpgradeMachine: SoulManager not found!");
            return;
        }

        // Get current weapon
        IUpgradeableWeapon currentWeapon = _weaponController.CurrentWeapon as IUpgradeableWeapon;
        if (currentWeapon == null)
        {
            Debug.Log("No weapon equipped!");
            PlaySound(deniedSound);
            return;
        }

        // Check if weapon can be upgraded
        if (!currentWeapon.CanUpgrade())
        {
            Debug.Log($"{currentWeapon.DisplayName} is already at maximum tier (Legendary)!");
            PlaySound(deniedSound);
            return;
        }

        // Get upgrade cost
        int cost = currentWeapon.GetUpgradeCost();

        // Check if player can afford it
        if (!SoulManager.Instance.CanAfford(cost))
        {
            Debug.Log($"Not enough souls! Need {cost}, have {SoulManager.Instance.CurrentSouls}");
            PlaySound(deniedSound);
            return;
        }

        // Spend souls
        if (SoulManager.Instance.TrySpendSouls(cost))
        {
            // Upgrade the weapon
            if (currentWeapon.TryUpgradeTier())
            {
                Debug.Log($"Upgraded {currentWeapon.DisplayName} to {currentWeapon.TierName}!");
                
                // Visual/Audio feedback
                PlaySound(upgradeSound);
                if (upgradeEffect)
                    upgradeEffect.Play();

                // Fire event
                OnWeaponUpgraded?.Invoke(currentWeapon, currentWeapon.CurrentTier);
            }
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (_audioSource && clip)
        {
            _audioSource.PlayOneShot(clip);
        }
    }

    public bool IsPlayerInRange()
    {
        return _playerInRange;
    }

    public WeaponInstance_Hitscan GetCurrentWeapon()
    {
        if (weaponController == null) return null;
        return (weaponController as WeaponController)?.CurrentWeapon as WeaponInstance_Hitscan;
    }

    public IUpgradeableWeapon GetCurrentUpgradeableWeapon()
    {
        if (_weaponController == null) return null;
        return _weaponController.CurrentWeapon as IUpgradeableWeapon;
    }

    public int GetCurrentUpgradeCost()
    {
        var weapon = GetCurrentUpgradeableWeapon();
        if (weapon == null) return 0;
        return weapon.GetUpgradeCost();
    }

    void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
