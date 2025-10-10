using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    public float maxHealth = 50f;
    public System.Action<EnemyHealth> OnDeath;
    float _hp;

    void Awake() { _hp = maxHealth; }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (_hp <= 0f) return;
        _hp -= amount;
        // TODO: hit FX / stagger
        if (_hp <= 0f) { OnDeath?.Invoke(this);  Destroy(gameObject); }
    }
}