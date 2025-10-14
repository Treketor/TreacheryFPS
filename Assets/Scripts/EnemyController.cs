using UnityEngine;
using UnityEngine.AI;

// This script handles zombie melee attacks
// Movement is handled by HoardLocomotion script
public class EnemyController : MonoBehaviour
{
    [Header("Attack Settings")]
    public float meleeRange = 1.6f;
    public float meleeDamage = 10f;
    public float attackCooldown = 1f;
    [Tooltip("How long the enemy stops moving during attack animation")]
    public float attackStopDuration = 0.5f;
    [Tooltip("How fast the enemy rotates to face player during attack")]
    public float attackRotationSpeed = 720f;

    Transform _player;
    NavMeshAgent _agent;
    HoardLocomotion _locomotion;
    float _cd;
    float _attackStopTimer;
    bool _isAttacking;

    public bool IsInAttackPosition { get; private set; }

    void Start() 
    { 
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
        }

        _agent = GetComponent<NavMeshAgent>();
        _locomotion = GetComponent<HoardLocomotion>();
    }

    void Update()
    {
        if (!_player) return;
        
        // Cooldown timer
        if (_cd > 0f) _cd -= Time.deltaTime;

        // Check if in melee range
        float dist = Vector3.Distance(transform.position, _player.position);
        IsInAttackPosition = dist <= meleeRange;

        // Notify locomotion to stop moving if in attack position
        if (_locomotion != null)
        {
            _locomotion.SetInAttackPosition(IsInAttackPosition);
        }

        // Attack stop timer
        if (_attackStopTimer > 0f)
        {
            _attackStopTimer -= Time.deltaTime;
            
            // Keep facing the player during attack
            FacePlayer();
            
            // Stop movement during attack
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
            }
        }
        else
        {
            // Resume movement if not in attack position
            if (_agent != null && _agent.isOnNavMesh && _isAttacking)
            {
                _agent.isStopped = false;
                _isAttacking = false;
            }
        }

        // Perform attack if in range and ready
        if (IsInAttackPosition && _cd <= 0f)
        {
            PerformAttack();
        }
    }

    void PerformAttack()
    {
        _cd = attackCooldown;
        _attackStopTimer = attackStopDuration;
        _isAttacking = true;

        // Face player immediately when starting attack
        FacePlayer();

        if (_player.TryGetComponent<IDamageable>(out var damageable))
        {
            Vector3 direction = (_player.position - transform.position).normalized;
            damageable.TakeDamage(meleeDamage, _player.position, direction);
        }
    }

    void FacePlayer()
    {
        if (_player == null) return;

        // Calculate direction to player (only on horizontal plane)
        Vector3 directionToPlayer = _player.position - transform.position;
        directionToPlayer.y = 0f;

        if (directionToPlayer.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, 
                targetRotation, 
                attackRotationSpeed * Time.deltaTime
            );
        }
    }
}