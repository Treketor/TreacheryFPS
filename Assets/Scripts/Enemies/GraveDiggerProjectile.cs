using UnityEngine;

/// <summary>
/// Simple physics projectile for GraveDigger ranged attack.
/// Attach to the projectile prefab alongside a Rigidbody + Collider.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class GraveDiggerProjectile : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] float damage = 15f;

    [Header("Lifetime")]
    [SerializeField] float lifetimeSeconds = 6f;

    [Header("Impact")]
    [SerializeField] bool destroyOnHit = true;
    [SerializeField] GameObject impactEffectPrefab;
    [SerializeField] float impactEffectLifetime = 2f;

    Rigidbody _rb;
    GameObject _owner;
    bool _launched;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void Launch(Vector3 velocity, float damageAmount, float lifetime, GameObject owner)
    {
        damage = damageAmount;
        lifetimeSeconds = lifetime;
        _owner = owner;

        if (_rb == null)
            _rb = GetComponent<Rigidbody>();

        _rb.linearVelocity = velocity;
        _launched = true;

        Destroy(gameObject, lifetimeSeconds);
    }

    void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.collider, collision.GetContact(0).point, collision.GetContact(0).normal);
    }

    void OnTriggerEnter(Collider other)
    {
        HandleHit(other, transform.position, Vector3.up);
    }

    void HandleHit(Collider other, Vector3 point, Vector3 normal)
    {
        if (!_launched) return;
        if (other == null) return;

        // Ignore owner
        if (_owner != null && (other.gameObject == _owner || other.transform.IsChildOf(_owner.transform)))
            return;

        // Damage player if present
        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            Vector3 dir = (_rb != null && _rb.linearVelocity.sqrMagnitude > 0.01f) ? _rb.linearVelocity.normalized : transform.forward;
            damageable.TakeDamage(damage, point, dir);
        }

        if (impactEffectPrefab != null)
        {
            var fx = Instantiate(impactEffectPrefab, point, Quaternion.LookRotation(normal));
            Destroy(fx, impactEffectLifetime);
        }

        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }
}
