using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class HoardLocomotion : MonoBehaviour
{
    [Header("Targets")]
    public Transform player;

    [Header("Speeds")]
    public float moveSpeed = 3.2f;
    public float rotationSpeed = 720f; // degrees per second

    [Header("Separation")]
    public float separationRadius = 0.9f;
    public float separationWeight = 6f;
    public LayerMask separationMask; // Enemy Layer

    [Header("Slotting Around Player")]
    public float slotRadius = 1.5f; // Radius around player to slot into
    public float slotWeight = 0.45f; // How strongly to move towards slot position
    [Range(0f, 1f)] public float slotResponsiveness = 0.15f; // How quickly to adjust slot position

    [Header("Side Bias")]
    [Range(0f, 1f)] public float lateralBias = 0.25f;

    NavMeshAgent _agent;
    Vector3 _velocity;
    float _slotAngle; // current target angle around player
    float _slotAngleGoal; // desired angle we ease towards
    int _idHash;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.updatePosition = false;
        _agent.updateRotation = false;
        _agent.autoBraking = false;

        // Unique, stable angle seed per enemy
        _idHash = Mathf.Abs(GetInstanceID());
        _slotAngle = _slotAngleGoal = (_idHash % 360) * Mathf.Deg2Rad;
    }

    void Start()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
    }

    void Update()
    {
        if (!player) return;

        _agent.SetDestination(player.position);
        Vector3 desired = _agent.desiredVelocity;

        Vector3 toPlayer = player.position - transform.position;
        Vector2 toPlayer2 = new(toPlayer.x, toPlayer.z);
        float baseAngle = Mathf.Atan2(toPlayer2.y, toPlayer2.x);

        float angleOffset = (_idHash * 0.61803398875f) % (2f * Mathf.PI); // golden ratio offset
        _slotAngleGoal = baseAngle + angleOffset;

        _slotAngle = Mathf.LerpAngle(_slotAngle, _slotAngleGoal, slotResponsiveness);

        Vector3 slotDir = new(Mathf.Cos(_slotAngle), 0f, Mathf.Sin(_slotAngle));
        Vector3 slotPos = player.position - slotDir * slotRadius;
        Vector3 toSlot = (slotPos - transform.position);
        Vector3 slotSteer = toSlot.normalized * moveSpeed;

        Vector3 separation = Vector3.zero;
        int hits = 0;
        var center = transform.position + Vector3.up * 0.25f;
        Collider[] cols = Physics.OverlapSphere(center, separationRadius, separationMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < cols.Length; i++)
        {
            var other = cols[i];
            if (other.attachedRigidbody && other.attachedRigidbody.gameObject == gameObject) continue;
            Vector3 away = transform.position - other.ClosestPoint(transform.position);
            float dist = away.magnitude + 0.0001f;
            separation += away / (dist * dist);
            hits++;
        }
        if (hits > 0) separation = separation.normalized * moveSpeed;

        Vector3 fwd = desired.sqrMagnitude > 0.001f ? desired.normalized : transform.forward;
        Vector3 right = new Vector3(fwd.z, 0f, -fwd.x);

        float sideSign = ((_idHash & 1) == 0) ? 1f : -1f;
        Vector3 side = right * sideSign * moveSpeed * lateralBias;

        Vector3 steer =
            desired.normalized * moveSpeed * (1f - slotWeight) +
            slotSteer * slotWeight +
            separation * (separationWeight / Mathf.Max(1f, hits)) +
            side;

        Vector3 step = steer * Time.deltaTime;
        if (step.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(step.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        Vector3 nextPos = transform.position + step;
        _agent.nextPosition = nextPos;
        transform.position = nextPos;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.6f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.25f, separationRadius);
        if (player)
        {
            // draw slot position
            Vector3 slotDir = new Vector3(Mathf.Cos(_slotAngle), 0f, Mathf.Sin(_slotAngle));
            Vector3 slotPos = player.position - slotDir * slotRadius;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, slotPos);
            Gizmos.DrawWireSphere(slotPos, 0.15f);
        }
    }
#endif
}