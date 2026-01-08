using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Wretch: stalker enemy that maintains distance, retreats if approached,
// then occasionally screeches and sprints in to attack aggressively.
[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class WretchStalkerAI : MonoBehaviour
{
    enum State
    {
        Stalking,
        Screeching,
        Frenzy,
        Dead,
    }

    [Header("Target")]
    [SerializeField] Transform target;
    [SerializeField] string playerTag = "Player";

    [Header("Spawn")]
    [Tooltip("Seconds to wait after spawn before the AI starts moving/acting.")]
    [SerializeField] float spawnDelay = 0f;

    [Header("Stalk Distance")]
    [Tooltip("Preferred distance band while stalking.")]
    [SerializeField] float stalkMinDistance = 8f;
    [SerializeField] float stalkMaxDistance = 14f;
    [Tooltip("If the player gets closer than this, retreat.")]
    [SerializeField] float retreatIfCloserThan = 6f;
    [Tooltip("How far to try to move away when retreating (randomized per retreat episode).")]
    [SerializeField] Vector2 retreatStepDistanceRange = new(6f, 10f);
    [Tooltip("Random left/right angle (degrees) applied to retreat direction per retreat episode.")]
    [SerializeField, Range(0f, 75f)] float retreatAngleOffsetDegrees = 25f;

    [Header("Facing")]
    [Tooltip("How fast the wretch turns to face the player while stalking and not moving.")]
    [SerializeField] float stalkFaceRotationSpeed = 540f;

    [Header("Stalk Timing")]
    [Tooltip("Random time window (seconds) before deciding to initiate an attack sequence.")]
    [SerializeField] Vector2 timeBetweenAttacks = new(6f, 12f);

    [Header("Screech")]
    [SerializeField] float screechDuration = 1.0f;

    [Header("Frenzy")]
    [Tooltip("How long the wretch stays in Frenzy before returning to stalking.")]
    [SerializeField] Vector2 frenzyDuration = new(4f, 8f);

    [Header("Movement")]
    [Header("Calm (Stalking)")]
    [SerializeField] float calmSpeed = 2.2f;
    [SerializeField] float calmAcceleration = 12f;
    [SerializeField] float calmAngularSpeed = 540f;

    [Header("Frenzy (Chasing/Attacking)")]
    [SerializeField] float frenzySpeed = 6.5f;
    [SerializeField] float frenzyAcceleration = 30f;
    [SerializeField] float frenzyAngularSpeed = 720f;

    [Header("Retreat (Disengage)")]
    [Tooltip("Used after frenzy ends, to quickly regain stalking distance.")]
    [SerializeField] float retreatSpeed = 5.5f;
    [SerializeField] float retreatAcceleration = 26f;
    [SerializeField] float retreatAngularSpeed = 720f;

    [Tooltip("How often to refresh SetDestination (seconds).")]
    [SerializeField] float destinationRefreshInterval = 0.15f;

    [Header("Attack")]
    [Tooltip("If within this distance, the wretch will still try to move closer (and will hold position once inside this range).")]
    [SerializeField] float meleeMinRange = 1.1f;
    [Tooltip("Maximum distance at which the wretch can perform melee attacks.")]
    [SerializeField] float meleeRange = 1.6f;
    [SerializeField] float meleeDamage = 14f;
    [SerializeField] float attackCooldown = 0.6f;
    [Tooltip("How long to stop moving during an attack beat.")]
    [SerializeField] float attackStopDuration = 0.15f;
    [Tooltip("Extra pause after an attack (adds on top of Attack Stop Duration).")]
    [SerializeField] float postAttackMovePause = 0.15f;
    [SerializeField] float attackRotationSpeed = 900f;

    [Header("Animation")]
    [Tooltip("Animator on the model (usually a child). If empty, auto-finds in children.")]
    [SerializeField] Animator animator;

    [Tooltip("Optional movement blend float parameter (Wretch.controller uses 'Blend').")]
    [SerializeField] string moveBlendFloat = "Blend";

    [Tooltip("Seconds to smooth/damp the movement blend value. 0 = no smoothing.")]
    [SerializeField] float moveBlendDampTime = 0.1f;

    [Tooltip("Optional trigger fired when screeching.")]
    [SerializeField] string screechTrigger = "Screech";

    [Tooltip("Optional trigger fired when attacking.")]
    [SerializeField] string attackTrigger = "Attack";

    [Tooltip("Optional int parameter for attack variation.")]
    [SerializeField] string attackIntParameter = "AttackInt";

    [Tooltip("How many attack animations/variants exist for AttackInt (values 0..N-1).")]
    [SerializeField, Min(1)] int attackIntVariants = 2;

    NavMeshAgent _agent;
    EnemyHealth _enemyHealth;

    State _state = State.Stalking;

    float _nextAttackTime;
    float _stateEndTime;
    float _attackCd;
    float _attackStopTimer;
    float _nextDestinationRefreshTime;

    bool _isSpawning;
    float _spawnReadyTime;

    bool _isRetreating;

    bool _hasRetreatRandom;
    float _retreatDistance;
    float _retreatAngleDeg;
    Vector3 _retreatDirection;
    bool _hasRetreatDestination;
    Vector3 _retreatDestination;

    int _lastAttackInt = -1;

    int _moveBlendHash;
    int _screechTriggerHash;
    int _attackTriggerHash;
    int _attackIntHash;

    bool _hasMoveBlend;
    bool _hasScreechTrigger;
    bool _hasAttackTrigger;
    bool _hasAttackInt;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _enemyHealth = GetComponent<EnemyHealth>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        CacheAnimatorParameters();

        if (_enemyHealth != null)
        {
            _enemyHealth.OnDeath += OnEnemyDeath;
            _enemyHealth.OnDamaged += OnEnemyDamaged;
        }
    }

    void Start()
    {
        if (target == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
                target = playerObj.transform;
        }

        ApplyCalmMovementSettings();

        if (spawnDelay > 0.01f)
        {
            _isSpawning = true;
            _spawnReadyTime = Time.time + spawnDelay;

            // Prevent immediate movement/pathing until the spawn delay ends.
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.velocity = Vector3.zero;
                _agent.ResetPath();
            }

            _nextAttackTime = float.PositiveInfinity;
        }
        else
        {
            ScheduleNextAttack();
        }
    }

    void OnDestroy()
    {
        if (_enemyHealth != null)
        {
            _enemyHealth.OnDeath -= OnEnemyDeath;
            _enemyHealth.OnDamaged -= OnEnemyDamaged;
        }
    }

    void OnEnemyDeath(EnemyHealth _)
    {
        OnDied();
    }

    void OnEnemyDamaged(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (_state == State.Dead) return;
        if (_isSpawning) return;

        // If the wretch is stalking (including retreating to maintain distance), taking damage
        // should immediately trigger an aggressive chase.
        if (_state == State.Stalking)
        {
            BeginFrenzy();
        }
    }

    void Update()
    {
        if (_state == State.Dead) return;
        if (target == null) return;

        if (_isSpawning)
        {
            if (Time.time < _spawnReadyTime)
            {
                if (_agent != null && _agent.isOnNavMesh)
                {
                    _agent.isStopped = true;
                    _agent.velocity = Vector3.zero;
                    _agent.ResetPath();
                }

                UpdateAnimatorMoveBlend();
                return;
            }

            _isSpawning = false;
            if (_agent != null && _agent.isOnNavMesh)
                _agent.isStopped = false;

            ScheduleNextAttack();
            _nextDestinationRefreshTime = 0f;
        }

        if (_attackCd > 0f) _attackCd -= Time.deltaTime;

        if (_attackStopTimer > 0f)
        {
            _attackStopTimer -= Time.deltaTime;
            FaceTarget(attackRotationSpeed);

            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.velocity = Vector3.zero;
            }
        }
        else
        {
            if (_agent != null && _agent.isOnNavMesh)
                _agent.isStopped = false;
        }

        switch (_state)
        {
            case State.Stalking:
                UpdateStalking();
                break;
            case State.Screeching:
                UpdateScreeching();
                break;
            case State.Frenzy:
                UpdateFrenzy();
                break;
        }

        UpdateAnimatorMoveBlend();
    }

    void UpdateStalking()
    {
        float dist = Vector3.Distance(transform.position, target.position);

        // If we just came out of frenzy, retreat quickly until we re-establish a safe stalking distance.
        if (_isRetreating)
        {
            if (dist >= stalkMinDistance)
            {
                _isRetreating = false;
                ClearRetreatRandomization();
                ApplyCalmMovementSettings();
            }
            else
            {
                EnsureRetreatDestination();
                return;
            }
        }

        // If we're not currently retreating/too-close, clear the cached randomization
        // so the next retreat picks a new direction and distance.
        if (dist >= stalkMinDistance && dist > retreatIfCloserThan)
            ClearRetreatRandomization();

        // Time to initiate attack sequence.
        if (Time.time >= _nextAttackTime)
        {
            BeginScreech();
            return;
        }

        // Retreat if player is too close.
        if (dist <= retreatIfCloserThan)
        {
            EnsureRetreatDestination();
            return;
        }

        // Maintain a distance band: if too far, close in; if within band, hover/hold.
        if (dist > stalkMaxDistance)
        {
            if (Time.time >= _nextDestinationRefreshTime)
            {
                _nextDestinationRefreshTime = Time.time + destinationRefreshInterval;
                SetDestination(target.position);
            }
        }
        else if (dist < stalkMinDistance)
        {
            if (Time.time >= _nextDestinationRefreshTime)
            {
                _nextDestinationRefreshTime = Time.time + destinationRefreshInterval;
                SetDestinationIfChanged(GetRetreatPoint(stalkMinDistance - dist + 1.0f), 1.0f);
            }
        }
        else
        {
            // In the band: stop updating destination to reduce path jitter.
            if (_agent != null && _agent.isOnNavMesh)
                _agent.ResetPath();

            // While stalking and not moving, keep facing the player.
            FaceTarget(stalkFaceRotationSpeed);
        }
    }

    void BeginScreech()
    {
        _state = State.Screeching;
        _stateEndTime = Time.time + Mathf.Max(0.05f, screechDuration);

        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
            _agent.ResetPath();
        }

        FaceTarget(attackRotationSpeed);

        if (_hasScreechTrigger)
            animator.SetTrigger(_screechTriggerHash);
    }

    void UpdateScreeching()
    {
        FaceTarget(attackRotationSpeed);

        if (Time.time >= _stateEndTime)
        {
            BeginFrenzy();
        }
    }

    void BeginFrenzy()
    {
        _state = State.Frenzy;
        _stateEndTime = Time.time + Random.Range(frenzyDuration.x, frenzyDuration.y);
        ApplyFrenzyMovementSettings();
        _isRetreating = false;

        if (_agent != null && _agent.isOnNavMesh)
            _agent.isStopped = false;

        // Immediately start closing in.
        _nextDestinationRefreshTime = 0f;
    }

    void UpdateFrenzy()
    {
        float dist = Vector3.Distance(transform.position, target.position);

        float minMelee = Mathf.Max(0f, Mathf.Min(meleeMinRange, meleeRange));
        float maxMelee = Mathf.Max(minMelee, meleeRange);

        // End frenzy after duration.
        if (Time.time >= _stateEndTime)
        {
            _state = State.Stalking;
            BeginRetreat();
            ScheduleNextAttack();
            return;
        }

        // Within melee max range: can attack, but try to close to the min melee range.
        // Once inside min range, hold position to avoid jitter/overlapping.
        if (dist <= maxMelee)
        {
            FaceTarget(attackRotationSpeed);

            if (_agent != null && _agent.isOnNavMesh)
            {
                if (dist <= minMelee)
                {
                    _agent.isStopped = true;
                    _agent.velocity = Vector3.zero;
                    _agent.ResetPath();
                }
                else
                {
                    _agent.isStopped = false;
                }
            }

            if (_attackStopTimer <= 0f && _attackCd <= 0f)
                PerformAttack();

            // If we're not yet at min melee range, keep trying to move closer.
            if (dist > minMelee)
            {
                if (Time.time >= _nextDestinationRefreshTime)
                {
                    _nextDestinationRefreshTime = Time.time + destinationRefreshInterval;
                    SetDestination(target.position);
                }
            }

            return;
        }

        // Out of melee range: chase hard.
        if (_agent != null && _agent.isOnNavMesh)
            _agent.isStopped = false;

        if (Time.time >= _nextDestinationRefreshTime)
        {
            _nextDestinationRefreshTime = Time.time + destinationRefreshInterval;
            SetDestination(target.position);
        }
    }

    void PerformAttack()
    {
        _attackCd = attackCooldown;
        _attackStopTimer = Mathf.Max(0f, attackStopDuration) + Mathf.Max(0f, postAttackMovePause);

        FaceTarget(attackRotationSpeed);

        if (animator != null)
        {
            if (_hasAttackInt)
            {
                int variants = Mathf.Max(1, attackIntVariants);
                int attackInt = (variants == 1) ? 0 : Random.Range(0, variants);
                if (variants > 1 && attackInt == _lastAttackInt)
                    attackInt = (attackInt + 1) % variants;
                _lastAttackInt = attackInt;

                animator.SetInteger(_attackIntHash, attackInt);
            }

            if (_hasAttackTrigger)
                animator.SetTrigger(_attackTriggerHash);
        }

        if (target.TryGetComponent<IDamageable>(out var damageable))
        {
            Vector3 direction = (target.position - transform.position).normalized;
            damageable.TakeDamage(meleeDamage, target.position, direction);
        }
    }

    void FaceTarget(float rotateSpeedDegPerSec)
    {
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.0001f) return;

        Quaternion desired = Quaternion.LookRotation(toTarget);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, rotateSpeedDegPerSec * Time.deltaTime);
    }

    void SetDestination(Vector3 worldPos)
    {
        if (_agent == null || !_agent.isOnNavMesh) return;

        if (TrySampleNavMesh(worldPos, out var hit))
            _agent.SetDestination(hit.position);
        else
            _agent.SetDestination(worldPos);
    }

    void SetDestinationIfChanged(Vector3 worldPos, float minChangeDistance = 0.25f)
    {
        if (_agent == null || !_agent.isOnNavMesh) return;

        Vector3 desired = worldPos;
        if (TrySampleNavMesh(worldPos, out var hit))
            desired = hit.position;

        // Avoid repeatedly re-setting the same (or nearly the same) destination,
        // which can cause visible stutter as paths get recalculated.
        if (_agent.hasPath)
        {
            Vector3 current = _agent.destination;
            current.y = desired.y;
            if ((current - desired).sqrMagnitude <= minChangeDistance * minChangeDistance)
                return;
        }

        _agent.SetDestination(desired);
    }

    Vector3 GetRetreatPoint(float stepDistance)
    {
        Vector3 away;
        if (_hasRetreatRandom)
        {
            away = _retreatDirection;
        }
        else
        {
            away = (transform.position - target.position);
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
                away = transform.forward;
            else
                away.Normalize();
        }

        return transform.position + away * Mathf.Max(0.5f, stepDistance);
    }

    void EnsureRetreatDestination()
    {
        if (_agent == null || !_agent.isOnNavMesh) return;

        // If we have a destination and it's still valid, keep it.
        if (_hasRetreatDestination)
        {
            if (_agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                _hasRetreatDestination = false;
            }
            else if (_agent.hasPath && !_agent.pathPending)
            {
                // Reached the retreat point but still need to keep backing off.
                if (_agent.remainingDistance <= 0.8f)
                    _hasRetreatDestination = false;
            }
        }

        if (_hasRetreatDestination)
        {
            // Only re-issue destination if something cleared the path.
            if (!_agent.hasPath && !_agent.pathPending)
                _agent.SetDestination(_retreatDestination);
            return;
        }

        // Pick a new retreat plan (distance + slight left/right) and compute a single navmesh-snapped destination.
        ClearRetreatRandomization();
        EnsureRetreatRandomization();

        Vector3 desired = transform.position + _retreatDirection * Mathf.Max(0.5f, _retreatDistance);
        if (TrySampleNavMesh(desired, out var hit))
            _retreatDestination = hit.position;
        else
            _retreatDestination = desired;

        _hasRetreatDestination = true;
        _agent.SetDestination(_retreatDestination);
    }

    void EnsureRetreatRandomization()
    {
        if (_hasRetreatRandom) return;

        float min = Mathf.Max(0.1f, retreatStepDistanceRange.x);
        float max = Mathf.Max(min, retreatStepDistanceRange.y);
        _retreatDistance = Random.Range(min, max);

        float maxAngle = Mathf.Max(0f, retreatAngleOffsetDegrees);
        _retreatAngleDeg = (maxAngle <= 0.01f) ? 0f : Random.Range(-maxAngle, maxAngle);

        // Cache the retreat direction so it doesn't jitter as the player moves.
        Vector3 away = (transform.position - target.position);
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f)
            away = transform.forward;
        else
            away.Normalize();

        if (_retreatAngleDeg != 0f)
            away = Quaternion.AngleAxis(_retreatAngleDeg, Vector3.up) * away;

        _retreatDirection = away;

        _hasRetreatRandom = true;
    }

    void ClearRetreatRandomization()
    {
        _hasRetreatRandom = false;
        _retreatDirection = Vector3.zero;
        _hasRetreatDestination = false;
    }

    static bool TrySampleNavMesh(Vector3 pos, out NavMeshHit hit)
    {
        return NavMesh.SamplePosition(pos, out hit, 2.0f, NavMesh.AllAreas);
    }

    void ApplyStalkMovementSettings()
    {
        if (_agent == null) return;
        _agent.speed = calmSpeed;
        _agent.acceleration = calmAcceleration;
        _agent.angularSpeed = calmAngularSpeed;
        _agent.stoppingDistance = 0f;
        _agent.autoBraking = false;
    }

    void ApplyCalmMovementSettings() => ApplyStalkMovementSettings();

    void ApplyFrenzyMovementSettings()
    {
        if (_agent == null) return;
        _agent.speed = frenzySpeed;
        _agent.acceleration = frenzyAcceleration;
        _agent.angularSpeed = frenzyAngularSpeed;

        float minMelee = Mathf.Max(0f, Mathf.Min(meleeMinRange, meleeRange));
        _agent.stoppingDistance = minMelee;
        _agent.autoBraking = false;
    }

    void ApplyRetreatMovementSettings()
    {
        if (_agent == null) return;
        _agent.speed = retreatSpeed;
        _agent.acceleration = retreatAcceleration;
        _agent.angularSpeed = retreatAngularSpeed;
        _agent.stoppingDistance = 0f;
        _agent.autoBraking = false;
    }

    void BeginRetreat()
    {
        _isRetreating = true;
        ApplyRetreatMovementSettings();
        ClearRetreatRandomization();
        _nextDestinationRefreshTime = 0f;
    }

    void ScheduleNextAttack()
    {
        float min = Mathf.Max(0.1f, timeBetweenAttacks.x);
        float max = Mathf.Max(min, timeBetweenAttacks.y);
        _nextAttackTime = Time.time + Random.Range(min, max);
    }

    void CacheAnimatorParameters()
    {
        if (animator == null) return;

        _moveBlendHash = Animator.StringToHash(moveBlendFloat);
        _screechTriggerHash = Animator.StringToHash(screechTrigger);
        _attackTriggerHash = Animator.StringToHash(attackTrigger);
        _attackIntHash = Animator.StringToHash(attackIntParameter);

        var parameters = animator.parameters;
        _hasMoveBlend = HasParameter(parameters, moveBlendFloat, AnimatorControllerParameterType.Float);
        _hasScreechTrigger = HasParameter(parameters, screechTrigger, AnimatorControllerParameterType.Trigger);
        _hasAttackTrigger = HasParameter(parameters, attackTrigger, AnimatorControllerParameterType.Trigger);
        _hasAttackInt = HasParameter(parameters, attackIntParameter, AnimatorControllerParameterType.Int);
    }

    static bool HasParameter(AnimatorControllerParameter[] parameters, string name, AnimatorControllerParameterType type)
    {
        if (string.IsNullOrEmpty(name)) return false;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == type && parameters[i].name == name)
                return true;
        }
        return false;
    }

    void UpdateAnimatorMoveBlend()
    {
        if (animator == null || !_hasMoveBlend) return;

        float moveSpeed = 0f;
        if (_agent != null && _agent.isOnNavMesh)
        {
            moveSpeed = _agent.velocity.magnitude;
        }

        if (moveBlendDampTime > 0f)
            animator.SetFloat(_moveBlendHash, moveSpeed, moveBlendDampTime, Time.deltaTime);
        else
            animator.SetFloat(_moveBlendHash, moveSpeed);
    }

    void OnDied()
    {
        _state = State.Dead;
        if (_agent != null)
        {
            if (_agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
            _agent.enabled = false;
        }

        enabled = false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position;
        center.y = transform.position.y;

        // Attempt to locate the player in-editor if not assigned.
        Transform debugTarget = target;
        if (debugTarget == null && !string.IsNullOrEmpty(playerTag))
        {
            var playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
                debugTarget = playerObj.transform;
        }

        float minStalk = Mathf.Max(0f, stalkMinDistance);
        float maxStalk = Mathf.Max(minStalk, stalkMaxDistance);
        float retreatDist = Mathf.Max(0f, retreatIfCloserThan);
        float meleeMin = Mathf.Max(0f, Mathf.Min(meleeMinRange, meleeRange));
        float meleeMax = Mathf.Max(meleeMin, meleeRange);

        // Draw distance rings around the Wretch.
        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

        Handles.color = new Color(1f, 0.25f, 0.25f, 0.9f); // retreat
        Handles.DrawWireDisc(center, Vector3.up, retreatDist);
        Handles.Label(center + Vector3.up * 0.25f + Vector3.forward * retreatDist, $"Retreat <= {retreatDist:0.##}m");

        Handles.color = new Color(1f, 0.85f, 0.2f, 0.85f); // stalk min
        Handles.DrawWireDisc(center, Vector3.up, minStalk);
        Handles.Label(center + Vector3.up * 0.25f + Vector3.right * minStalk, $"Stalk Min {minStalk:0.##}m");

        Handles.color = new Color(0.2f, 1f, 0.2f, 0.85f); // stalk max
        Handles.DrawWireDisc(center, Vector3.up, maxStalk);
        Handles.Label(center + Vector3.up * 0.25f + Vector3.left * maxStalk, $"Stalk Max {maxStalk:0.##}m");

        Handles.color = new Color(0.2f, 0.9f, 1f, 0.85f); // melee min
        Handles.DrawWireDisc(center, Vector3.up, meleeMin);
        Handles.Label(center + Vector3.up * 0.25f + Vector3.back * meleeMin, $"Melee Min {meleeMin:0.##}m");

        Handles.color = new Color(1f, 0.2f, 1f, 0.85f); // melee max
        Handles.DrawWireDisc(center, Vector3.up, meleeMax);
        Handles.Label(center + Vector3.up * 0.25f + Vector3.back * meleeMax, $"Melee Max {meleeMax:0.##}m");

        // If we have a target, also draw a line and current distance.
        if (debugTarget != null)
        {
            Vector3 targetPos = debugTarget.position;
            Handles.color = new Color(1f, 1f, 1f, 0.6f);
            Handles.DrawDottedLine(center, targetPos, 4f);

            float dist = Vector3.Distance(center, targetPos);
            Handles.Label((center + targetPos) * 0.5f + Vector3.up * 0.25f, $"Player dist: {dist:0.##}m");
        }
    }
#endif
}
