using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Treachery.Weapons.Interfaces;

/// <summary>
/// UI display for the weapon upgrade machine.
/// Shows when player is near machine: weapon info, upgrade cost, and prompt.
/// </summary>
public class UIWeaponUpgradePrompt : MonoBehaviour
{
    [Header("References")]
    [SerializeField] WeaponUpgradeMachine upgradeMachine;
    [SerializeField] CanvasGroup canvasGroup;

    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI weaponNameText;
    [SerializeField] TextMeshProUGUI currentTierText;
    [SerializeField] TextMeshProUGUI nextTierText;
    [SerializeField] TextMeshProUGUI costText;
    [SerializeField] TextMeshProUGUI promptText;
    [SerializeField] Image currentTierColorBar;
    [SerializeField] Image nextTierColorBar;

    [Header("Display Settings")]
    [SerializeField] string promptFormat = "Press [E] to Upgrade";
    [SerializeField] string costFormat = "Cost: {0} Souls";
    [SerializeField] string maxTierMessage = "MAX TIER";
    [SerializeField] float fadeSpeed = 8f;

    float _targetAlpha = 0f;

    void Start()
    {
        if (!canvasGroup)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup)
            canvasGroup.alpha = 0f;

        // Auto-find upgrade machine if not assigned
        if (!upgradeMachine)
            upgradeMachine = FindFirstObjectByType<WeaponUpgradeMachine>();
    }

    void Update()
    {
        if (!upgradeMachine || !canvasGroup) return;

        // Check if player is in range and has a weapon
        bool shouldShow = upgradeMachine.IsPlayerInRange();
        IUpgradeableWeapon currentWeapon = upgradeMachine.GetCurrentUpgradeableWeapon();
        shouldShow = shouldShow && currentWeapon != null;

        _targetAlpha = shouldShow ? 1f : 0f;

        // Fade in/out
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, _targetAlpha, Time.deltaTime * fadeSpeed);
        canvasGroup.interactable = canvasGroup.alpha > 0.5f;
        canvasGroup.blocksRaycasts = canvasGroup.alpha > 0.5f;

        // Update UI content when visible
        if (shouldShow && currentWeapon != null)
        {
            UpdateDisplay(currentWeapon);
        }
    }

    void UpdateDisplay(IUpgradeableWeapon weapon)
    {
        // Weapon name
        if (weaponNameText)
            weaponNameText.text = weapon.DisplayName;

        // Current tier
        WeaponTierData currentTierData = WeaponTierSystem.GetTierData(weapon.CurrentTier);
        if (currentTierText)
        {
            currentTierText.text = currentTierData.tierName;
            currentTierText.color = currentTierData.tierColor;
        }

        if (currentTierColorBar)
            currentTierColorBar.color = currentTierData.tierColor;

        // Check if weapon can be upgraded
        if (!weapon.CanUpgrade())
        {
            // Max tier reached
            if (nextTierText)
                nextTierText.text = maxTierMessage;
            
            if (costText)
                costText.text = "";

            if (promptText)
                promptText.text = "";

            if (nextTierColorBar)
                nextTierColorBar.gameObject.SetActive(false);
        }
        else
        {
            // Show next tier
            WeaponTier? nextTier = WeaponTierSystem.GetNextTier(weapon.CurrentTier);
            if (nextTier.HasValue)
            {
                WeaponTierData nextTierData = WeaponTierSystem.GetTierData(nextTier.Value);
                
                if (nextTierText)
                {
                    nextTierText.text = nextTierData.tierName;
                    nextTierText.color = nextTierData.tierColor;
                }

                if (nextTierColorBar)
                {
                    nextTierColorBar.gameObject.SetActive(true);
                    nextTierColorBar.color = nextTierData.tierColor;
                }
            }

            // Show cost
            int cost = weapon.GetUpgradeCost();
            if (costText)
            {
                costText.text = string.Format(costFormat, cost);
                
                // Change color based on affordability
                bool canAfford = SoulManager.Instance != null && SoulManager.Instance.CanAfford(cost);
                costText.color = canAfford ? Color.white : Color.red;
            }

            // Show prompt
            if (promptText)
                promptText.text = promptFormat;
        }
    }
}
