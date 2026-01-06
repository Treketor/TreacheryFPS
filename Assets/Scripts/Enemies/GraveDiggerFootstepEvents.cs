using UnityEngine;

/// <summary>
/// Attach this to the Grave Digger MODEL object (the object that has the Animator / SkinnedMeshRenderer).
///
/// Add Animation Events to the Grave Digger walking animation that call:
/// - AnimEvent_Footstep()
/// - AnimEvent_FootstepScaled(float multiplier)   (optional)
/// - AnimEvent_FootstepLeft() / AnimEvent_FootstepRight() (optional convenience)
///
/// This triggers ScreenShake with intensity based on distance to the player.
/// </summary>
[DisallowMultipleComponent]
public class GraveDiggerFootstepEvents : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Optional: explicitly assign the Player. If empty, will search by tag at runtime.")]
    [SerializeField] Transform player;

    [SerializeField] string playerTag = "Player";

    [Tooltip("Optional: the enemy root transform to measure distance from. If empty, uses transform.root.")]
    [SerializeField] Transform enemyRoot;

    [Header("Distance -> Shake Intensity")]
    [Tooltip("At or below this distance, intensity will be maxShakeAmplitude.")]
    [SerializeField, Min(0f)] float minDistance = 2.0f;

    [Tooltip("At or above this distance, intensity will be minShakeAmplitude.")]
    [SerializeField, Min(0f)] float maxDistance = 12.0f;

    [Tooltip("Shake amplitude when far (at/above maxDistance). Set to 0 to disable shaking at range.")]
    [SerializeField, Min(0f)] float minShakeAmplitude = 0.0f;

    [Tooltip("Shake amplitude when close (at/below minDistance).")]
    [SerializeField, Min(0f)] float maxShakeAmplitude = 0.35f;

    [Header("Shake Shape")]
    [SerializeField, Min(0.01f)] float duration = 0.12f;
    [SerializeField, Min(0f)] float frequency = 22f;
    [SerializeField] Vector3 positionStrength = new Vector3(0.04f, 0.04f, 0.02f);
    [SerializeField] Vector3 rotationStrength = new Vector3(1.1f, 0.8f, 0.8f);

    void Awake()
    {
        if (enemyRoot == null)
            enemyRoot = transform.root;
    }

    /// <summary>
    /// Animation Event: call this at each foot plant.
    /// </summary>
    public void AnimEvent_Footstep()
    {
        TriggerFootstepShake(1f);
    }

    /// <summary>
    /// Animation Event (optional): same as AnimEvent_Footstep but allows per-event scaling.
    /// Useful if you want heavier/lighter steps for different clips.
    /// </summary>
    public void AnimEvent_FootstepScaled(float multiplier)
    {
        TriggerFootstepShake(multiplier);
    }

    // Convenience names if you prefer separate events per foot.
    public void AnimEvent_FootstepLeft() => TriggerFootstepShake(1f);
    public void AnimEvent_FootstepRight() => TriggerFootstepShake(1f);

    void TriggerFootstepShake(float multiplier)
    {
        if (ScreenShake.Instance == null)
            return;

        Transform playerTransform = GetPlayer();
        if (playerTransform == null)
            return;

        Transform from = enemyRoot != null ? enemyRoot : transform;
        float dist = Vector3.Distance(from.position, playerTransform.position);

        float minD = Mathf.Min(minDistance, maxDistance);
        float maxD = Mathf.Max(minDistance, maxDistance);

        float t = (maxD <= minD) ? 1f : Mathf.InverseLerp(minD, maxD, dist);

        // Closer = stronger.
        float amplitude = Mathf.Lerp(maxShakeAmplitude, minShakeAmplitude, t);
        amplitude = Mathf.Max(0f, amplitude * Mathf.Max(0f, multiplier));

        if (amplitude <= 0f)
            return;

        ScreenShake.Shake(amplitude, duration, frequency, positionStrength, rotationStrength);
    }

    Transform GetPlayer()
    {
        if (player != null)
            return player;

        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
            player = playerObj.transform;

        return player;
    }
}
