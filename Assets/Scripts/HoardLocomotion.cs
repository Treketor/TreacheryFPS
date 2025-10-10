using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class HoardLocomotion : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Movement")]
    [Tooltip("Walk speed of the zombie")]
    public float walkSpeed = 2.5f;
    [Tooltip("How fast the zombie rotates")]
    public float rotationSpeed = 180f;
    [Tooltip("Zombies slow down near player for better melee")]
    public float slowdownDistance = 2f;
    [Tooltip("Minimum speed when near player")]
    public float minSpeed = 0.8f;

    [Header("Separation (Anti-Clumping)")]
    [Tooltip("How far to check for nearby zombies")]
    public float separationRadius = 1.2f;
    [Tooltip("How strong the push-away force is")]
    public float separationStrength = 2f;
    [Tooltip("Layer mask for other zombies")]
    public LayerMask zombieLayer;

    [Header("NavMesh Settings")]
    [Tooltip("Stopping distance from target")]
    public float stoppingDistance = 1.5f;

    private NavMeshAgent agent;
    private Vector3 currentVelocity;
    private int zombieID;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        
        // Configure NavMeshAgent for COD Zombies style movement
        agent.speed = walkSpeed;
        agent.angularSpeed = rotationSpeed;
        agent.acceleration = 8f;
        agent.stoppingDistance = stoppingDistance;
        agent.autoBraking = true;
        agent.updateRotation = true;
        agent.updatePosition = true;
        
        // Avoidance settings - varied priorities prevent deadlocks
        agent.avoidancePriority = Random.Range(30, 70);
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.radius = 0.5f;
        
        // Unique ID for this zombie
        zombieID = GetInstanceID();
    }

    void Start()
    {
        // Find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
    }

    void Update()
    {
        if (player == null || agent == null || !agent.isOnNavMesh)
            return;

        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Calculate separation force FIRST
        Vector3 separationForce = CalculateSeparation();

        // Apply separation by offsetting the target position
        Vector3 targetPosition = player.position + separationForce;
        
        // Sample to ensure it's on the NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, 2f, agent.areaMask))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // If separation target is off-mesh, just go to player
            agent.SetDestination(player.position);
        }

        // Apply speed based on distance (slow down when close for better melee)
        float speedMultiplier = Mathf.Lerp(minSpeed, 1f, Mathf.InverseLerp(0f, slowdownDistance, distanceToPlayer));
        agent.speed = walkSpeed * speedMultiplier;
    }

    Vector3 CalculateSeparation()
    {
        Vector3 separationForce = Vector3.zero;
        int neighborCount = 0;

        // Find nearby zombies
        Collider[] nearbyZombies = Physics.OverlapSphere(transform.position, separationRadius, zombieLayer);

        foreach (Collider col in nearbyZombies)
        {
            // Skip self
            if (col.gameObject == gameObject)
                continue;

            // Calculate push-away direction
            Vector3 awayFromNeighbor = transform.position - col.transform.position;
            awayFromNeighbor.y = 0; // Keep on horizontal plane

            float distance = awayFromNeighbor.magnitude;
            
            if (distance > 0.01f && distance < separationRadius)
            {
                // Stronger push when closer (inverse square falloff)
                float pushStrength = separationRadius / (distance + 0.1f);
                separationForce += awayFromNeighbor.normalized * pushStrength;
                neighborCount++;
            }
        }

        // Average and scale by strength
        if (neighborCount > 0)
        {
            separationForce = (separationForce / neighborCount) * separationStrength;
        }

        return separationForce;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Draw separation radius
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        // Draw line to player
        if (player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, player.position);
        }

        // Draw stopping distance
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        if (player != null)
        {
            Gizmos.DrawWireSphere(player.position, stoppingDistance);
        }
    }
#endif
}
