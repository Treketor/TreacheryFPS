using System.Collections;
using UnityEngine;

/// <summary>
/// Traditional magazine-based reload behavior for most weapons.
/// Reloads the entire magazine at once after a fixed duration.
/// </summary>
[System.Serializable]
public class MagazineReloadBehavior : IWeaponReloadBehavior
{
    [Header("Magazine Reload Settings")]
    [SerializeField] private float reloadDuration = 2.0f;
    [Tooltip("Total time to complete the reload")]
    
    [SerializeField] private float baseAnimationDuration = 2.0f;
    [Tooltip("Base duration of the reload animation at normal speed")]
    
    [SerializeField] private float reloadDelayBuffer = 0.5f;
    [Tooltip("Extra time after animation completes before weapon is ready")]
    
    [Header("Animation")]
    [SerializeField] private string reloadTriggerName = "Reload";

    // Runtime state
    private bool _isReloading = false;
    private Coroutine _reloadCoroutine = null;
    private MonoBehaviour _coroutineRunner = null;
    private Animator _animator = null;
    
    // Reload data
    private System.Action<int> _onAmmoAdded;
    private System.Action _onReloadComplete;
    private System.Action _onReloadCancelled;

    public bool IsReloading => _isReloading;

    /// <summary>
    /// Initialize the reload behavior with required components
    /// </summary>
    public void Initialize(MonoBehaviour coroutineRunner, Animator animator)
    {
        _coroutineRunner = coroutineRunner;
        _animator = animator;
    }

    /// <summary>
    /// Set the reload duration (used by weapon stats)
    /// </summary>
    public void SetReloadDuration(float duration)
    {
        reloadDuration = duration;
    }

    public bool CanReload(int currentAmmo, int maxAmmo, int reserveAmmo)
    {
        return !_isReloading && currentAmmo < maxAmmo && reserveAmmo > 0;
    }

    public void StartReload(int currentAmmo, int maxAmmo, int reserveAmmo, System.Action<int> onAmmoAdded, System.Action onReloadComplete, System.Action onReloadCancelled)
    {
        if (_isReloading || _coroutineRunner == null)
            return;

        _onAmmoAdded = onAmmoAdded;
        _onReloadComplete = onReloadComplete;
        _onReloadCancelled = onReloadCancelled;

        _isReloading = true;
        _reloadCoroutine = _coroutineRunner.StartCoroutine(MagazineReloadCoroutine(currentAmmo, maxAmmo, reserveAmmo));
    }

    public void CancelReload()
    {
        if (!_isReloading)
            return;

        StopReload(true);
    }

    public void Update()
    {
        // This reload behavior is coroutine-based, no update needed
    }

    private void StopReload(bool wasCancelled)
    {
        if (_reloadCoroutine != null && _coroutineRunner != null)
        {
            _coroutineRunner.StopCoroutine(_reloadCoroutine);
            _reloadCoroutine = null;
        }

        _isReloading = false;

        // Reset animator speed if it was changed
        if (_animator != null)
        {
            _animator.speed = 1.0f;
        }

        if (wasCancelled)
        {
            _onReloadCancelled?.Invoke();
            Debug.Log("Magazine reload cancelled");
        }
        else
        {
            _onReloadComplete?.Invoke();
            Debug.Log("Magazine reload completed");
        }
    }

    private IEnumerator MagazineReloadCoroutine(int currentAmmo, int maxAmmo, int reserveAmmo)
    {
        // Calculate animation speed based on reload time
        float targetAnimationDuration = Mathf.Max(0.1f, reloadDuration - reloadDelayBuffer);
        float animationSpeed = baseAnimationDuration / targetAnimationDuration;
        
        // Apply animation speed and trigger
        if (_animator != null)
        {
            _animator.speed = animationSpeed;
            
            if (!string.IsNullOrEmpty(reloadTriggerName))
            {
                _animator.SetTrigger(reloadTriggerName);
            }
        }
        
        // Wait for reload duration
        yield return new WaitForSeconds(reloadDuration);

        // Calculate how much ammo to add
        int needed = maxAmmo - currentAmmo;
        int toAdd = Mathf.Min(needed, reserveAmmo);
        
        // Add all the ammo at once
        if (toAdd > 0)
        {
            _onAmmoAdded?.Invoke(toAdd);
        }

        // Reload complete
        StopReload(false);
    }
}