using UnityEngine;

/// <summary>
/// Marks a collider as a headshot zone for enemies.
/// When hit, it forwards damage to the enemy's IDamageable component with headshot flag.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HeadshotZone : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The enemy's main GameObject with IDamageable component")]
    [SerializeField] GameObject enemyRoot;
    
    [Header("Headshot Settings")]
    [Tooltip("Damage multiplier for headshots")]
    [SerializeField] float headshotDamageMultiplier = 2.0f;

    IDamageable _damageable;

    void Start()
    {
        // Auto-find enemy root if not assigned
        if (!enemyRoot)
        {
            // Try to find parent with IDamageable
            Transform current = transform.parent;
            while (current != null)
            {
                if (current.TryGetComponent<IDamageable>(out _))
                {
                    enemyRoot = current.gameObject;
                    break;
                }
                current = current.parent;
            }
        }

        if (enemyRoot)
        {
            _damageable = enemyRoot.GetComponent<IDamageable>();
            if (_damageable == null)
            {
                Debug.LogWarning($"HeadshotZone on {gameObject.name}: enemyRoot has no IDamageable component!");
            }
        }
        else
        {
            Debug.LogWarning($"HeadshotZone on {gameObject.name}: No enemy root assigned or found!");
        }

        // Ensure collider is set as trigger
        var col = GetComponent<Collider>();
        if (col && !col.isTrigger)
        {
            Debug.LogWarning($"HeadshotZone on {gameObject.name}: Collider should be a trigger!");
        }
    }

    /// <summary>
    /// Called by weapon raycast when headshot zone is hit.
    /// Returns the damage multiplier and forwards to enemy's IDamageable.
    /// </summary>
    public bool ProcessHeadshot(float baseDamage, Vector3 hitPoint, Vector3 hitNormal, out float finalDamage)
    {
        if (_damageable != null)
        {
            finalDamage = baseDamage * headshotDamageMultiplier;
            
            // Mark enemy as killed by headshot
            if (enemyRoot && enemyRoot.TryGetComponent<EnemyHealth>(out var enemyHealth))
            {
                enemyHealth.MarkAsHeadshot();
            }
            
            _damageable.TakeDamage(finalDamage, hitPoint, hitNormal);
            return true;
        }

        finalDamage = baseDamage;
        return false;
    }

    /// <summary>
    /// Get the damage multiplier for headshots.
    /// </summary>
    public float GetHeadshotMultiplier()
    {
        return headshotDamageMultiplier;
    }

    void OnDrawGizmosSelected()
    {
        // Visualize headshot zone
        Gizmos.color = Color.red;
        var col = GetComponent<Collider>();
        if (col is SphereCollider sphere)
        {
            Gizmos.DrawWireSphere(transform.position, sphere.radius * transform.lossyScale.x);
        }
        else if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
