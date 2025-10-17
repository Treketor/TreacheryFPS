using UnityEngine;
using TMPro;

/// <summary>
/// UI component that displays detailed combat statistics (kills, headshots, etc.)
/// DISABLED - Script is inactive and will not display stats
/// </summary>
public class UIStatsDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] TextMeshProUGUI killsText;
    [SerializeField] TextMeshProUGUI headshotsText;
    [SerializeField] TextMeshProUGUI accuracyText;

    [Header("Display Format")]
    [SerializeField] string killsFormat = "Kills: {0}";
    [SerializeField] string headshotsFormat = "Headshots: {0}";
    [SerializeField] string accuracyFormat = "Accuracy: {0:F1}%";

    // Script disabled - uncomment methods below to re-enable

    /*
    void Start()
    {
        // Subscribe to score manager events
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnKill.AddListener(UpdateKills);
            ScoreManager.Instance.OnHeadshot.AddListener(UpdateHeadshots);
            
            // Initial update
            UpdateKills(ScoreManager.Instance.TotalKills);
            UpdateHeadshots(ScoreManager.Instance.TotalHeadshots);
            UpdateAccuracy();
        }
    }

    void OnDestroy()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnKill.RemoveListener(UpdateKills);
            ScoreManager.Instance.OnHeadshot.RemoveListener(UpdateHeadshots);
        }
    }

    void UpdateKills(int kills)
    {
        if (killsText)
            killsText.text = string.Format(killsFormat, kills);
        
        UpdateAccuracy();
    }

    void UpdateHeadshots(int headshots)
    {
        if (headshotsText)
            headshotsText.text = string.Format(headshotsFormat, headshots);
        
        UpdateAccuracy();
    }

    void UpdateAccuracy()
    {
        if (accuracyText && ScoreManager.Instance != null)
        {
            float accuracy = ScoreManager.Instance.HeadshotAccuracy;
            accuracyText.text = string.Format(accuracyFormat, accuracy);
        }
    }
    */
}
