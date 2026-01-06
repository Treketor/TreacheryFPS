using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Grave Digger enemy: slow/tanky mixed melee + ranged attacker.
// Requires: NavMeshAgent + Animator (usually on child).
public class GraveDiggerAI : MonoBehaviour
{
    enum State { SpawnDelay, Chasing, Attacking }
    enum AttackType { None, Melee, Ranged }

    [Header("Target")]
    [SerializeField] Transform player;
    [SerializeField] string playerTag = "Player";

    [Header("Movement")]
    [Tooltip("How often we refresh the NavMesh destination.")]
    [SerializeField] float destinationUpdateInterval = 0.1f;
    [SerializeField] bool faceTargetWhileAttacking = true;
    [SerializeField] float faceTargetTurnSpeed = 720f;

    [Header("Spawn")]
    [SerializeField] float spawnDelay = 0f;
    [SerializeField] bool freezeOnSpawn = true;

    [Header("Melee")]
    [SerializeField] float meleeRange = 1.8f;
    [SerializeField] float meleeDamage = 20f;
    [SerializeField] float meleeCooldown = 1.2f;
    [Tooltip("If you don't use animation events, this is how long we lock movement for.")]
    [SerializeField] float meleeAttackDuration = 0.75f;
    [Tooltip("Where melee hit is centered (optional).")]
    [SerializeField] Transform meleeHitPoint;
    [SerializeField] float meleeHitRadius = 1.2f;

    [Header("Ranged")]
    [Tooltip("Minimum distance to consider ranged attack (keeps it from throwing point-blank).")]
    [SerializeField] float rangedMinRange = 3.0f;
    [SerializeField] float rangedMaxRange = 14.0f;
    [SerializeField] float rangedDamage = 15f;
    [Tooltip("Ranged cooldown will be randomized between these values (seconds).")]
    [SerializeField] float rangedCooldownMin = 2.0f;
    [SerializeField] float rangedCooldownMax = 3.0f;
    [Tooltip("If you don't use animation events, this is how long we lock movement for.")]
    [SerializeField] float rangedAttackDuration = 1.0f;
    [Tooltip("Seconds after starting the ranged animation before the projectile is spawned.")]
    [SerializeField] float rangedThrowDelay = 0.45f;

