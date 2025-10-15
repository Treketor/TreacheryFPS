using UnityEngine;

public class WeaponRaycaster : MonoBehaviour
{
    [SerializeField] float maxRange = 100f;
    [SerializeField] LayerMask hitMask = ~0; // Everything by default
    [SerializeField] LayerMask ignoreLayerMask = 0; // Nothing ignored by default

    LayerMask _effectiveMask;

    void Awake()
    {
        // Combine the masks: hit what's in hitMask but NOT in ignoreLayerMask
        _effectiveMask = hitMask & ~ignoreLayerMask;
    }

    public bool TryShoot(out RaycastHit hit, float spreadDeg = 0f)
    {
        var dir = transform.forward;
        if (spreadDeg > 0f)
        {
            var r = Random.insideUnitCircle * Mathf.Tan(spreadDeg * Mathf.Deg2Rad);
            dir = (transform.forward + transform.right * r.x + transform.up * r.y).normalized;
        }

        // Use QueryTriggerInteraction.Collide to detect headshot zones (which are triggers)
        return Physics.Raycast(transform.position, dir, out hit, maxRange, _effectiveMask, QueryTriggerInteraction.Collide);
    }

    // Call this if you change the layer masks at runtime
    public void RefreshLayerMask()
    {
        _effectiveMask = hitMask & ~ignoreLayerMask;
    }
}