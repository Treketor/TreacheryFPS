using UnityEngine;

/// <summary>
/// Marks a collider as a headshot zone for enemies.
/// When hit, it forwards damage to the enemy's IDamageable component with headshot flag.
/// Can be placed on parent enemy with a reference to the head position object.
/// </summary>
public class HeadshotZone : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Child GameObject that represents the head position (will get the collider)")]
    [SerializeField] GameObject headObject;
    [Tooltip("The enemy's main GameObject with IDamageable component (leave empty to use this object)")]
    [SerializeField] GameObject enemyRoot;
    
    [Header("Headshot Settings")]
    [Tooltip("Damage multiplier for headshots")]
    [SerializeField] float headshotDamageMultiplier = 2.0f;
    [Tooltip("Radius of the headshot zone collider")]
    [SerializeField] float headshotRadius = 0.3f;

    IDamageable _damageable;
    Collider _headshotCollider;

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

        // Setup headshot collider
        SetupHeadshotCollider();
    }

    void SetupHeadshotCollider()
    {
        if (headObject != null)
        {
            // Add collider to the head object if it doesn't have one
            _headshotCollider = headObject.GetComponent<Collider>();
            if (_headshotCollider == null)
            {
                // Create a sphere collider on the head object
                SphereCollider sphereCol = headObject.AddComponent<SphereCollider>();
                sphereCol.radius = headshotRadius;
                sphereCol.isTrigger = true;
                _headshotCollider = sphereCol;
            }
            else
            {
                // Ensure existing collider is a trigger
                _headshotCollider.isTrigger = true;
            }

            // Don't create additional HeadshotZone components - the weapon system will find this one via GetComponentInParent
        }
        else
        {
            Debug.LogWarning($"HeadshotZone on {gameObject.name}: No head object assigned!");
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
        if (headObject == null || _headshotCollider == null)
            return false;
            
        return collider == _headshotCollider;
    }

    void OnDrawGizmos()
    {
        // Always show headshot zone in Scene view
        if (headObject != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(headObject.transform.position, headshotRadius);
            
            // Draw a slightly transparent filled sphere for better visibility
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawSphere(headObject.transform.position, headshotRadius);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Show a brighter version when selected
        if (headObject != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(headObject.transform.position, headshotRadius);
            
            // Draw connection line from this object to head object
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, headObject.transform.position);
        }
    }
}
