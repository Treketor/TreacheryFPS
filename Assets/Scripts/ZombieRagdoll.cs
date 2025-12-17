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
    [Tooltip("Cleanup ragdolls when wave completes (recommended)")]
    [SerializeField] bool cleanupOnWaveComplete = true;
    [Tooltip("Delay before cleaning up ragdolls after wave completion")]
    [SerializeField] float waveCleanupDelay = 5f;
    [Tooltip("Layer to put ragdoll colliders on")]
    [SerializeField] LayerMask ragdollLayer = 1;
    [Tooltip("Ignore collisions with these layers (typically Player and Enemy layers)")]
    [SerializeField] LayerMask ignoreCollisionLayers = 0;
    
    // Static tracking for wave completion cleanup
    private static readonly System.Collections.Generic.List<ZombieRagdoll> _activeRagdolls = new();
    
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
        
        // Disable main collider and use ragdoll colliders for live enemies
        if (mainCollider != null)
        {
            mainCollider.enabled = false;
        }
        
        // Start with ragdoll colliders enabled for live enemy hit detection
        SetLiveEnemyColliders(true);
    }
    
    /// <summary>
    /// Activate the ragdoll with optional death force (will persist until wave completion)
    /// </summary>
    /// <param name="forceDirection">Direction and strength of death force</param>
    /// <param name="forcePoint">Point where force is applied (optional)</param>
    public void ActivateRagdoll(Vector3 forceDirection = default, Vector3 forcePoint = default)
    {
        ActivateRagdollWithoutDestroy(forceDirection, forcePoint);
        
        // Register for wave completion cleanup
        if (cleanupOnWaveComplete)
        {
            RegisterForWaveCleanup();
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
        
        // Transition from live enemy colliders to full ragdoll physics
        SetLiveEnemyColliders(false); // Disable live enemy setup
        SetRagdollEnabled(true);       // Enable full ragdoll physics with collision filtering
        
        // Apply death force
        ApplyDeathForce(forceDirection, forcePoint);
    }
    
    /// <summary>
    /// Register this ragdoll for cleanup when the wave completes
    /// </summary>
    private void RegisterForWaveCleanup()
    {
        if (!_activeRagdolls.Contains(this))
        {
            _activeRagdolls.Add(this);
            
            // Subscribe to wave completion events if this is the first ragdoll
            if (_activeRagdolls.Count == 1)
            {
                SubscribeToWaveEvents();
            }
        }
    }
    
    /// <summary>
    /// Subscribe to ScoreManager wave completion events
    /// </summary>
    private static void SubscribeToWaveEvents()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnWaveComplete.AddListener(OnWaveCompleted);
        }
    }
    
    /// <summary>
    /// Handle wave completion - clean up all ragdolls after a delay
    /// </summary>
    private static void OnWaveCompleted(int waveNumber)
    {
        Debug.Log($"Wave {waveNumber} completed! Will clean up {_activeRagdolls.Count} ragdolls after delay.");
        
        // Find a MonoBehaviour to start the coroutine on
        // Use the first available ragdoll if any exist
        if (_activeRagdolls.Count > 0 && _activeRagdolls[0] != null)
        {
            _activeRagdolls[0].StartCoroutine(DelayedCleanupCoroutine());
        }
        else
        {
            // No ragdolls to clean up
            CleanupAllRagdolls();
        }
    }
    
    /// <summary>
    /// Coroutine to handle delayed ragdoll cleanup
    /// </summary>
    private static System.Collections.IEnumerator DelayedCleanupCoroutine()
    {
        // Get delay from the first ragdoll (they should all have the same setting)
        float delay = _activeRagdolls.Count > 0 && _activeRagdolls[0] != null ? _activeRagdolls[0].waveCleanupDelay : 5f;
        
        yield return new WaitForSeconds(delay);
        
        CleanupAllRagdolls();
    }
    
    /// <summary>
    /// Clean up all ragdolls immediately
    /// </summary>
    private static void CleanupAllRagdolls()
    {
        Debug.Log($"Cleaning up {_activeRagdolls.Count} ragdolls now.");
        
        // Clean up all registered ragdolls
        for (int i = _activeRagdolls.Count - 1; i >= 0; i--)
        {
            if (_activeRagdolls[i] != null)
            {
                Destroy(_activeRagdolls[i].gameObject);
            }
        }
        
        // Clear the list
        _activeRagdolls.Clear();
        
        // Unsubscribe since no ragdolls remain
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnWaveComplete.RemoveListener(OnWaveCompleted);
        }
    }
    
    void OnDestroy()
    {
        // Remove from active ragdolls list when destroyed
        if (_activeRagdolls.Contains(this))
        {
            _activeRagdolls.Remove(this);
            
            // If no ragdolls remain, unsubscribe from events
            if (_activeRagdolls.Count == 0 && ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnWaveComplete.RemoveListener(OnWaveCompleted);
            }
        }
    }
    
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
        
        // Main collider is already disabled - ragdoll colliders handle all collision detection
        
        // Move to a different layer if needed (ragdoll layer)
        gameObject.layer = Mathf.RoundToInt(Mathf.Log(ragdollLayer.value, 2));
    }
    
    /// <summary>
    /// Configure colliders for live enemy (kinematic, no physics forces)
    /// </summary>
    private void SetLiveEnemyColliders(bool enabled)
    {
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb != null)
            {
                // Keep kinematic for live enemies (no physics)
                rb.isKinematic = true;
                
                if (enabled)
                {
                    // Clear any residual velocities
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
        
        foreach (Collider col in ragdollColliders)
        {
            if (col != null && col != mainCollider)
            {
                col.enabled = enabled;
                
                // Don't apply collision filtering for live enemies - they should hit normally
                // Only apply filtering when in actual ragdoll state
            }
        }
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
                    int ragdollLayerIndex = Mathf.RoundToInt(Mathf.Log(ragdollLayer.value, 2));
                    rb.gameObject.layer = ragdollLayerIndex;
                    
                    // Configure collision filtering if this rigidbody has a collider
                    if (rb.TryGetComponent<Collider>(out var rbCollider))
                    {
                        ConfigureCollisionFiltering(rbCollider, ragdollLayerIndex);
                    }
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
                    int ragdollLayerIndex = Mathf.RoundToInt(Mathf.Log(ragdollLayer.value, 2));
                    col.gameObject.layer = ragdollLayerIndex;
                    
                    // Configure collision filtering
                    ConfigureCollisionFiltering(col, ragdollLayerIndex);
                }
            }
        }
    }
    
    /// <summary>
    /// Configure collision filtering for ragdoll colliders
    /// </summary>
    private void ConfigureCollisionFiltering(Collider ragdollCollider, int ragdollLayerIndex)
    {
        // Ignore collisions with specified layers (Player, Enemy, etc.)
        for (int i = 0; i < 32; i++)
        {
            if ((ignoreCollisionLayers.value & (1 << i)) != 0)
            {
                Physics.IgnoreLayerCollision(ragdollLayerIndex, i, true);
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