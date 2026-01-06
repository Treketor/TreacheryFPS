using UnityEngine;
using UnityEngine.UI;

public class UIHealthBar : MonoBehaviour
{
    [SerializeField] Image fill;
    [SerializeField] PlayerHealth playerHealth;

    void Start()
    {
        if (!playerHealth) playerHealth = FindFirstObjectByType<PlayerHealth>();
        playerHealth.OnHealthChanged.AddListener(OnHealthChanged);
        OnHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    void OnDestroy()
    {
        if (playerHealth) playerHealth.OnHealthChanged.RemoveListener(OnHealthChanged);
    }

    void OnHealthChanged(float current, float max)
    {
        fill.fillAmount = Mathf.Approximately(max, 0f) ? 0f : current / max;
    }
}