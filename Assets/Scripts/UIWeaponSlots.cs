using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// UI component that displays both weapon slots with highlighting for active slot.
/// </summary>
public class UIWeaponSlots : MonoBehaviour
{
    [Header("Weapon Slot UI")]
    [SerializeField] GameObject slotPrefab;
    [SerializeField] Transform slotsContainer;
    
    [Header("Slot Display")]
    [SerializeField] Color activeSlotColor = Color.white;
    [SerializeField] Color inactiveSlotColor = Color.gray;
    
    [Header("Auto Setup")]
    [SerializeField] WeaponController weaponController;
    [SerializeField] bool autoFindWeaponController = true;

    private UIWeaponSlotDisplay[] slotDisplays;

    void Start()
    {
        // Auto-find weapon controller
        if (!weaponController && autoFindWeaponController)
        {
            weaponController = FindFirstObjectByType<WeaponController>();
        }

        if (weaponController != null)
        {
            // Subscribe to events
            weaponController.OnWeaponChanged += HandleWeaponChanged;
            weaponController.OnAmmoChanged += HandleAmmoChanged;
            weaponController.OnWeaponSlotChanged += HandleWeaponSlotChanged;
            
            // Initialize slots
            InitializeSlots();
            
            // Update display
            UpdateAllSlots();
        }
        else
        {
            Debug.LogWarning("UIWeaponSlots: WeaponController not found!");
        }
    }

    void OnDestroy()
    {
        if (weaponController != null)
        {
            weaponController.OnWeaponChanged -= HandleWeaponChanged;
            weaponController.OnAmmoChanged -= HandleAmmoChanged;
            weaponController.OnWeaponSlotChanged -= HandleWeaponSlotChanged;
        }
    }

    void InitializeSlots()
    {
        // Clear existing slots
        foreach (Transform child in slotsContainer)
        {
            DestroyImmediate(child.gameObject);
        }

        // Create slot displays
        slotDisplays = new UIWeaponSlotDisplay[weaponController.WeaponSlotCount];
        
        for (int i = 0; i < weaponController.WeaponSlotCount; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
            slotDisplays[i] = slotObj.GetComponent<UIWeaponSlotDisplay>();
            
            if (slotDisplays[i] == null)
            {
                slotDisplays[i] = slotObj.AddComponent<UIWeaponSlotDisplay>();
            }
            
            slotDisplays[i].Initialize(i + 1); // Display as 1-indexed
        }
    }

    void HandleWeaponChanged(string displayName, string tierName)
    {
        UpdateCurrentSlot();
    }

    void HandleAmmoChanged(int inMag, int reserve)
    {
        UpdateCurrentSlot();
    }

    void HandleWeaponSlotChanged(int activeSlot, int totalSlots)
    {
        UpdateSlotHighlighting();
        UpdateCurrentSlot();
    }

    void UpdateAllSlots()
    {
        for (int i = 0; i < slotDisplays.Length; i++)
        {
            UpdateSlot(i);
        }
        UpdateSlotHighlighting();
    }

    void UpdateCurrentSlot()
    {
        UpdateSlot(weaponController.ActiveWeaponIndex);
    }

    void UpdateSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotDisplays.Length)
            return;

        var weapon = weaponController.GetWeaponInSlot(slotIndex);
        slotDisplays[slotIndex].UpdateDisplay(weapon);
    }

    void UpdateSlotHighlighting()
    {
        for (int i = 0; i < slotDisplays.Length; i++)
        {
            bool isActive = (i == weaponController.ActiveWeaponIndex);
            Color slotColor = isActive ? activeSlotColor : inactiveSlotColor;
            slotDisplays[i].SetHighlight(isActive, slotColor);
        }
    }
}

/// <summary>
/// Individual weapon slot display component.
/// </summary>
public class UIWeaponSlotDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] TextMeshProUGUI slotNumberText;
    [SerializeField] TextMeshProUGUI weaponNameText;
    [SerializeField] TextMeshProUGUI weaponTierText;
    [SerializeField] TextMeshProUGUI ammoText;
    [SerializeField] Image slotBackground;
    [SerializeField] GameObject emptySlotIndicator;

    private int slotNumber;

    public void Initialize(int displaySlotNumber)
    {
        slotNumber = displaySlotNumber;
        
        if (slotNumberText)
            slotNumberText.text = slotNumber.ToString();

        // Auto-find components if not assigned
        if (!slotNumberText) slotNumberText = GetComponentInChildren<TextMeshProUGUI>();
        if (!slotBackground) slotBackground = GetComponent<Image>();
    }

    public void UpdateDisplay(WeaponInstance_Hitscan weapon)
    {
        bool hasWeapon = weapon != null;
        
        if (emptySlotIndicator)
            emptySlotIndicator.SetActive(!hasWeapon);

        if (hasWeapon)
        {
            if (weaponNameText)
                weaponNameText.text = weapon.DisplayName;
            
            if (weaponTierText)
            {
                weaponTierText.text = weapon.TierName;
                weaponTierText.color = TierColorUtility.GetColor(weapon.TierName);
            }
            
            if (ammoText)
                ammoText.text = $"{weapon.CurrentMag}/{weapon.CurrentReserve}";
        }
        else
        {
            if (weaponNameText) weaponNameText.text = "Empty";
            if (weaponTierText) weaponTierText.text = "";
            if (ammoText) ammoText.text = "";
        }
    }

    public void SetHighlight(bool isActive, Color highlightColor)
    {
        if (slotBackground)
        {
            slotBackground.color = highlightColor;
        }
    }
}