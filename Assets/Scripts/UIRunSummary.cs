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

    void Start()
    {
        // Hide panel initially
        if (summaryPanel)
            summaryPanel.SetActive(false);

        // Subscribe to player death event
        // You'll need to hook this up to your PlayerHealth OnDeath event
    }

    /// <summary>
    /// Show the summary screen with current run stats.
    /// </summary>
    public void ShowSummary(bool wasSuccessful = false)
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("UIRunSummary: ScoreManager not found!");
            return;
        }

        StartCoroutine(ShowSummaryCoroutine(wasSuccessful));
    }

    IEnumerator ShowSummaryCoroutine(bool wasSuccessful)
    {
        yield return new WaitForSeconds(delayBeforeShow);

        RunSummary summary = ScoreManager.Instance.GetRunSummary();

        if (summaryPanel)
            summaryPanel.SetActive(true);

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
