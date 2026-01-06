using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Treachery.Weapons.Interfaces;
using Treachery.Weapons.Runtime;

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
    [SerializeField] MonoBehaviour weaponController;
    [SerializeField] bool autoFindWeaponController = true;

    IWeaponController _weaponController;

    private UIWeaponSlotDisplay[] slotDisplays;

    void Start()
    {
        // Auto-find weapon controller
        if (!weaponController && autoFindWeaponController)
        {
            var v2 = FindFirstObjectByType<WeaponControllerV2>();
            if (v2 != null)
                weaponController = v2;
            else
                weaponController = FindFirstObjectByType<WeaponController>();
        }

        _weaponController = weaponController as IWeaponController;

        if (_weaponController != null)
        {
            // Subscribe to events
            _weaponController.WeaponChanged += HandleWeaponChanged;
            _weaponController.AmmoChanged += HandleAmmoChanged;
            _weaponController.WeaponSlotChanged += HandleWeaponSlotChanged;
            
            // Initialize slots
            InitializeSlots();
            
            // Update display
            UpdateAllSlots();
        }
        else
        {
            Debug.LogWarning("UIWeaponSlots: No weapon controller found (WeaponControllerV2 or WeaponController). ");
        }
    }

    void OnDestroy()
    {
        if (_weaponController != null)
        {
            _weaponController.WeaponChanged -= HandleWeaponChanged;
            _weaponController.AmmoChanged -= HandleAmmoChanged;
            _weaponController.WeaponSlotChanged -= HandleWeaponSlotChanged;
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
        slotDisplays = new UIWeaponSlotDisplay[_weaponController.WeaponSlotCount];
        
        for (int i = 0; i < _weaponController.WeaponSlotCount; i++)
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
        UpdateSlot(_weaponController.ActiveWeaponIndex);
    }

    void UpdateSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotDisplays.Length)
            return;

        var weapon = _weaponController.GetWeaponInSlot(slotIndex);
        slotDisplays[slotIndex].UpdateDisplay(weapon);
    }

    void UpdateSlotHighlighting()
    {
        for (int i = 0; i < slotDisplays.Length; i++)
        {
            bool isActive = (i == _weaponController.ActiveWeaponIndex);
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

    public void UpdateDisplay(IWeapon weapon)
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