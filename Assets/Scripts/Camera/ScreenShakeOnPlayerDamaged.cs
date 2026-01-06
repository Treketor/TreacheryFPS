using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Optional helper: attach to the Player (or anywhere) and point it at PlayerHealth.
/// It listens to PlayerHealth.OnDamaged and triggers ScreenShake.
/// </summary>
[DisallowMultipleComponent]
public class ScreenShakeOnPlayerDamaged : MonoBehaviour
{
    [SerializeField] PlayerHealth playerHealth;

    [Header("Mapping")]
    [Tooltip("How much shake amplitude per 1 damage point.")]
    [SerializeField] float amplitudePerDamage = 0.02f;

    [SerializeField] float maxAmplitude = 0.8f;

    [Header("Shake")]
    [SerializeField] float duration = 0.18f;
    [SerializeField] float frequency = 22f;
    [SerializeField] Vector3 positionStrength = new Vector3(0.06f, 0.06f, 0.02f);
    [SerializeField] Vector3 rotationStrength = new Vector3(1.8f, 1.2f, 1.2f);

    UnityAction<float, Vector3, Vector3> _handler;

    void Awake()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        _handler = OnDamaged;
    }

    void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnDamaged.AddListener(_handler);
    }

    void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnDamaged.RemoveListener(_handler);
    }

    void OnDamaged(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        float amp = Mathf.Clamp(amount * amplitudePerDamage, 0f, maxAmplitude);
        ScreenShake.Shake(amp, duration, frequency, positionStrength, rotationStrength);
    }
}
