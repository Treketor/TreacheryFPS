using UnityEngine;

/// <summary>
/// Singleton that manages the player's soul currency.
/// Souls are earned by killing enemies and spent on upgrades, gambling, and consumables.
/// </summary>
public class SoulManager : MonoBehaviour
{
    public static SoulManager Instance { get; private set; }

    [Header("Starting Souls")]
    [SerializeField] int startingSouls = 0;

    int _currentSouls;

    public int CurrentSouls => _currentSouls;

    // Events for UI and other systems to subscribe to
    public System.Action<int> OnSoulsChanged; // new soul count
    public System.Action<int> OnSoulsGained; // amount gained
    public System.Action<int> OnSoulsSpent; // amount spent

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _currentSouls = startingSouls;
    }

    void Start()
    {
        // Broadcast initial soul count
        OnSoulsChanged?.Invoke(_currentSouls);
    }

    /// <summary>
    /// Add souls to the player's total.
    /// </summary>
    public void AddSouls(int amount)
    {
        if (amount <= 0) return;

        _currentSouls += amount;
        OnSoulsGained?.Invoke(amount);
        OnSoulsChanged?.Invoke(_currentSouls);
    }

    /// <summary>
    /// Attempt to spend souls. Returns true if successful, false if not enough souls.
    /// </summary>
    public bool TrySpendSouls(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("Cannot spend 0 or negative souls.");
            return false;
        }

        if (_currentSouls < amount)
        {
            Debug.Log($"Not enough souls! Need {amount}, have {_currentSouls}");
            return false;
        }

        _currentSouls -= amount;
        OnSoulsSpent?.Invoke(amount);
        OnSoulsChanged?.Invoke(_currentSouls);
        return true;
    }

    /// <summary>
    /// Check if player can afford a purchase.
    /// </summary>
    public bool CanAfford(int amount)
    {
        return _currentSouls >= amount;
    }

    /// <summary>
    /// Force set souls (for debugging or special events).
    /// </summary>
    public void SetSouls(int amount)
    {
        _currentSouls = Mathf.Max(0, amount);
        OnSoulsChanged?.Invoke(_currentSouls);
    }

#if UNITY_EDITOR
    [ContextMenu("Debug: Add 100 Souls")]
    void DebugAdd100Souls() => AddSouls(100);

    [ContextMenu("Debug: Add 500 Souls")]
    void DebugAdd500Souls() => AddSouls(500);

    [ContextMenu("Debug: Add 1000 Souls")]
    void DebugAdd1000Souls() => AddSouls(1000);

    [ContextMenu("Debug: Spend 50 Souls")]
    void DebugSpendSouls() => TrySpendSouls(50);
#endif
}
