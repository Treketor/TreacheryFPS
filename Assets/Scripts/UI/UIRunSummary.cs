using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// End-of-run summary screen that displays all statistics and final score.
/// Shows when player dies or completes the run.
/// </summary>
public class UIRunSummary : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] GameObject summaryPanel;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI killsText;
    [SerializeField] TextMeshProUGUI headshotsText;
    [SerializeField] TextMeshProUGUI accuracyText;
    [SerializeField] TextMeshProUGUI bossKillsText;
    [SerializeField] TextMeshProUGUI wavesText;
    [SerializeField] TextMeshProUGUI highestWaveText;
    [SerializeField] TextMeshProUGUI baseScoreText;
    [SerializeField] TextMeshProUGUI finalScoreText;

    [Header("Display Settings")]
    [SerializeField] string titleSuccess = "RUN COMPLETE!";
    [SerializeField] string titleDeath = "YOU DIED";
    [SerializeField] float delayBeforeShow = 2f;
    [SerializeField] bool animateNumbers = true;
    [SerializeField] float numberAnimationDuration = 1f;

    [Header("Auto-Connect")]
    [SerializeField] PlayerHealth playerHealth;
    [SerializeField] bool autoFindPlayer = true;

    void Start()
    {
        Debug.Log("UIRunSummary: Start() called");
        
        // Hide panel initially
        if (summaryPanel)
            summaryPanel.SetActive(false);
        else
            Debug.LogWarning("UIRunSummary: Summary Panel reference is missing!");

        // Auto-find player if not assigned
        if (autoFindPlayer && !playerHealth)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth)
                Debug.Log("UIRunSummary: Auto-found PlayerHealth");
        }

        // Subscribe to player death event
        if (playerHealth)
        {
            playerHealth.OnDeath.AddListener(OnPlayerDeath);
            Debug.Log("UIRunSummary: Successfully subscribed to PlayerHealth.OnDeath event");
        }
        else
        {
            Debug.LogWarning("UIRunSummary: PlayerHealth not found! Run summary won't show on death.");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (playerHealth)
            playerHealth.OnDeath.RemoveListener(OnPlayerDeath);
    }

    void OnPlayerDeath()
    {
        Debug.Log("UIRunSummary: OnPlayerDeath() called - showing summary");
        ShowSummary(false); // false = player died (not successful completion)
    }

    /// <summary>
    /// Show the summary screen with current run stats.
    /// </summary>
    public void ShowSummary(bool wasSuccessful = false)
    {
        Debug.Log($"UIRunSummary: ShowSummary() called with wasSuccessful={wasSuccessful}");
        
        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("UIRunSummary: ScoreManager not found!");
            return;
        }

        // Activate panel FIRST so coroutines can run
        if (summaryPanel)
        {
            summaryPanel.SetActive(true);
            Debug.Log("UIRunSummary: Summary panel activated");
        }
        else
        {
            Debug.LogError("UIRunSummary: Cannot show summary - summaryPanel is null!");
            return;
        }

        StartCoroutine(ShowSummaryCoroutine(wasSuccessful));
    }

    IEnumerator ShowSummaryCoroutine(bool wasSuccessful)
    {
        yield return new WaitForSeconds(delayBeforeShow);

        RunSummary summary = ScoreManager.Instance.GetRunSummary();

        // Set title
        if (titleText)
            titleText.text = wasSuccessful ? titleSuccess : titleDeath;

        if (animateNumbers)
        {
            yield return AnimateStats(summary);
        }
        else
        {
            DisplayStats(summary);
        }
    }

    IEnumerator AnimateStats(RunSummary summary)
    {
        float elapsed = 0f;

        while (elapsed < numberAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / numberAnimationDuration;

            // Animate each stat from 0 to final value
            int kills = Mathf.RoundToInt(Mathf.Lerp(0, summary.totalKills, t));
            int headshots = Mathf.RoundToInt(Mathf.Lerp(0, summary.totalHeadshots, t));
            int bossKills = Mathf.RoundToInt(Mathf.Lerp(0, summary.bossKills, t));
            int waves = Mathf.RoundToInt(Mathf.Lerp(0, summary.wavesCompleted, t));
            int baseScore = Mathf.RoundToInt(Mathf.Lerp(0, summary.baseScore, t));
            int finalScore = Mathf.RoundToInt(Mathf.Lerp(0, summary.finalScore, t));

            if (killsText)
                killsText.text = $"Kills: {kills}";
            if (headshotsText)
                headshotsText.text = $"Headshots: {headshots}";
            if (accuracyText)
                accuracyText.text = $"Headshot Accuracy: {summary.headshotAccuracy:F1}%";
            if (bossKillsText)
                bossKillsText.text = $"Boss Kills: {bossKills}";
            if (wavesText)
                wavesText.text = $"Waves Completed: {waves}";
            if (highestWaveText)
                highestWaveText.text = $"Highest Wave: {summary.highestWave}";
            if (baseScoreText)
                baseScoreText.text = $"Base Score: {baseScore:N0}";
            if (finalScoreText)
                finalScoreText.text = $"Final Score: {finalScore:N0}";

            yield return null;
        }

        // Ensure final values are exact
        DisplayStats(summary);
    }

    void DisplayStats(RunSummary summary)
    {
        if (killsText)
            killsText.text = $"Kills: {summary.totalKills}";
        if (headshotsText)
            headshotsText.text = $"Headshots: {summary.totalHeadshots}";
        if (accuracyText)
            accuracyText.text = $"Headshot Accuracy: {summary.headshotAccuracy:F1}%";
        if (bossKillsText)
            bossKillsText.text = $"Boss Kills: {summary.bossKills}";
        if (wavesText)
            wavesText.text = $"Waves Completed: {summary.wavesCompleted}";
        if (highestWaveText)
            highestWaveText.text = $"Highest Wave: {summary.highestWave}";
        if (baseScoreText)
            baseScoreText.text = $"Base Score: {summary.baseScore:N0}";
        if (finalScoreText)
            finalScoreText.text = $"FINAL SCORE: {summary.finalScore:N0}";
    }

    /// <summary>
    /// Hide the summary panel.
    /// </summary>
    public void HideSummary()
    {
        if (summaryPanel)
            summaryPanel.SetActive(false);
    }

    // Call this from a button or after delay to return to menu
    public void OnContinue()
    {
        HideSummary();
        // TODO: Load main menu or restart game
        Debug.Log("Return to menu or restart");
    }
}
