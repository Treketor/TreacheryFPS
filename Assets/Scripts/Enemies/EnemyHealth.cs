using UnityEngine;
using Treachery.Weapons.Runtime;

// Ensures new enemies get the receiver automatically.
[RequireComponent(typeof(EnemyHitReceiver))]
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 50f;
    
    [Header("Enemy Type")]
    [Tooltip("Is this a boss enemy?")]
    public bool isBoss = false;
    
    [Header("Soul Drop")]
    [Tooltip("Prefab to spawn when enemy dies")]
    public GameObject soulPickupPrefab;
    [Tooltip("Number of souls to drop")]
    public int soulsToDropMin = 1;
    public int soulsToDropMax = 3;

    public System.Action<EnemyHealth> OnDeath;
    public System.Action<float, Vector3, Vector3> OnDamaged; // amount, hitPoint, hitNormal
    float _hp;
    bool _killedByHeadshot = false; // Track if killed by headshot
    bool _isDead = false; // Prevent multiple death processing
    float _lastBulletForce = 400f; // Store bullet force from weapon
    EnemyRagdoll _ragdoll;

    void Awake() 
    { 
        _hp = maxHealth; 
        _ragdoll = GetComponent<EnemyRagdoll>();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Auto-install the hit receiver on existing enemies in scenes/prefabs.
        if (GetComponent<EnemyHitReceiver>() == null)
        {
            UnityEditor.Undo.AddComponent<EnemyHitReceiver>(gameObject);
        }
    }
#endif

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        TakeDamage(amount, hitPoint, hitNormal, 400f); // Default bullet force
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal, float bulletForce)
    {
        if (_isDead) return; // Prevent damage after death
        
        // Store bullet force for death calculation
        _lastBulletForce = bulletForce;
        
        // Debug damage dealt
        Debug.Log($"<color=orange>{gameObject.name}</color> took <color=yellow>{amount:F1}</color> damage. HP: {_hp:F1} -> {(_hp - amount):F1}");
        
        _hp -= amount;
        OnDamaged?.Invoke(amount, hitPoint, hitNormal);
        // TODO: hit FX / stagger
        if (_hp <= 0f && !_isDead)
        {
            _isDead = true; // Mark as dead immediately to prevent multiple death calls
            Debug.Log($"<color=red>{gameObject.name} DIED!</color> (Final damage: {amount:F1})");
            OnDeath?.Invoke(this);
            RegisterKillInScoreSystem();
            SpawnSouls();
            Die(hitPoint, hitNormal, amount);
        }
    }

    /// <summary>
    /// Called by HeadshotZone to mark that this enemy was hit in the head.
    /// </summary>
    public void MarkAsHeadshot()
    {
        if (!_isDead) // Only mark headshot if not already dead
        {
            _killedByHeadshot = true;
        }
    }

    void RegisterKillInScoreSystem()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.RegisterKill(_killedByHeadshot, isBoss);
        }
    }

    void SpawnSouls()
    {
        if (soulPickupPrefab == null) return;

        int soulCount = Random.Range(soulsToDropMin, soulsToDropMax + 1);
        Vector3 spawnPosition = transform.position + Vector3.up * 1f; // Spawn above enemy center

        for (int i = 0; i < soulCount; i++)
        {
            GameObject soulObj = Instantiate(soulPickupPrefab, spawnPosition, Quaternion.identity);
            
            // Tell the soul to eject
            if (soulObj.TryGetComponent<SoulPickup>(out var soulPickup))
            {
                soulPickup.Eject(spawnPosition);
            }
        }
    }

    /// <summary>
    /// Handle enemy death with ragdoll integration
    /// </summary>
    void Die(Vector3 hitPoint, Vector3 hitDirection, float finalDamage)
    {
        // IMMEDIATELY remove zombie from game logic (for wave/scoring systems)
        RemoveFromGameSystems();
        
        if (_ragdoll != null)
        {
            // Calculate death force based on hit direction and damage
            Vector3 deathForce = CalculateDeathForce(hitDirection, finalDamage);
            
            // Pass death force to ragdoll
            _ragdoll.CreateDetachedRagdoll(deathForce, hitPoint);
            
            // IMMEDIATELY destroy this GameObject so WaveManager counts it as dead
            Destroy(gameObject, 0.1f); // Small delay to ensure ragdoll creation completes
        }
        else
        {
            // No ragdoll component - just destroy immediately
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Calculate appropriate death force based on hit direction and damage
    /// </summary>
    Vector3 CalculateDeathForce(Vector3 hitDirection, float damage)
    {
        // Normalize hit direction
        if (hitDirection == Vector3.zero)
        {
            hitDirection = Vector3.forward; // Default forward direction if no direction provided
        }
        else
        {
            hitDirection = hitDirection.normalized;
        }
        
        // Use weapon-specific bullet force with damage scaling
        float damageMultiplier = Mathf.Clamp01(damage / maxHealth) * 0.5f + 0.5f; // Scale from 0.5 to 1.0
        float forceStrength = _lastBulletForce * damageMultiplier;
        
        return hitDirection * forceStrength;
    }
    
    /// <summary>
    /// Remove zombie from all game tracking systems immediately
    /// </summary>
    void RemoveFromGameSystems()
    {
        // Notify any game systems that this enemy has been killed
        // You can add specific system notifications here as needed
        // For example:
        // - Wave management systems
        // - Enemy counters
        // - Achievement systems
        // etc.
        

    }


}