using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] float maxHealth = 100f;
    [SerializeField] bool startAtMaxHealth = true;
    [SerializeField] bool canGoNegative = false;

    [Header("Invulnerability")]
    [SerializeField] float invulnerabilityDuration = 0.25f;

    [Header("Death")]
    [SerializeField] bool destroyOnDeath = false;

    [Header("Events")]
    public UnityEvent<float, float> OnHealthChanged; // currentHealth, maxHealth
    public UnityEvent<float, Vector3, Vector3> OnDamaged; // amount, hitPoint, hitNormal
    public UnityEvent OnHealed;
    public UnityEvent OnDeath;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsInvulnerable => _invulnerabilityTimer > 0f;

    float _invulnerabilityTimer;

    void Awake()
    {
        CurrentHealth = startAtMaxHealth ? maxHealth : Mathf.Clamp(CurrentHealth, 0f, maxHealth);
        BroadcastHealth();
    }

    void Update()
    {
        if (_invulnerabilityTimer > 0f) _invulnerabilityTimer -= Time.deltaTime;
    }

    public void ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (IsDead || IsInvulnerable || amount <= 0f) return;

        CurrentHealth -= amount;
        if (!canGoNegative) CurrentHealth = Mathf.Max(0f, CurrentHealth);

        _invulnerabilityTimer = invulnerabilityDuration;

        OnDamaged?.Invoke(amount, hitPoint, hitNormal);
        BroadcastHealth();

        if (CurrentHealth <= 0f && !IsDead) Die();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;

        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
        OnHealed?.Invoke();
        BroadcastHealth();
    }

    public void SetMaxHealth(float newMax, bool refill = true)
    {
        maxHealth = Mathf.Max(1f, newMax);
        if (refill) CurrentHealth = maxHealth;
        else CurrentHealth = Mathf.Min(CurrentHealth, maxHealth);
        BroadcastHealth();
    }

    public void MakeInvulnerableFor(float seconds)
    {
        _invulnerabilityTimer = Mathf.Max(_invulnerabilityTimer, seconds);
    }

    void Die()
    {
        IsDead = true;
        OnDeath?.Invoke();
        BroadcastHealth();

        if (destroyOnDeath) Destroy(gameObject);
        // else: disable input, play death animation, etc. (not implemented here)
    }

    void BroadcastHealth()
    {
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    // Debug helpers
#if UNITY_EDITOR
    [ContextMenu("Debug: Take 10 Damage")]
    void DebugDamage() => ApplyDamage(10f, transform.position, Vector3.up);

    [ContextMenu("Debug: Heal 10 Health")]
    void DebugHeal() => Heal(10f);
#endif
}
