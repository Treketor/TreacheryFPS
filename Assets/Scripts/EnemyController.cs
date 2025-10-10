using UnityEngine;

// This script handles zombie melee attacks
// Movement is handled by HoardLocomotion script
public class EnemyController : MonoBehaviour
{
    public float meleeRange = 1.6f;
    public float meleeDamage = 10f;
    public float attackCooldown = 1f;

    Transform _player;
    float _cd;

    void Start() 
    { 
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
        }
    }

    void Update()
    {
        if (!_player) return;
        
        // Cooldown timer
        if (_cd > 0f) _cd -= Time.deltaTime;

        // Check if in melee range
        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist <= meleeRange && _cd <= 0f)
        {
            _cd = attackCooldown;
            if (_player.TryGetComponent<IDamageable>(out var damageable))
            {
                Vector3 direction = (_player.position - transform.position).normalized;
                damageable.TakeDamage(meleeDamage, _player.position, direction);
            }
        }
    }
}