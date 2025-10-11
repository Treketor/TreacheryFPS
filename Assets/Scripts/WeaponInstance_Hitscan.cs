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

    [Header("References")]
    public WeaponRaycaster raycaster;
    public LayerMask hitMask;

    float _cooldown;
    int _inMag;
    bool _reloading;

    public System.Action<int, int> OnAmmoChanged;
    public System.Action<string, string> OnTierChanged;

    public string DisplayName => displayName;
    public string TierName => tierName;
    public int CurrentMag => _inMag;
    public int CurrentReserve => 9999; // infinite reserve for now

    void Awake()
    {
        _inMag = magSize;
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
        OnAmmoChanged?.Invoke(_inMag, CurrentReserve);

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
        if (_reloading || _inMag == magSize) return;
        StartCoroutine(ReloadCoroutine());
    }

    IEnumerator ReloadCoroutine()
    {
        _reloading = true;
        yield return new WaitForSeconds(reloadTime);
        _inMag = magSize;
        _reloading = false;
        OnAmmoChanged?.Invoke(_inMag, CurrentReserve);
    }

    public void ApplyTier(string newTierName, int newMagSize, float newDamage, float newReloadTime, float newSpread)
    {
        tierName = newTierName;
        magSize = newMagSize;
        damage = newDamage;
        reloadTime = newReloadTime;
        spread = newSpread;

        _inMag = Mathf.Min(_inMag, magSize); // clamp if mag shrank
        OnTierChanged?.Invoke(displayName, tierName);
        OnAmmoChanged?.Invoke(_inMag, CurrentReserve);
    }
}