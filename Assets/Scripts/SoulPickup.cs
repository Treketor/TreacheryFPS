using UnityEngine;

/// <summary>
/// Soul pickup that ejects from enemies, then flies toward the player for collection.
/// Behavior: Eject with random direction → Wait → Attract to player → Collect on contact
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class SoulPickup : MonoBehaviour
{
    [Header("Soul Value")]
    [SerializeField] int soulValue = 1;

    [Header("Ejection Phase")]
    [Tooltip("Initial speed when ejected from enemy")]
    [SerializeField] float ejectionSpeed = 5f;
    [Tooltip("How long the soul flies away before starting attraction")]
    [SerializeField] float ejectionDuration = 0.3f;
    [Tooltip("Gravity strength during ejection (pulls down)")]
    [SerializeField] float ejectionGravity = 15f;
    [Tooltip("How quickly velocity dampens at end of ejection (higher = smoother stop)")]
    [SerializeField] float ejectionDamping = 8f;

    [Header("Attraction Phase")]
    [Tooltip("Delay before soul starts flying toward player")]
    [SerializeField] float attractionDelay = 0.5f;
    [Tooltip("Speed when flying toward player")]
    [SerializeField] float attractionSpeed = 12f;
    [Tooltip("Acceleration multiplier as it gets closer to player")]
    [SerializeField] float attractionAcceleration = 1.5f;

    [Header("Collection")]
    [Tooltip("Layer mask for player detection")]
    [SerializeField] LayerMask playerLayer;
    [Tooltip("Y offset to aim for on player (e.g., 1.0 for chest height)")]
    [SerializeField] float playerHeightOffset = 1f;

    [Header("Lifetime")]
    [Tooltip("Maximum time before auto-destroying (safety)")]
    [SerializeField] float maxLifetime = 10f;

    Transform _player;
    Vector3 _velocity;
    float _timer;
    bool _isEjecting = true;
    bool _isAttracting = false;

    void Start()
    {
        // Find player
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;

        // Setup collider as trigger
        var col = GetComponent<SphereCollider>();
        col.isTrigger = true;

        // Destroy after max lifetime as safety
        Destroy(gameObject, maxLifetime);
    }

    /// <summary>
    /// Call this to eject the soul in a random direction.
    /// </summary>
    public void Eject(Vector3 fromPosition)
    {
        transform.position = fromPosition;
        
        // Random direction with upward bias
        Vector3 randomDir = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(0.5f, 1.5f), // More upward
            Random.Range(-1f, 1f)
        ).normalized;

        _velocity = randomDir * ejectionSpeed;
        _isEjecting = true;
        _timer = 0f;
    }

    void Update()
    {
        _timer += Time.deltaTime;

        if (_isEjecting)
        {
            // Ejection phase: fly in initial direction with gravity
            _velocity.y -= ejectionGravity * Time.deltaTime;
            
            // Apply easing as we approach the end of ejection
            float ejectionProgress = _timer / ejectionDuration;
            float dampingFactor = Mathf.Lerp(1f, 0.1f, ejectionProgress);
            _velocity *= Mathf.Pow(dampingFactor, Time.deltaTime * ejectionDamping);
            
            transform.position += _velocity * Time.deltaTime;

            // End ejection phase
            if (_timer >= ejectionDuration)
            {
                _isEjecting = false;
                _timer = 0f;
            }
        }
        else if (!_isAttracting)
        {
            // Waiting phase: float in place (or fall slightly)
            _velocity.y -= ejectionGravity * 0.2f * Time.deltaTime;
            transform.position += _velocity * Time.deltaTime;
            _velocity *= 0.92f; // Gentle damping

            // Start attraction after delay
            if (_timer >= attractionDelay)
            {
                _isAttracting = true;
            }
        }
        else if (_isAttracting && _player != null)
        {
            // Attraction phase: fly toward player with acceleration
            // Target point is player position + height offset (aim for chest/center)
            Vector3 targetPosition = _player.position + Vector3.up * playerHeightOffset;
            Vector3 direction = (targetPosition - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, targetPosition);

            // Speed increases as it gets closer
            float speedMultiplier = Mathf.Lerp(1f, attractionAcceleration, 1f - Mathf.Clamp01(distance / 10f));
            float currentSpeed = attractionSpeed * speedMultiplier;

            transform.position += direction * currentSpeed * Time.deltaTime;

            // Optional: Rotate toward player
            transform.Rotate(Vector3.up, 360f * Time.deltaTime, Space.World);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if we hit the player
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            CollectSoul();
        }
    }

    void CollectSoul()
    {
        // Add souls to player
        if (SoulManager.Instance != null)
        {
            SoulManager.Instance.AddSouls(soulValue);
        }

        // TODO: Play collection sound/VFX here

        // Destroy the pickup
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        // Debug visualization
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        if (_player != null && _isAttracting)
        {
            Vector3 targetPosition = _player.position + Vector3.up * playerHeightOffset;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, 0.2f);
        }
    }
}
