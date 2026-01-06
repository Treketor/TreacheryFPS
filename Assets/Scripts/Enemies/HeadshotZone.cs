using UnityEngine;

/// <summary>
/// Marks a collider as a headshot zone for enemies.
/// When hit, it forwards damage to the enemy's IDamageable component with headshot flag.
/// Can be placed on parent enemy with a reference to the head position object.
/// </summary>
public class HeadshotZone : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The ragdoll head collider that should be treated as the headshot zone")]
    [SerializeField] Collider headCollider;
    [Tooltip("The enemy's main GameObject with IDamageable component (leave empty to use this object)")]
    [SerializeField] GameObject enemyRoot;
    
    [Header("Headshot Settings")]
    [Tooltip("Damage multiplier for headshots")]
    [SerializeField] float headshotDamageMultiplier = 2.0f;

    IDamageable _damageable;

    void Start()
    {
        // Auto-find enemy root if not assigned (use this object)
        if (!enemyRoot)
        {
            enemyRoot = gameObject;
        }

        // Get the IDamageable component from enemy root
        if (enemyRoot)
        {
            _damageable = enemyRoot.GetComponent<IDamageable>();
            if (_damageable == null)
            {
                Debug.LogWarning($"HeadshotZone on {gameObject.name}: enemyRoot has no IDamageable component!");
            }
        }

        // Validate head collider assignment
        if (headCollider == null)
        {
            Debug.LogWarning($"HeadshotZone on {gameObject.name}: No head collider assigned!");
        }
    }



    /// <summary>
    /// Called by weapon raycast when headshot zone is hit.
    /// Returns the damage multiplier and forwards to enemy's IDamageable.
    /// </summary>
    public bool ProcessHeadshot(float baseDamage, Vector3 hitPoint, Vector3 hitNormal, out float finalDamage)
    {
        return ProcessHeadshot(baseDamage, hitPoint, hitNormal, 400f, out finalDamage);
    }

    /// <summary>
    /// Called by weapon raycast when headshot zone is hit with bullet force.
    /// Returns the damage multiplier and forwards to enemy's IDamageable.
    /// </summary>
    public bool ProcessHeadshot(float baseDamage, Vector3 hitPoint, Vector3 hitNormal, float bulletForce, out float finalDamage)
    {
        if (_damageable != null)
        {
            finalDamage = baseDamage * headshotDamageMultiplier;
            
            // Debug headshot
            Debug.Log($"<color=red>HEADSHOT!</color> {enemyRoot.name} hit for {finalDamage:F1} damage (base: {baseDamage:F1} x{headshotDamageMultiplier})");
            
            // Mark enemy as killed by headshot
            if (enemyRoot && enemyRoot.TryGetComponent<EnemyHealth>(out var enemyHealth))
            {
                enemyHealth.MarkAsHeadshot();
                enemyHealth.TakeDamage(finalDamage, hitPoint, Vector3.zero, bulletForce);
            }
            else
            {
                _damageable.TakeDamage(finalDamage, hitPoint, Vector3.zero);
            }
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

    /// <summary>
    /// Check if the given collider is the head collider for this headshot zone.
    /// </summary>
    public bool IsHeadCollider(Collider collider)
    {
        return headCollider != null && collider == headCollider;
    }

    void OnDrawGizmos()
    {
        // Always show headshot zone in Scene view
        if (headCollider != null)
        {
            Gizmos.color = Color.red;
            Gizmos.matrix = headCollider.transform.localToWorldMatrix;
            
            // Draw wireframe based on collider type
            if (headCollider is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
                Gizmos.DrawSphere(sphere.center, sphere.radius);
            }
            else if (headCollider is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.center, box.size);
                Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (headCollider is CapsuleCollider capsule)
            {
                // Approximate capsule with sphere for simplicity
                Gizmos.DrawWireSphere(capsule.center, capsule.radius);
                Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
                Gizmos.DrawSphere(capsule.center, capsule.radius);
            }
            
            Gizmos.matrix = Matrix4x4.identity;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Show a brighter version when selected
        if (headCollider != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = headCollider.transform.localToWorldMatrix;
            
            // Draw yellow wireframe based on collider type
            if (headCollider is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            else if (headCollider is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (headCollider is CapsuleCollider capsule)
            {
                Gizmos.DrawWireSphere(capsule.center, capsule.radius);
            }
            
            Gizmos.matrix = Matrix4x4.identity;
            
            // Draw connection line from this object to head collider
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, headCollider.transform.position);
        }
    }
}
