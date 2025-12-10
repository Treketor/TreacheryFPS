using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Singleton that manages the player's score and statistics.
/// Tracks kills, headshots, wave reached, and calculates total score.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score Values")]
    [SerializeField] int pointsPerKill = 100;
    [SerializeField] int pointsPerHeadshot = 50; // Bonus on top of kill
    [SerializeField] int pointsPerBossKill = 500;
    [SerializeField] int pointsPerWaveComplete = 200;

    [Header("Statistics")]
    [SerializeField] int totalKills = 0;
    [SerializeField] int totalHeadshots = 0;
    [SerializeField] int totalShotsFired = 0;
    [SerializeField] int bossKills = 0;
    [SerializeField] int wavesCompleted = 0;
    [SerializeField] int highestWaveReached = 0;
    [SerializeField] int currentScore = 0;

    // Events
    public UnityEvent<int> OnScoreChanged; // new score
    public UnityEvent<int> OnKill; // total kills
    public UnityEvent<int> OnHeadshot; // total headshots
    public UnityEvent<int> OnWaveComplete; // waves completed
    public UnityEvent<int> OnShotFired; // total shots fired

    // Public accessors
    public int TotalKills => totalKills;
    public int TotalHeadshots => totalHeadshots;
    public int TotalShotsFired => totalShotsFired;
    public int BossKills => bossKills;
    public int WavesCompleted => wavesCompleted;
    public int HighestWaveReached => highestWaveReached;
    public int CurrentScore => currentScore;
    public float HeadshotAccuracy => totalShotsFired > 0 ? (float)totalHeadshots / totalShotsFired * 100f : 0f;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        // Broadcast initial values
        OnScoreChanged?.Invoke(currentScore);
    }

    /// <summary>
    /// Register a shot fired (for accuracy tracking).
    /// </summary>
    public void RegisterShotFired()
    {
        totalShotsFired++;
        OnShotFired?.Invoke(totalShotsFired);
    }

    /// <summary>
    /// Register a headshot hit (doesn't need to be a kill).
    /// </summary>
    public void RegisterHeadshot()
    {
        totalHeadshots++;
        OnHeadshot?.Invoke(totalHeadshots);
    }

    /// <summary>
    /// Register a kill and award points.
    /// </summary>
    public void RegisterKill(bool isHeadshot = false, bool isBoss = false)
    {
        totalKills++;
        int points = pointsPerKill;

        if (isHeadshot)
        {
            points += pointsPerHeadshot;
        }

        if (isBoss)
        {
            bossKills++;
            points = pointsPerBossKill; // Boss kills override normal points
        }

        AddScore(points);
        OnKill?.Invoke(totalKills);


    }

    /// <summary>
    /// Register wave completion and award bonus.
    /// </summary>
    public void RegisterWaveComplete(int waveNumber)
    {
        wavesCompleted++;
        
        if (waveNumber > highestWaveReached)
            highestWaveReached = waveNumber;

        int bonus = pointsPerWaveComplete;
        // Optional: Scale bonus with wave number
        bonus += waveNumber * 10; // +10 points per wave level

        AddScore(bonus);
        OnWaveComplete?.Invoke(wavesCompleted);

        Debug.Log($"Wave {waveNumber} complete! +{bonus} points");
    }

    /// <summary>
    /// Add points to the score.
    /// </summary>
    void AddScore(int points)
    {
        // TODO: Apply multiplayer multiplier if in multiplayer
        currentScore += points;
        OnScoreChanged?.Invoke(currentScore);
    }

    /// <summary>
    /// Calculate final score with all bonuses.
    /// </summary>
    public int CalculateFinalScore()
    {
        int finalScore = currentScore;

        // Survival bonus based on highest wave reached
        int survivalBonus = highestWaveReached * 100;
        finalScore += survivalBonus;

        // Accuracy bonus for high headshot percentage
        if (HeadshotAccuracy >= 50f)
            finalScore += 1000; // 50%+ headshot bonus

        // Boss slayer bonus
        if (bossKills > 0)
            finalScore += bossKills * 250;

        return finalScore;
    }

    /// <summary>
    /// Get a summary of the current run.
    /// </summary>
    public RunSummary GetRunSummary()
    {
        return new RunSummary
        {
            totalKills = this.totalKills,
            totalHeadshots = this.totalHeadshots,
            bossKills = this.bossKills,
            wavesCompleted = this.wavesCompleted,
            highestWave = this.highestWaveReached,
            headshotAccuracy = this.HeadshotAccuracy,
            baseScore = this.currentScore,
            finalScore = CalculateFinalScore()
        };
    }

    /// <summary>
    /// Reset all stats (for new run).
    /// </summary>
    public void ResetStats()
    {
        totalKills = 0;
        totalHeadshots = 0;
        totalShotsFired = 0;
        bossKills = 0;
        wavesCompleted = 0;
        highestWaveReached = 0;
        currentScore = 0;

        OnScoreChanged?.Invoke(currentScore);
        OnKill?.Invoke(totalKills);
        OnHeadshot?.Invoke(totalHeadshots);
        OnShotFired?.Invoke(totalShotsFired);
    }

    // Debug helpers
#if UNITY_EDITOR
    [ContextMenu("Debug: Register Kill")]
    void DebugKill() => RegisterKill(false, false);

    [ContextMenu("Debug: Register Headshot")]
    void DebugHeadshot() => RegisterKill(true, false);

    [ContextMenu("Debug: Register Boss Kill")]
    void DebugBossKill() => RegisterKill(false, true);

    [ContextMenu("Debug: Complete Wave")]
    void DebugWave() => RegisterWaveComplete(highestWaveReached + 1);

    [ContextMenu("Debug: Print Summary")]
    void DebugPrintSummary()
    {
        var summary = GetRunSummary();
        Debug.Log($"=== RUN SUMMARY ===\n" +
                  $"Kills: {summary.totalKills}\n" +
                  $"Headshots: {summary.totalHeadshots} ({summary.headshotAccuracy:F1}%)\n" +
                  $"Boss Kills: {summary.bossKills}\n" +
                  $"Waves: {summary.wavesCompleted} (Highest: {summary.highestWave})\n" +
                  $"Base Score: {summary.baseScore}\n" +
                  $"Final Score: {summary.finalScore}");
    }
#endif
}

/// <summary>
/// Data structure for end-of-run summary.
/// </summary>
[System.Serializable]
public struct RunSummary
{
    public int totalKills;
    public int totalHeadshots;
    public int bossKills;
    public int wavesCompleted;
    public int highestWave;
    public float headshotAccuracy;
    public int baseScore;
    public int finalScore;
}
