using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
public class DamageableProxy_Players : MonoBehaviour, IDamageable
{
    PlayerHealth _health;

    void Awake() => _health = GetComponent<PlayerHealth>();

    // Called by weapons/enemies via IDamageable interface
    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        _health.ApplyDamage(amount, hitPoint, hitNormal);
    }
}