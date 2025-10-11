using UnityEngine;
using TMPro;

public class UIAmmoWidget : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_Text ammoText;
    [SerializeField] TMP_Text weaponNameTierText;

    [Header("Sources")]
    [SerializeField] WeaponController weaponController;

    void Start()
    {
        if (!weaponController) weaponController = FindFirstObjectByType<WeaponController>();
        if (!weaponController)
        {
            Debug.LogWarning("[UIAmmoWidget] No WeaponController found.");
            enabled = false;
            return;
        }

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

    void HandleAmmoChanged(int inMag, int reserve)
    {
        ammoText.text = (reserve >= 0) ? $"{inMag} / {reserve}" : $"{inMag}";
    }

    void HandleWeaponChanged(string displayName, string tierName)
    {
        weaponNameTierText.text = $"{displayName}  <size=80%><color=#AAAAAA>({tierName})</color></size>";
    }
}