    [Header("Projectile")]
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] Transform throwOrigin;
    [SerializeField] float projectileSpeed = 14f;
    [SerializeField] float projectileUpwardBoost = 2.5f;
    [SerializeField] float projectileLifetime = 6f;
    [Tooltip("Seconds after starting the ranged animation before the projectile becomes visible at the throw origin (held until thrown).")]
    [SerializeField] float rangedProjectileAppearDelay = 0.25f;

    [Header("Line of Sight")]
    [SerializeField] bool requireLineOfSightForRanged = false;
    [SerializeField] LayerMask lineOfSightMask = ~0;
    [SerializeField] float lineOfSightHeightOffset = 1.2f;

    [Header("Animator")]
    [Tooltip("Animator (can be on child). If empty we'll auto-find.")]
    [SerializeField] Animator animator;
    [Tooltip("Animator float parameter for movement speed (optional).")]
    [SerializeField] string speedParameter = "Speed";
    [Tooltip("Trigger name for melee attack.")]
    [SerializeField] string meleeTrigger = "Attack";
    [Tooltip("Animator integer parameter used to select between 2 melee attack animations (0 or 1).")]
    [SerializeField] string meleeAttackIntParameter = "AttackInt";
    [Tooltip("Trigger name for ranged throw.")]
    [SerializeField] string rangedTrigger = "RangedAttack";

    [Header("Death")]
    [Tooltip("Optional: assign the shovel GameObject here to disable it when the enemy dies.")]
    [SerializeField] GameObject shovelObjectToDisable;

    [Tooltip("Optional: assign a shovel prefab instance (usually disabled + parented) to enable and detach on death.")]
    [SerializeField] GameObject shovelObjectToEnableAndDetach;

    NavMeshAgent _agent;
    EnemyHealth _enemyHealth;

    State _state;
    float _spawnTimer;
    float _nextDestinationTime;
    float _meleeCd;
    float _rangedCd;

    Coroutine _attackRoutine;
    Coroutine _throwRoutine;
    Coroutine _appearRoutine;

    AttackType _currentAttackType = AttackType.None;
    bool _hasThrownThisAttack;
    bool _throwScheduled;
    bool _projectileAppeared;
    float _rangedAttackStartTime;

    int _lastMeleeAttackInt = -1;

    GameObject _heldProjectile;
    Rigidbody _heldProjectileRb;
    Collider[] _heldProjectileColliders;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _enemyHealth = GetComponent<EnemyHealth>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (_enemyHealth != null)
            _enemyHealth.OnDeath += _ => OnDied();

        _spawnTimer = spawnDelay;
        _state = spawnDelay > 0f ? State.SpawnDelay : State.Chasing;
    }

    void OnDestroy()
    {
        if (_enemyHealth != null)
            _enemyHealth.OnDeath -= _ => OnDied();
    }

    void Start()
    {
        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (_state == State.SpawnDelay && freezeOnSpawn)
            StopAgent();
    }

    void Update()
    {
        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
                player = playerObj.transform;
            else
                return;
        }

        if (_meleeCd > 0f) _meleeCd -= Time.deltaTime;
        if (_rangedCd > 0f) _rangedCd -= Time.deltaTime;

        switch (_state)
        {
            case State.SpawnDelay:
                UpdateSpawnDelay();
                break;
            case State.Attacking:
                UpdateAttacking();
                break;
            default:
                UpdateChasingAndAttacks();
                break;
        }

        UpdateAnimatorSpeed();
    }

    void UpdateSpawnDelay()
    {
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            _state = State.Chasing;
            ResumeAgent();
        }
        else
        {
            if (freezeOnSpawn)
                StopAgent();
        }
    }

    void UpdateAttacking()
    {
        if (faceTargetWhileAttacking)
            FaceTarget(player.position);

        StopAgent();
    }

    void UpdateChasingAndAttacks()
    {
        float dist = Vector3.Distance(transform.position, player.position);

        // Attack priority: melee if close, otherwise ranged if valid, otherwise chase.
        if (dist <= meleeRange)
        {
            if (_meleeCd <= 0f)
            {
                StartMeleeAttack();
                return;
            }
        }
        else if (dist >= rangedMinRange && dist <= rangedMaxRange)
        {
            if (_rangedCd <= 0f && projectilePrefab != null)
            {
                if (!requireLineOfSightForRanged || HasLineOfSight())
                {
                    StartRangedAttack();
                    return;
                }
            }
        }

        // Chase.
        ResumeAgent();
        if (Time.time >= _nextDestinationTime)
        {
            _nextDestinationTime = Time.time + destinationUpdateInterval;
            if (_agent != null && _agent.isOnNavMesh)
                _agent.SetDestination(player.position);
        }

        // Face direction of travel naturally via NavMeshAgent (default).
    }

    bool HasLineOfSight()
    {
        Vector3 from = transform.position + Vector3.up * lineOfSightHeightOffset;
        Vector3 to = player.position + Vector3.up * lineOfSightHeightOffset;
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (dist <= 0.01f) return true;

        dir /= dist;
        if (Physics.Raycast(from, dir, out RaycastHit hit, dist, lineOfSightMask, QueryTriggerInteraction.Ignore))
        {
            return hit.transform == player || hit.transform.IsChildOf(player);
        }

        return true;
    }

    void StartMeleeAttack()
    {
        _meleeCd = meleeCooldown;

        // Performing a melee attack should cancel/reset the ranged cooldown so the enemy
        // can't instantly throw as soon as the player steps out of melee range.
        ResetRangedCooldown();

        BeginAttack(AttackType.Melee);

        if (animator != null && !string.IsNullOrEmpty(meleeAttackIntParameter))
        {
            int attackInt = Random.Range(0, 2); // 0 or 1
            if (attackInt == _lastMeleeAttackInt)
                attackInt = 1 - attackInt;

            _lastMeleeAttackInt = attackInt;
            animator.SetInteger(meleeAttackIntParameter, attackInt);
        }

        if (animator != null && !string.IsNullOrEmpty(meleeTrigger))
            animator.SetTrigger(meleeTrigger);

        // Fallback: if no animation event is used, apply melee hit mid-way.
        if (_attackRoutine != null) StopCoroutine(_attackRoutine);
        _attackRoutine = StartCoroutine(MeleeAttackFallbackRoutine());
    }

    void ResetRangedCooldown()
    {
        float max = Mathf.Max(rangedCooldownMin, rangedCooldownMax);
        _rangedCd = Random.Range(rangedCooldownMin, max);
    }

    IEnumerator MeleeAttackFallbackRoutine()
    {
        StopAgent();
        if (faceTargetWhileAttacking)
            FaceTarget(player.position);

        // approximate hit time
        yield return new WaitForSeconds(meleeAttackDuration * 0.45f);
        AnimEvent_MeleeHit();

        yield return new WaitForSeconds(Mathf.Max(0.01f, meleeAttackDuration * 0.55f));
        EndAttack();
    }

    void StartRangedAttack()
    {
        BeginAttack(AttackType.Ranged);

        if (animator != null && !string.IsNullOrEmpty(rangedTrigger))
            animator.SetTrigger(rangedTrigger);

        if (_attackRoutine != null) StopCoroutine(_attackRoutine);
        if (_throwRoutine != null) StopCoroutine(_throwRoutine);
        _attackRoutine = StartCoroutine(RangedAttackFallbackRoutine());
    }

    IEnumerator RangedAttackFallbackRoutine()
    {
        StopAgent();
        if (faceTargetWhileAttacking)
            FaceTarget(player.position);

        // Throw after a fixed delay from attack start, regardless of whether animation events are used.
        RequestRangedThrow();

        // Keep the attack alive long enough for delayed appear/throw timings.
        float minAttackTime = Mathf.Max(rangedAttackDuration, rangedProjectileAppearDelay, rangedThrowDelay) + 0.05f;
        yield return new WaitForSeconds(Mathf.Max(0.01f, minAttackTime));
        EndAttack();
    }

    void BeginAttack(AttackType type)
    {
        _currentAttackType = type;
        _hasThrownThisAttack = false;
        _throwScheduled = false;
        _projectileAppeared = false;
        _state = State.Attacking;
        StopAgent();
        FaceTarget(player.position);

        if (type == AttackType.Ranged)
        {
            _rangedAttackStartTime = Time.time;
            RequestRangedProjectileAppear();
        }
    }

    void EndAttack()
    {
        if (_state != State.Attacking)
            return;

        if (_currentAttackType == AttackType.Ranged)
        {
            float max = Mathf.Max(rangedCooldownMin, rangedCooldownMax);
            _rangedCd = Random.Range(rangedCooldownMin, max);

            // If the ranged attack ends without throwing (early finish/cancel), remove the held dirt while on cooldown.
            if (!_hasThrownThisAttack)
                ClearHeldProjectile();
        }

        _attackRoutine = null;
        if (_throwRoutine != null)
        {
            StopCoroutine(_throwRoutine);
            _throwRoutine = null;
        }

        if (_appearRoutine != null)
        {
            StopCoroutine(_appearRoutine);
            _appearRoutine = null;
        }

        _currentAttackType = AttackType.None;
        _state = State.Chasing;
        ResumeAgent();
    }

    void StopAgent()
    {
        if (_agent == null || !_agent.isOnNavMesh) return;
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;
        _agent.ResetPath();
    }

    void ResumeAgent()
    {
        if (_agent == null || !_agent.isOnNavMesh) return;
        if (_agent.isStopped) _agent.isStopped = false;
    }

    void FaceTarget(Vector3 targetPosition)
    {
        Vector3 to = targetPosition - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(to.normalized);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, faceTargetTurnSpeed * Time.deltaTime);
    }

    void UpdateAnimatorSpeed()
    {
        if (animator == null || string.IsNullOrEmpty(speedParameter) || _agent == null)
            return;

        float speed = (_agent != null && !_agent.isStopped) ? _agent.velocity.magnitude : 0f;
        animator.SetFloat(speedParameter, speed);
    }

    void OnDied()
    {
        if (shovelObjectToDisable != null)
            shovelObjectToDisable.SetActive(false);

        if (shovelObjectToEnableAndDetach != null)
        {
            // Ensure it keeps its current world pose when detaching.
            shovelObjectToEnableAndDetach.SetActive(true);
            shovelObjectToEnableAndDetach.transform.SetParent(null, true);
        }

        enabled = false;
        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }

        if (_throwRoutine != null)
        {
            StopCoroutine(_throwRoutine);
            _throwRoutine = null;
        }

        if (_appearRoutine != null)
        {
            StopCoroutine(_appearRoutine);
            _appearRoutine = null;
        }

        ClearHeldProjectile();

        StopAgent();
    }

    // -------- Animation Events (recommended) --------

    /// <summary>
    /// Call this from the melee animation at the impact frame.
    /// </summary>
    public void AnimEvent_MeleeHit()
    {
        if (player == null) return;

        Vector3 center = meleeHitPoint != null ? meleeHitPoint.position : (transform.position + transform.forward * 1.0f + Vector3.up * 1.0f);

        Collider[] hits = Physics.OverlapSphere(center, meleeHitRadius, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Transform t = hits[i].transform;
            if (t == player || t.IsChildOf(player))
            {
                if (player.TryGetComponent<IDamageable>(out var dmg))
                {
                    Vector3 dir = (player.position - transform.position).normalized;
                    dmg.TakeDamage(meleeDamage, center, dir);
                }
                break;
            }
        }
    }

    /// <summary>
    /// Call this from the ranged animation at the throw frame.
    /// </summary>
    public void AnimEvent_ThrowProjectile()
    {
        // Supports animation events: will still enforce "not before rangedThrowDelay since attack start".
        RequestRangedThrow();
    }

    void RequestRangedThrow()
    {
        if (_state != State.Attacking || _currentAttackType != AttackType.Ranged)
            return;

        if (_throwScheduled || _hasThrownThisAttack)
            return;

        float elapsed = Time.time - _rangedAttackStartTime;
        float remaining = Mathf.Max(0f, rangedThrowDelay - elapsed);

        _throwScheduled = true;
        if (_throwRoutine != null)
            StopCoroutine(_throwRoutine);
        _throwRoutine = StartCoroutine(ThrowAfterDelay(remaining));
    }

    void RequestRangedProjectileAppear()
    {
        if (_state != State.Attacking || _currentAttackType != AttackType.Ranged)
            return;

        if (_projectileAppeared || _hasThrownThisAttack)
            return;

        if (_appearRoutine != null)
            StopCoroutine(_appearRoutine);
        _appearRoutine = StartCoroutine(AppearProjectileAfterDelay(rangedProjectileAppearDelay));
    }

    IEnumerator AppearProjectileAfterDelay(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (_state != State.Attacking || _currentAttackType != AttackType.Ranged)
            yield break;

        if (_projectileAppeared || _hasThrownThisAttack)
            yield break;

        EnsureHeldProjectile();
        _projectileAppeared = true;
    }

    IEnumerator ThrowAfterDelay(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (_state != State.Attacking || _currentAttackType != AttackType.Ranged)
            yield break;

        _hasThrownThisAttack = true;
        SpawnProjectileNow();
    }

    void SpawnProjectileNow()
    {
        if (projectilePrefab == null || player == null) return;

        Transform origin = GetThrowOrigin();
        Vector3 spawnPos = origin.position;

        Vector3 aimPos = player.position + Vector3.up * 1.2f;
        Vector3 dir = (aimPos - spawnPos);
        if (dir.sqrMagnitude < 0.001f) dir = transform.forward;
        dir.Normalize();

        Vector3 velocity = dir * projectileSpeed + Vector3.up * projectileUpwardBoost;

        GameObject go;
        if (_heldProjectile != null)
        {
            go = _heldProjectile;
            _heldProjectile = null;

            // Detach and place at the origin before throwing.
            go.transform.SetParent(null, true);
            go.transform.position = spawnPos;
            go.transform.rotation = Quaternion.LookRotation(dir);

            if (_heldProjectileColliders != null)
            {
                for (int i = 0; i < _heldProjectileColliders.Length; i++)
                {
                    if (_heldProjectileColliders[i] != null)
                        _heldProjectileColliders[i].enabled = true;
                }
            }

            if (_heldProjectileRb != null)
            {
                _heldProjectileRb.isKinematic = false;
                _heldProjectileRb.detectCollisions = true;
            }

            _heldProjectileRb = null;
            _heldProjectileColliders = null;
        }
        else
        {
            go = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(dir));
        }

        if (go.TryGetComponent<GraveDiggerProjectile>(out var proj))
        {
            proj.Launch(velocity, rangedDamage, projectileLifetime, gameObject);
        }
        else
        {
            // Fallback: try to drive Rigidbody directly.
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = velocity;
            Destroy(go, projectileLifetime);
        }
    }

    Transform GetThrowOrigin()
    {
        return throwOrigin != null ? throwOrigin : transform;
    }

    void EnsureHeldProjectile()
    {
        if (_heldProjectile != null)
            return;

        if (projectilePrefab == null)
            return;

        Transform origin = GetThrowOrigin();

        _heldProjectile = Instantiate(projectilePrefab, origin.position, origin.rotation, origin);
        _heldProjectile.transform.localPosition = Vector3.zero;
        _heldProjectile.transform.localRotation = Quaternion.identity;

        _projectileAppeared = true;

        _heldProjectileRb = _heldProjectile.GetComponent<Rigidbody>();
        if (_heldProjectileRb != null)
        {
            _heldProjectileRb.linearVelocity = Vector3.zero;
            _heldProjectileRb.angularVelocity = Vector3.zero;
            _heldProjectileRb.isKinematic = true;
            _heldProjectileRb.detectCollisions = false;
        }

        _heldProjectileColliders = _heldProjectile.GetComponentsInChildren<Collider>(true);
        if (_heldProjectileColliders != null)
        {
            for (int i = 0; i < _heldProjectileColliders.Length; i++)
            {
                if (_heldProjectileColliders[i] != null)
                    _heldProjectileColliders[i].enabled = false;
            }
        }
    }

    void ClearHeldProjectile()
    {
        if (_heldProjectile != null)
            Destroy(_heldProjectile);

        _heldProjectile = null;
        _heldProjectileRb = null;
        _heldProjectileColliders = null;
    }

    /// <summary>
    /// Optional animation event to end the attack early (if you don't want fallback timers).
    /// </summary>
    public void AnimEvent_AttackFinished()
    {
        if (_state == State.Attacking)
            EndAttack();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangedMinRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, rangedMaxRange);

        if (meleeHitPoint != null)
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(meleeHitPoint.position, meleeHitRadius);
        }
    }
}
