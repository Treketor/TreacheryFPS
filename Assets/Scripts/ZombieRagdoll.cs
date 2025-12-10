using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controls the transition between animated zombie and ragdoll physics
/// </summary>
public class ZombieRagdoll : MonoBehaviour
{
    [Header("Ragdoll Settings")]
    [Tooltip("Force applied to ragdoll when activated")]
    [SerializeField] float ragdollForce = 500f;
    [Tooltip("How long before the ragdoll is destroyed (0 = never)")]
    [SerializeField] float ragdollLifetime = 15f;
    [Tooltip("Layer to put ragdoll colliders on")]
    [SerializeField] LayerMask ragdollLayer = 1;
    
    [Header("Components")]
    [Tooltip("Main character collider (will be disabled when ragdoll activates)")]
    [SerializeField] Collider mainCollider;
    [Tooltip("Auto-find main collider if not assigned")]
    [SerializeField] bool autoFindMainCollider = true;
    
    private Animator animator;
    private NavMeshAgent navMeshAgent;
    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;
    private bool isRagdoll = false;
    
    void Awake()
    {
        // Get components
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        
        // Find ragdoll components
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();
        
        // Auto-find main collider
        if (mainCollider == null && autoFindMainCollider)
        {
            mainCollider = GetComponent<Collider>();
        }
        
        // Start with ragdoll disabled
        SetRagdollEnabled(false);
    }
    
    /// <summary>
    /// Activate the ragdoll with optional death force (and destroy GameObject after lifetime)
    /// </summary>
    /// <param name="forceDirection">Direction and strength of death force</param>
    /// <param name="forcePoint">Point where force is applied (optional)</param>
    public void ActivateRagdoll(Vector3 forceDirection = default, Vector3 forcePoint = default)
    {
        ActivateRagdollWithoutDestroy(forceDirection, forcePoint);
        
        // Set cleanup timer
        if (ragdollLifetime > 0)
        {
            Destroy(gameObject, ragdollLifetime);
        }
    }
    
    /// <summary>
    /// Activate ragdoll but don't destroy the GameObject (external system will handle destruction)
    /// </summary>
    /// <param name="forceDirection">Direction and strength of death force</param>
    /// <param name="forcePoint">Point where force is applied (optional)</param>
    public void ActivateRagdollWithoutDestroy(Vector3 forceDirection = default, Vector3 forcePoint = default)
    {
        if (isRagdoll) return;
        

        
        isRagdoll = true;
        
        // Disable other components first
        DisableZombieComponents();
        
        // Enable ragdoll physics
        SetRagdollEnabled(true);
        
        // Apply death force
        ApplyDeathForce(forceDirection, forcePoint);
    }
    
    /// <summary>
    /// Get the ragdoll lifetime for external systems to use
    /// </summary>
    public float RagdollLifetime => ragdollLifetime;
    
    /// <summary>
    /// Create a detached ragdoll GameObject that persists after the original is destroyed
    /// </summary>
    public void CreateDetachedRagdoll(Vector3 forceDirection = default, Vector3 forcePoint = default)
    {
        if (isRagdoll)
        {
            Debug.LogWarning($"{gameObject.name}: Already a ragdoll, skipping detached ragdoll creation");
            return;
        }
        

        
        // Disable main collider FIRST to prevent physics conflicts
        if (mainCollider != null)
        {
            mainCollider.enabled = false;
        }
        
        // Mark as ragdoll immediately to prevent multiple calls
        isRagdoll = true;
        
        // Create new GameObject for the ragdoll
        GameObject ragdollObject = Instantiate(gameObject);
        ragdollObject.name = $"{gameObject.name}_Ragdoll";
        
        // Get the ragdoll component on the new object
        ZombieRagdoll newRagdoll = ragdollObject.GetComponent<ZombieRagdoll>();
        if (newRagdoll != null)
        {
            // Remove non-ragdoll components from the copy
            if (ragdollObject.TryGetComponent<EnemyHealth>(out var health)) Destroy(health);
            if (ragdollObject.TryGetComponent<EnemyController>(out var controller)) Destroy(controller);
            if (ragdollObject.TryGetComponent<HoardLocomotion>(out var locomotion)) Destroy(locomotion);
            
            // Activate ragdoll on the copy
            newRagdoll.ActivateRagdoll(forceDirection, forcePoint);
            

        }
        else
        {
            Debug.LogError($"Failed to get ZombieRagdoll component from detached ragdoll: {ragdollObject.name}");
        }
    }
    

    
    /// <summary>
    /// Check if this zombie is currently in ragdoll state
    /// </summary>
    public bool IsRagdoll => isRagdoll;
    
