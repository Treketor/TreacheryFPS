using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    public float meleeRange = 1.6f;
    public float meleeDamage = 10f;
    public float attackCooldown = 1f;

    NavMeshAgent _agent;
    Transform _player;
    float _cd;

    void Awake() { _agent = GetComponent<NavMeshAgent>(); }

    void Start() { _player = GameObject.FindGameObjectWithTag("Player").transform; }

    void Update()
    {
        if (!_player) return;
        _agent.SetDestination(_player.position);
        if (_cd > 0f) _cd -= Time.deltaTime;

        var dist = Vector3.Distance(transform.position, _player.position);
        if (dist <= meleeRange && _cd <= 0f)
        {
            _cd = attackCooldown;
            if (_player.TryGetComponent<IDamageable>(out var damageable))
                damageable.TakeDamage(meleeDamage, _player.position, (_player.position - transform.position).normalized);
        }
    }
}