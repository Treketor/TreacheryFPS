using UnityEngine;
using TMPro;

/// <summary>
/// UI component that displays the player's current soul count.
/// Updates automatically when souls change.
/// </summary>
public class UISoulCounter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] TextMeshProUGUI soulText;

    [Header("Display Format")]
    [SerializeField] string prefix = "Souls: ";
    [SerializeField] bool useThousandsSeparator = true;

    [Header("Visual Feedback")]
    [SerializeField] bool pulseOnChange = true;
    [SerializeField] float pulseScale = 1.15f;
    [SerializeField] float pulseSpeed = 8f;

    Vector3 _originalScale;
    float _pulseTimer;

    void Start()
    {
        if (!soulText) soulText = GetComponent<TextMeshProUGUI>();
        
        _originalScale = soulText.transform.localScale;

        // Subscribe to soul manager events
        if (SoulManager.Instance != null)
        {
            SoulManager.Instance.OnSoulsChanged += OnSoulsChanged;
            // Set initial display
            UpdateDisplay(SoulManager.Instance.CurrentSouls);
        }
        else
        {
            Debug.LogWarning("UISoulCounter: SoulManager instance not found!");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (SoulManager.Instance != null)
        {
            SoulManager.Instance.OnSoulsChanged -= OnSoulsChanged;
        }
    }

    void Update()
    {
        // Pulse animation
        if (_pulseTimer > 0f)
        {
            _pulseTimer -= Time.deltaTime * pulseSpeed;
            float scale = Mathf.Lerp(1f, pulseScale, Mathf.Clamp01(_pulseTimer));
            soulText.transform.localScale = _originalScale * scale;
        }
    }

    void OnSoulsChanged(int newSoulCount)
    {
        UpdateDisplay(newSoulCount);

        if (pulseOnChange)
        {
            _pulseTimer = 1f;
        }
    }

    void UpdateDisplay(int soulCount)
    {
        if (!soulText) return;

        string formattedNumber = useThousandsSeparator 
            ? soulCount.ToString("N0") 
            : soulCount.ToString();

        soulText.text = $"{prefix}{formattedNumber}";
    }
}
