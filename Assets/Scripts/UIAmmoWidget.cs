using UnityEngine;
using TMPro;

public class UIAmmoWidget : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_Text ammoText;
    [SerializeField] TMP_Text weaponNameText;
    [SerializeField] TMP_Text weaponTierText;

    [Header("Sources")]
    [SerializeField] WeaponController weaponController;

    [Header("Low Ammo FX")]
    [SerializeField] private int lowAmmoThreshold = 2;
    [SerializeField] private Color normalAmmoColor = Color.white;
    [SerializeField] private Color lowAmmoColor = new Color32(255, 80, 80, 255);
    [SerializeField] private bool pulseOnLowAmmo = true;
    [SerializeField] private float pulseSpeed = 6f;
    [SerializeField] private float pulseScale = 1.08f;

    Vector3 _defaultScale;
    bool _isLow;
    Color _currentTierColor;

    void Start()
    {
        _defaultScale = ammoText.transform.localScale;
        _currentTierColor = normalAmmoColor;

        if (!weaponController) weaponController = FindFirstObjectByType<WeaponController>();
        if (!weaponController) { enabled = false; return; }

        // Subscribe to events
        weaponController.OnAmmoChanged += HandleAmmoChanged;
        weaponController.OnWeaponChanged += HandleWeaponChanged;

        // Initialise with current values
        HandleWeaponChanged(weaponController.CurrentWeaponDisplayName, weaponController.CurrentTierName);
        HandleAmmoChanged(weaponController.CurrentAmmoInMag, weaponController.CurrentReserveAmmo);
    }

    void OnDestroy()
    {
        if (!weaponController) return;
        weaponController.OnAmmoChanged -= HandleAmmoChanged;
        weaponController.OnWeaponChanged -= HandleWeaponChanged;
    }

    void Update()
    {
        if (pulseOnLowAmmo && _isLow)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float s = Mathf.Lerp(1f, pulseScale, t);
            ammoText.transform.localScale = _defaultScale * s;
        }
        else
        {
            ammoText.transform.localScale = Vector3.Lerp(ammoText.transform.localScale, _defaultScale, Time.deltaTime * 10f);
        }
    }

    void HandleAmmoChanged(int inMag, int reserve)
    {
        ammoText.text = (reserve >= 0) ? $"{inMag}/{reserve}" : $"{inMag}";
        _isLow = inMag <= lowAmmoThreshold;
        
        // Update color: low ammo = red, normal ammo = current tier color
        ammoText.color = _isLow ? lowAmmoColor : _currentTierColor;
    }

    void HandleWeaponChanged(string displayName, string tierName)
    {
        var c = TierColorUtility.GetColor(tierName);
        string hex = TierColorUtility.ToHex(c);

        // Set weapon name
        if (weaponNameText)
        {
            weaponNameText.text = $"{displayName.ToUpper()}";
            weaponNameText.color = c;
        }

        // Set tier with color
        if (weaponTierText)
        {
            weaponTierText.text = $"({tierName.ToUpper()})";
            weaponTierText.color = c;
        }

        // Store the tier color and apply it if not currently in low ammo state
        _currentTierColor = c;
        if (!_isLow) ammoText.color = _currentTierColor;
    }
}