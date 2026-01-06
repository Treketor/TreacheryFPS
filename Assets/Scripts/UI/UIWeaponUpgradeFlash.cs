using UnityEngine;
using System.Collections;
using Treachery.Weapons.Interfaces;

/// <summary>
/// Visual feedback for weapon upgrades.
/// Flashes the screen with the tier color and displays upgrade text.
/// </summary>
public class UIWeaponUpgradeFlash : MonoBehaviour
{
    [Header("References")]
    [SerializeField] UnityEngine.UI.Image flashImage;
    [SerializeField] TMPro.TextMeshProUGUI upgradeText;

    [Header("Flash Settings")]
    [SerializeField] float flashDuration = 0.5f;
    [SerializeField] float flashIntensity = 0.3f;
    [SerializeField] AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Text Settings")]
    [SerializeField] string upgradeMessageFormat = "{0} Upgraded to {1}!";
    [SerializeField] float textDisplayDuration = 2f;
    [SerializeField] float textFadeSpeed = 3f;

    Coroutine _currentFlash;
    Coroutine _currentTextFade;

    void Start()
    {
        // Initialize to transparent
        if (flashImage)
        {
            Color c = flashImage.color;
            c.a = 0f;
            flashImage.color = c;
        }

        if (upgradeText)
        {
            Color c = upgradeText.color;
            c.a = 0f;
            upgradeText.color = c;
        }

        // Subscribe to all upgrade machines in scene
        var machines = FindObjectsByType<WeaponUpgradeMachine>(FindObjectsSortMode.None);
        foreach (var machine in machines)
        {
            machine.OnWeaponUpgraded += OnWeaponUpgraded;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from all upgrade machines
        var machines = FindObjectsByType<WeaponUpgradeMachine>(FindObjectsSortMode.None);
        foreach (var machine in machines)
        {
            if (machine != null)
                machine.OnWeaponUpgraded -= OnWeaponUpgraded;
        }
    }

    void OnWeaponUpgraded(IUpgradeableWeapon weapon, WeaponTier newTier)
    {
        Color tierColor = WeaponTierSystem.GetTierColor(newTier);
        string tierName = WeaponTierSystem.GetTierData(newTier).tierName;
        string message = string.Format(upgradeMessageFormat, weapon.DisplayName, tierName);

        TriggerFlash(tierColor);
        ShowUpgradeText(message, tierColor);
    }

    void TriggerFlash(Color color)
    {
        if (!flashImage) return;

        if (_currentFlash != null)
            StopCoroutine(_currentFlash);

        _currentFlash = StartCoroutine(FlashCoroutine(color));
    }

    IEnumerator FlashCoroutine(Color baseColor)
    {
        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashDuration;
            float alpha = flashCurve.Evaluate(t) * flashIntensity;

            Color c = baseColor;
            c.a = alpha;
            flashImage.color = c;

            yield return null;
        }

        // Ensure it ends transparent
        Color finalColor = baseColor;
        finalColor.a = 0f;
        flashImage.color = finalColor;
    }

    void ShowUpgradeText(string message, Color color)
    {
        if (!upgradeText) return;

        if (_currentTextFade != null)
            StopCoroutine(_currentTextFade);

        upgradeText.text = message;
        upgradeText.color = color;
        _currentTextFade = StartCoroutine(TextFadeCoroutine());
    }

    IEnumerator TextFadeCoroutine()
    {
        // Fade in
        Color c = upgradeText.color;
        c.a = 0f;
        upgradeText.color = c;

        while (c.a < 1f)
        {
            c.a += Time.deltaTime * textFadeSpeed;
            upgradeText.color = c;
            yield return null;
        }

        c.a = 1f;
        upgradeText.color = c;

        // Hold
        yield return new WaitForSeconds(textDisplayDuration);

        // Fade out
        while (c.a > 0f)
        {
            c.a -= Time.deltaTime * textFadeSpeed;
            upgradeText.color = c;
            yield return null;
        }

        c.a = 0f;
        upgradeText.color = c;
    }
}
