using UnityEngine;
using TMPro;

/// <summary>
/// UI component that displays the player's current score.
/// Updates automatically when score changes.
/// </summary>
public class UIScoreDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] TextMeshProUGUI scoreText;

    [Header("Display Format")]
    [SerializeField] string prefix = "";
    [SerializeField] int arcadeDigits = 6; // Number of digits to display (e.g., 6 = 000000)

    [Header("Visual Feedback")]
    [SerializeField] bool pulseOnScoreGain = true;
    [SerializeField] float pulseScale = 1.1f;
    [SerializeField] float pulseSpeed = 10f;
    [SerializeField] Color scoreGainColor = Color.yellow;
    [SerializeField] float colorFadeSpeed = 5f;

    Vector3 _originalScale;
    Color _originalColor;
    float _pulseTimer;
    float _colorTimer;

    void Start()
    {
        if (!scoreText) scoreText = GetComponent<TextMeshProUGUI>();
        
        _originalScale = scoreText.transform.localScale;
        _originalColor = scoreText.color;

        // Subscribe to score manager events
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged.AddListener(OnScoreChanged);
            // Set initial display
            UpdateDisplay(ScoreManager.Instance.CurrentScore);
        }
        else
        {
            Debug.LogWarning("UIScoreDisplay: ScoreManager instance not found!");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged.RemoveListener(OnScoreChanged);
        }
    }

    void Update()
    {
        // Pulse animation
        if (_pulseTimer > 0f)
        {
            _pulseTimer -= Time.deltaTime * pulseSpeed;
            float scale = Mathf.Lerp(1f, pulseScale, Mathf.Clamp01(_pulseTimer));
            scoreText.transform.localScale = _originalScale * scale;
        }

        // Color fade
        if (_colorTimer > 0f)
        {
            _colorTimer -= Time.deltaTime * colorFadeSpeed;
            scoreText.color = Color.Lerp(_originalColor, scoreGainColor, Mathf.Clamp01(_colorTimer));
        }
    }

    void OnScoreChanged(int newScore)
    {
        UpdateDisplay(newScore);

        if (pulseOnScoreGain)
        {
            _pulseTimer = 1f;
            _colorTimer = 1f;
        }
    }

    void UpdateDisplay(int score)
    {
        if (!scoreText) return;

        // Format as arcade-style with leading zeros (e.g., 000000, 000150, 012345)
        string formattedNumber = score.ToString($"D{arcadeDigits}");

        scoreText.text = $"{prefix}{formattedNumber}";
    }
}
