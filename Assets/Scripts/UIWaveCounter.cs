using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class UIWaveCounter : MonoBehaviour
{
    [SerializeField] TMP_Text waveText;
    [SerializeField] WaveManager waves;

    void Start()
    {
        if (!waveText) waveText = GetComponent<TMP_Text>();
        if (!waves) waves = FindFirstObjectByType<WaveManager>();

        if (!waves || !waveText)
        {
            Debug.LogWarning("[UIWaveCounter] Missing references, disabling", this);
            enabled = false;
            return;
        }

        waves.OnWaveChanged += HandleWaveChanged;
        HandleWaveChanged(waves.CurrentWaveIndex);
    }

    void OnDestroy()
    {
        if (waves != null) waves.OnWaveChanged -= HandleWaveChanged;
    }

    void HandleWaveChanged(int waveIndex)
    {
        waveText.text = $"{waveIndex + 1}";
    }
}