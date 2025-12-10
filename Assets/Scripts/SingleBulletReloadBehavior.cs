using System.Collections;
using UnityEngine;

/// <summary>
/// Single-bullet reload behavior for pump shotguns and similar weapons.
/// Handles the three-phase reload: Start Reload -> Reload Single Bullet (looped) -> Finish Reload
/// Can be interrupted at any time to allow shooting with partial ammo.
/// </summary>
[System.Serializable]
public class SingleBulletReloadBehavior : IWeaponReloadBehavior
{
    [Header("Single Bullet Reload Settings")]
    [SerializeField] private float startReloadDuration = 0.8f;
    [Tooltip("Time for the 'Start Reload' animation")]
    
    [SerializeField] private float singleBulletReloadDuration = 0.6f;
    [Tooltip("Time for each 'Reload Single Bullet' animation")]
    
    [SerializeField] private float finishReloadDuration = 0.5f;
    [Tooltip("Time for the 'Finish Reload' animation (cocking)")]
    
    [Header("Animation Triggers")]
    [SerializeField] private string startReloadTrigger = "StartReload";
    [SerializeField] private string reloadSingleBulletTrigger = "ReloadSingleBullet";
    [SerializeField] private string finishReloadTrigger = "FinishReload";
    
    [Header("Auto-Finish Settings")]
    [SerializeField] private bool autoFinishWhenFull = true;
    [Tooltip("Automatically play finish reload animation when magazine is full")]
    
    // [SerializeField] private float autoFinishDelay = 0.3f; // Unused - commented out to eliminate warning
    // [Tooltip("Delay before auto-finishing when magazine becomes full")]

    // Runtime state
    private bool _isReloading = false;
    private ReloadPhase _currentPhase = ReloadPhase.None;
    private Coroutine _reloadCoroutine = null;
    private MonoBehaviour _coroutineRunner = null;
    private Animator _animator = null;
    private bool _finishReloadRequested = false;
    
    // Reload data
    private int _currentAmmo;
    private int _maxAmmo;
    private int _reserveAmmo;
    private System.Action<int> _onAmmoAdded;
    private System.Action _onReloadComplete;
    private System.Action _onReloadCancelled;

    private enum ReloadPhase
    {
        None,
        Starting,
        LoadingSingleBullet,
        Finishing
    }

    public bool IsReloading => _isReloading;

    /// <summary>
    /// Initialize the reload behavior with required components
    /// </summary>
    public void Initialize(MonoBehaviour coroutineRunner, Animator animator)
    {
        _coroutineRunner = coroutineRunner;
        _animator = animator;
    }



    public bool CanReload(int currentAmmo, int maxAmmo, int reserveAmmo)
    {
        return !_isReloading && currentAmmo < maxAmmo && reserveAmmo > 0;
    }

    public void StartReload(int currentAmmo, int maxAmmo, int reserveAmmo, System.Action<int> onAmmoAdded, System.Action onReloadComplete, System.Action onReloadCancelled)
    {
        if (_isReloading || _coroutineRunner == null)
            return;

        _currentAmmo = currentAmmo;
        _maxAmmo = maxAmmo;
        _reserveAmmo = reserveAmmo;
        _onAmmoAdded = onAmmoAdded;
        _onReloadComplete = onReloadComplete;
        _onReloadCancelled = onReloadCancelled;

        _isReloading = true;
        _finishReloadRequested = false;
        _reloadCoroutine = _coroutineRunner.StartCoroutine(SingleBulletReloadCoroutine());
    }

    public void CancelReload()
    {
        if (!_isReloading)
            return;

        // If we're in the middle of loading bullets, we can interrupt
        // If we're in start or finish phase, we should complete that phase gracefully
        if (_currentPhase == ReloadPhase.LoadingSingleBullet)
        {
            // Immediate cancellation during bullet loading
            StopReload(true);
        }
        else if (_currentPhase == ReloadPhase.Starting)
        {
            // Allow start phase to complete, then cancel
            // The coroutine will handle this gracefully
        }
        // Note: Finish phase should not be interrupted as it's the cocking animation
    }

    /// <summary>
    /// Request to skip to finish reload animation (for interrupting to shoot)
    /// </summary>
    public void RequestFinishReload()
    {
        if (_isReloading && _currentPhase == ReloadPhase.LoadingSingleBullet)
        {
            _finishReloadRequested = true;
        }
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
        _currentPhase = ReloadPhase.None;
        _finishReloadRequested = false;

        // Reset animator speed if it was changed
        if (_animator != null)
        {
            _animator.speed = 1.0f;
        }

        if (wasCancelled)
        {
            _onReloadCancelled?.Invoke();
        }
        else
        {
            _onReloadComplete?.Invoke();
        }
    }

    private IEnumerator SingleBulletReloadCoroutine()
    {
        // Phase 1: Start Reload Animation
        _currentPhase = ReloadPhase.Starting;
        
        if (_animator != null && !string.IsNullOrEmpty(startReloadTrigger))
        {
            _animator.SetTrigger(startReloadTrigger);
        }
        
        yield return new WaitForSeconds(startReloadDuration);

        // Phase 2: Load Single Bullets (can be interrupted)
        _currentPhase = ReloadPhase.LoadingSingleBullet;
        
        while (_currentAmmo < _maxAmmo && _reserveAmmo > 0 && _isReloading)
        {
            // Play single bullet reload animation
            if (_animator != null && !string.IsNullOrEmpty(reloadSingleBulletTrigger))
            {
                _animator.SetTrigger(reloadSingleBulletTrigger);
            }
            
            // Wait for animation duration first
            yield return new WaitForSeconds(singleBulletReloadDuration);
            
            // Then add one bullet after animation completes
            if (_reserveAmmo > 0)
            {
                _currentAmmo++;
                _reserveAmmo--;
                _onAmmoAdded?.Invoke(1); // Add 1 bullet
            }

            // Check if we should auto-finish when full
            if (_currentAmmo >= _maxAmmo && autoFinishWhenFull)
            {
                break;
            }

            // Check if finish reload was requested (interrupt to shoot)
            if (_finishReloadRequested)
            {
                break;
            }

            // No delay between bullets for faster reloading
            // yield return null; // Removed to eliminate delays
        }

        // Check if reload was cancelled during bullet loading
        if (!_isReloading)
            yield break;

        // Phase 3: Finish Reload Animation (cocking the shotgun)
        _currentPhase = ReloadPhase.Finishing;
        
        if (_animator != null && !string.IsNullOrEmpty(finishReloadTrigger))
        {
            _animator.SetTrigger(finishReloadTrigger);
        }
        
        yield return new WaitForSeconds(finishReloadDuration);

        // Reload complete
        StopReload(false);
    }

    /// <summary>
    /// Get the current reload phase for debugging or external systems
    /// </summary>
    public string GetCurrentPhaseString()
    {
        return _currentPhase.ToString();
    }

    /// <summary>
    /// Check if the reload can be safely interrupted (not during critical animations)
    /// </summary>
    public bool CanBeInterrupted()
    {
        return _currentPhase == ReloadPhase.LoadingSingleBullet;
    }
}