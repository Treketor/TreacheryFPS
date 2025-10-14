using System.Collections;
using UnityEngine;

public class WeaponInstance_Hitscan : MonoBehaviour
{
    [Header("Stats")]
    public string displayName = "Pistol";
    public string tierName = "Common";
    public float damage = 20f;
    public float fireRate = 5f; // shots per second
    public int magSize = 12;
    public float reloadTime = 1.2f;
    public float spread = 1.5f;

    [Header("Ammo Pools")]
    public int startingReserve = 60;

    [Header("References")]
    public WeaponRaycaster raycaster;
    public LayerMask hitMask;

    float _cooldown;
    int _inMag;
    int _reserve;
    bool _reloading;

    public System.Action<int, int> OnAmmoChanged;
    public System.Action<string, string> OnTierChanged;

    public string DisplayName => displayName;
    public string TierName => tierName;
    public int CurrentMag => _inMag;
    public int CurrentReserve => _reserve;

    void Awake()
    {
        _inMag = magSize;
        _reserve = Mathf.Max(0, startingReserve);
    }

    void Update()
    {
        if (_cooldown > 0f) _cooldown -= Time.deltaTime;
    }

    public void TryFire()
    {
        if (_reloading || _cooldown > 0f || _inMag <= 0) return;

        _cooldown = 1f / fireRate;
        _inMag--;
        OnAmmoChanged?.Invoke(_inMag, _reserve);

        if (raycaster.TryShoot(out var hit, spread))
        {
            if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
                damageable.TakeDamage(damage, hit.point, hit.normal);
            // TODO: spawn impact FX at hit.point
        }
        // TODO: spawn muzzle flash FX and sound effect
    }

    public void TryReload()
    {
        if (_reloading) return;
        if (_inMag >= magSize) return; // already full
        if (_reserve <= 0) return; // no reserve ammo

        StartCoroutine(ReloadCoroutine());
    }

    IEnumerator ReloadCoroutine()
    {
        _reloading = true;
        // play reload animation/sound here
        yield return new WaitForSeconds(reloadTime);

        int needed = magSize - _inMag;
        int taken = Mathf.Min(needed, _reserve);
        _inMag += taken;
        _reserve -= taken;

        _reloading = false;
        OnAmmoChanged?.Invoke(_inMag, _reserve);
    }

    public void ApplyTier(string newTierName, int newMagSize, float newDamage, float newReloadTime, float newSpread)
    {
        tierName = newTierName;
        magSize = newMagSize;
        damage = newDamage;
        reloadTime = newReloadTime;
        spread = newSpread;

        _inMag = Mathf.Min(_inMag, magSize);
        OnTierChanged?.Invoke(displayName, tierName);
        OnAmmoChanged?.Invoke(_inMag, _reserve);
    }

    public void AddReserve(int amount)
    {
        _reserve = Mathf.Max(0, _reserve + amount);
        OnAmmoChanged?.Invoke(_inMag, _reserve);
    }
}