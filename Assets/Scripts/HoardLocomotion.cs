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

    [Header("COD Zombies Circle Positioning")]
    [Tooltip("Ideal distance to maintain around player")]
    public float attackCircleRadius = 1.8f;
    [Tooltip("How strongly zombies are pulled to circle position")]
    public float circlePositionStrength = 1.5f;
    [Tooltip("Distance at which zombie tries to find circle position")]
    public float circleActivationDistance = 4f;

    [Header("NavMesh Settings")]
    [Tooltip("Stopping distance from target")]
    public float stoppingDistance = 1.5f;

    [Header("Animation")]
    [Tooltip("Animator component (assign manually - can be on child object)")]
    public Animator animator;
    [Tooltip("Name of the Speed parameter in the animator")]
    public string speedParameterName = "Speed";
    [Tooltip("Normalize speed value (0-1) or use actual speed units")]
    public bool normalizeSpeed = true;
    [Tooltip("Maximum speed value for normalization (when normalizeSpeed is true)")]
    public float maxSpeedForNormalization = 3f;

    private NavMeshAgent agent;
    private Vector3 currentVelocity;
    private int zombieID;
    private float assignedAngle; // Angle around player for this zombie
    private bool isInAttackPosition = false; // Set by EnemyController

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        
        // Configure NavMeshAgent for COD Zombies style movement
        agent.speed = walkSpeed;
        agent.angularSpeed = rotationSpeed;
        agent.acceleration = 20f; // Higher acceleration for more responsive stopping
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
        
        // Assign a semi-random angle based on instance ID for circle positioning
        assignedAngle = (zombieID % 360) * Mathf.Deg2Rad;
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

    public void SetInAttackPosition(bool inPosition)
    {
        isInAttackPosition = inPosition;
        
        // Immediately stop the agent when entering attack position
        if (inPosition && agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero; // Kill all momentum immediately
            agent.ResetPath(); // Clear any pathfinding
        }
    }

    void Update()
    {
        if (player == null || agent == null || !agent.isOnNavMesh)
            return;

        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // If in attack position, stop moving and just maintain position
        if (isInAttackPosition)
        {
            // Ensure agent stays completely stopped
            agent.isStopped = true;
            agent.velocity = Vector3.zero; // Keep zeroing velocity to prevent drift
            return;
        }
        else
        {
            // Make sure agent is not stopped
            if (agent.isStopped)
            {
                agent.isStopped = false;
            }
        }

        Vector3 targetPosition;

        // Use circle positioning when close to player (COD Zombies style)
        if (distanceToPlayer <= circleActivationDistance)
        {
            targetPosition = CalculateCirclePosition();
            
            // Add separation force to prevent overlap
            Vector3 separationForce = CalculateSeparation();
            targetPosition += separationForce;
        }
        else
        {
            // Far away: just chase player directly with separation
            Vector3 separationForce = CalculateSeparation();
            targetPosition = player.position + separationForce;
        }
        
        // Sample to ensure it's on the NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, 2f, agent.areaMask))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // If target is off-mesh, just go to player
            agent.SetDestination(player.position);
        }

        // Apply speed based on distance (slow down when close for better melee)
        float speedMultiplier = Mathf.Lerp(minSpeed, 1f, Mathf.InverseLerp(0f, slowdownDistance, distanceToPlayer));
        
        // Additional slowdown when very close to target position to prevent overshooting
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        if (distanceToTarget < stoppingDistance)
        {
            float targetSlowdown = Mathf.InverseLerp(0f, stoppingDistance, distanceToTarget);
            speedMultiplier *= targetSlowdown;
        }
        
        agent.speed = walkSpeed * speedMultiplier;
        
        // Update animation based on current movement speed
        UpdateAnimation();
    }

    void UpdateAnimation()
    {
        if (animator == null || string.IsNullOrEmpty(speedParameterName))
            return;

        // Get current movement speed from NavMeshAgent
        float currentSpeed = agent.velocity.magnitude;
        
        // Handle different speed parameter formats
        float speedValue;
        if (normalizeSpeed)
        {
            // Normalize to 0-1 range based on max speed
            speedValue = Mathf.Clamp01(currentSpeed / maxSpeedForNormalization);
        }
        else
        {
            // Use raw speed value
            speedValue = currentSpeed;
        }
        
        // Set the animator parameter
        animator.SetFloat(speedParameterName, speedValue);
    }

    Vector3 CalculateCirclePosition()
    {
        // Calculate position on circle around player
        // Dynamically adjust angle based on nearby zombies to spread out evenly
        float adjustedAngle = assignedAngle;
        
        // Check for nearby zombies and adjust angle to avoid clustering
        Collider[] nearbyZombies = Physics.OverlapSphere(player.position, circleActivationDistance, zombieLayer);
        
        foreach (Collider col in nearbyZombies)
        {
            if (col.gameObject == gameObject) continue;
            
            // Check if the other zombie is in attack position - avoid them more strongly
            EnemyController otherController = col.GetComponent<EnemyController>();
            bool otherIsAttacking = otherController != null && otherController.IsInAttackPosition;
            
            Vector3 toNeighbor = col.transform.position - player.position;
            toNeighbor.y = 0;
            
            if (toNeighbor.sqrMagnitude > 0.01f)
            {
                float neighborAngle = Mathf.Atan2(toNeighbor.z, toNeighbor.x);
                float angleDiff = Mathf.DeltaAngle(adjustedAngle * Mathf.Rad2Deg, neighborAngle * Mathf.Rad2Deg);
                
                // Avoid zombies in attack positions more strongly
                float avoidanceThreshold = otherIsAttacking ? 45f : 30f;
                float avoidanceAmount = otherIsAttacking ? 25f : 15f;
                
                // If too close in angle, shift away
                if (Mathf.Abs(angleDiff) < avoidanceThreshold)
                {
                    adjustedAngle += Mathf.Sign(angleDiff) * -avoidanceAmount * Mathf.Deg2Rad;
                }
            }
        }
        
        // Calculate position on the circle
        Vector3 circleOffset = new Vector3(
            Mathf.Cos(adjustedAngle) * attackCircleRadius,
            0f,
            Mathf.Sin(adjustedAngle) * attackCircleRadius
        );
        
        Vector3 idealPosition = player.position + circleOffset;
        
        // Blend between current path and circle position based on distance
        Vector3 directionToCircle = idealPosition - transform.position;
        directionToCircle.y = 0;
        
        return player.position + (directionToCircle.normalized * circlePositionStrength) + circleOffset;
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
            
            // Draw attack circle radius
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawWireSphere(player.position, attackCircleRadius);
            
            // Draw circle activation distance
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(player.position, circleActivationDistance);
            
            // Draw assigned angle position
            Vector3 circlePos = player.position + new Vector3(
                Mathf.Cos(assignedAngle) * attackCircleRadius,
                0.5f,
                Mathf.Sin(assignedAngle) * attackCircleRadius
            );
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(circlePos, 0.2f);
            Gizmos.DrawLine(player.position, circlePos);
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
