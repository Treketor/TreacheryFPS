using UnityEngine;

/// <summary>
/// Handles procedural camera recoil when weapon fires.
/// Works additively with FirstPersonLook script.
/// Add this to the PLAYER GameObject (same object as FirstPersonLook).
/// </summary>
public class WeaponRecoil : MonoBehaviour
{
    [Header("Recoil Settings")]
    [SerializeField] float recoilAmountX = 2f; // Vertical recoil (pitch up)
    [SerializeField] float recoilAmountY = 0.5f; // Horizontal recoil (random left/right)
    [SerializeField] float recoilAmountZ = 0.1f; // Roll recoil
    
    [Header("Recoil Timing")]
    [SerializeField] float recoilSpeed = 10f; // How fast recoil kicks
    [SerializeField] float returnSpeed = 5f; // How fast camera returns to center
    
    [Header("Randomness")]
    [SerializeField] float randomnessAmount = 0.5f; // How much variation in recoil pattern
    
    [Header("Snappiness")]
    [SerializeField] bool useSnappyRecoil = true; // Instant kick vs smooth
    [SerializeField] float snapAmount = 0.8f; // 0-1, how much of recoil is instant

    Vector3 _currentRotation;
    Vector3 _targetRotation;

    // Public properties that FirstPersonLook can read
    public Vector3 RecoilRotation => _currentRotation;

    void Update()
    {
        // Smoothly interpolate current rotation towards target
        _currentRotation = Vector3.Lerp(_currentRotation, _targetRotation, recoilSpeed * Time.deltaTime);
        
        // Return target rotation to zero
        _targetRotation = Vector3.Lerp(_targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Trigger recoil effect. Call this when weapon fires.
    /// </summary>
    /// <param name="multiplier">Scale the recoil amount (1.0 = normal, 2.0 = double, etc.)</param>
    public void ApplyRecoil(float multiplier = 1f)
    {
        // Calculate recoil with randomness
        float recoilX = recoilAmountX * multiplier;
        float recoilY = Random.Range(-recoilAmountY, recoilAmountY) * multiplier;
        float recoilZ = Random.Range(-recoilAmountZ, recoilAmountZ) * multiplier;
        
        // Apply randomness variation
        recoilX *= Random.Range(1f - randomnessAmount, 1f + randomnessAmount);
        
        Vector3 recoil = new Vector3(-recoilX, recoilY, recoilZ);
        
        if (useSnappyRecoil)
        {
            // Apply part of recoil instantly for snappy feel
            _currentRotation += recoil * snapAmount;
            _targetRotation += recoil * (1f - snapAmount);
        }
        else
        {
            // Smooth recoil
            _targetRotation += recoil;
        }
    }

    /// <summary>
    /// Reset recoil immediately (useful when switching weapons or aiming)
    /// </summary>
    public void ResetRecoil()
    {
        _currentRotation = Vector3.zero;
        _targetRotation = Vector3.zero;
    }

    /// <summary>
    /// Set recoil multiplier based on weapon stats (fire rate, tier, etc.)
    /// </summary>
    public void SetRecoilMultipliers(float vertical, float horizontal, float roll)
    {
        recoilAmountX = vertical;
        recoilAmountY = horizontal;
        recoilAmountZ = roll;
    }
}