    /// <summary>
    /// Disable zombie AI and movement components
    /// </summary>
    private void DisableZombieComponents()
    {
        // Change tag to prevent wave system from counting this as an active enemy
        if (gameObject.tag == "Enemy")
        {
            gameObject.tag = "Untagged";
        }
        
        // Disable ALL animators (both on this object and children)
        Animator[] allAnimators = GetComponentsInChildren<Animator>();
        foreach (Animator anim in allAnimators)
        {
            if (anim != null)
            {
                anim.enabled = false;
            }
        }
        
        // Disable NavMeshAgent
        if (navMeshAgent != null)
        {
            navMeshAgent.enabled = false;
        }
        
        // Disable other zombie scripts that might be used for enemy counting
        if (TryGetComponent<EnemyController>(out var enemyController))
        {
            enemyController.enabled = false;
        }
        
        if (TryGetComponent<HoardLocomotion>(out var locomotion))
        {
            locomotion.enabled = false;
        }
        
        // Disable EnemyHealth to prevent it from being counted as alive
        if (TryGetComponent<EnemyHealth>(out var enemyHealth))
        {
            enemyHealth.enabled = false;
        }
        
        // Disable main character collider
        if (mainCollider != null)
        {
            mainCollider.enabled = false;
        }
        
        // Move to a different layer if needed (ragdoll layer)
        gameObject.layer = Mathf.RoundToInt(Mathf.Log(ragdollLayer.value, 2));
    }
    
    /// <summary>
    /// Enable or disable ragdoll physics
    /// </summary>
    private void SetRagdollEnabled(bool enabled)
    {
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb != null)
            {
                rb.isKinematic = !enabled;
                
                if (enabled)
                {
                    // Clear all velocities to prevent erratic movement
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    
                    // Set ragdoll layer
                    rb.gameObject.layer = Mathf.RoundToInt(Mathf.Log(ragdollLayer.value, 2));
                }
            }
        }
        
        foreach (Collider col in ragdollColliders)
        {
            if (col != null && col != mainCollider)
            {
                col.enabled = enabled;
                
                if (enabled)
                {
                    // Set ragdoll layer
                    col.gameObject.layer = Mathf.RoundToInt(Mathf.Log(ragdollLayer.value, 2));
                }
            }
        }
    }
    
    /// <summary>
    /// Apply death force to the ragdoll
    /// </summary>
    private void ApplyDeathForce(Vector3 forceDirection, Vector3 forcePoint)
    {
        if (forceDirection == Vector3.zero || ragdollRigidbodies.Length == 0) return;
        
        // Find the best rigidbody to apply force to
        Rigidbody targetRigidbody = null;
        
        if (forcePoint != Vector3.zero)
        {
            // Find closest rigidbody to force point
            float closestDistance = float.MaxValue;
            foreach (Rigidbody rb in ragdollRigidbodies)
            {
                if (rb != null)
                {
                    float distance = Vector3.Distance(rb.transform.position, forcePoint);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        targetRigidbody = rb;
                    }
                }
            }
        }
        else
        {
            // Use center rigidbody (usually pelvis/spine)
            targetRigidbody = ragdollRigidbodies[0];
        }
        
        // Apply the force (forceDirection already contains the magnitude)
        if (targetRigidbody != null)
        {
            if (forcePoint != Vector3.zero)
            {
                targetRigidbody.AddForceAtPosition(forceDirection, forcePoint, ForceMode.Impulse);
            }
            else
            {
                targetRigidbody.AddForce(forceDirection, ForceMode.Impulse);
            }
            
            Debug.Log($"Applied death force {forceDirection} to {targetRigidbody.name}");
        }
    }
    
    /// <summary>
    /// Get the main body rigidbody (useful for applying forces)
    /// </summary>
    public Rigidbody GetMainBodyRigidbody()
    {
        if (ragdollRigidbodies.Length > 0)
        {
            return ragdollRigidbodies[0]; // Usually the pelvis/spine
        }
        return null;
    }
}