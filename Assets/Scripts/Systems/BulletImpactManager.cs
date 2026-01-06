using UnityEngine;

/// <summary>
/// Manages bullet impact effects for different surface types
/// </summary>
public class BulletImpactManager : MonoBehaviour
{
    [Header("Default Impact Effect")]
    [SerializeField] GameObject defaultImpactPrefab;
    [Tooltip("Default impact effect used when no specific surface effect is available")]
    
    [Header("Effect Settings")]
    [SerializeField] float effectLifetime = 2f;
    [Tooltip("How long impact effects stay active before being destroyed")]
    
    [SerializeField] bool alignToSurface = true;
    [Tooltip("Whether to align the impact effect to the surface normal")]
    
    [SerializeField] Vector3 surfaceOffset = Vector3.zero;
    [Tooltip("Offset from surface to prevent z-fighting")]
    
    // Singleton for easy access
    public static BulletImpactManager Instance { get; private set; }
    
    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    /// <summary>
    /// Spawn an impact effect at the specified position and rotation
    /// </summary>
    /// <param name="hitPoint">World position where the impact occurred</param>
    /// <param name="hitNormal">Surface normal at impact point</param>
    /// <param name="hitCollider">The collider that was hit (for future surface detection)</param>
    public void SpawnImpactEffect(Vector3 hitPoint, Vector3 hitNormal, Collider hitCollider = null)
    {
        // For now, just use the default impact effect
        // Later this can be expanded to check surface types
        GameObject impactPrefab = GetImpactPrefabForSurface(hitCollider);
        
        if (impactPrefab == null)
        {
            return;
        }
        
        // Calculate rotation based on surface normal if alignment is enabled
        Quaternion rotation = Quaternion.identity;
        if (alignToSurface)
        {
            // Align the effect to face away from the surface
            rotation = Quaternion.LookRotation(-hitNormal);
        }
        
        // Apply surface offset to prevent z-fighting
        Vector3 spawnPosition = hitPoint + (hitNormal * surfaceOffset.magnitude);
        
        // Instantiate the impact effect
        GameObject impactEffect = Instantiate(impactPrefab, spawnPosition, rotation);
        
        // Auto-destroy after lifetime expires
        if (effectLifetime > 0)
        {
            Destroy(impactEffect, effectLifetime);
        }
    }
    
    /// <summary>
    /// Get the appropriate impact prefab for the hit surface
    /// Currently returns default prefab, but can be expanded for surface-specific effects
    /// </summary>
    /// <param name="hitCollider">The collider that was hit</param>
    /// <returns>Impact prefab to spawn</returns>
    GameObject GetImpactPrefabForSurface(Collider hitCollider)
    {
        // TODO: Add surface detection logic here
        // For now, always return default impact effect
        // Future expansion could check:
        // - Surface material (wood, metal, concrete, flesh, etc.)
        // - Hit collider tags or components
        // - Surface material properties
        
        return defaultImpactPrefab;
    }
    
    /// <summary>
    /// Manually set the default impact prefab at runtime
    /// </summary>
    /// <param name="prefab">New default impact prefab</param>
    public void SetDefaultImpactPrefab(GameObject prefab)
    {
        defaultImpactPrefab = prefab;
    }
}