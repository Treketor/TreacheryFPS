using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Add this to the MainCamera (child of the Player). Provides a global, additive screen shake.
///
/// - Does not replace your look/recoil: it applies in LateUpdate, after FirstPersonLook (Update).
/// - Safe with your current setup: FirstPersonLook writes camera localRotation each Update.
/// - Shake is applied as an additive local position + local rotation offset.
///
/// Call from anywhere: ScreenShake.Shake(...)
/// </summary>
[DisallowMultipleComponent]
public class ScreenShake : MonoBehaviour
{
    [Serializable]
    public struct ShakeRequest
    {
        [Min(0f)] public float amplitude;
        [Min(0.01f)] public float duration;
        [Min(0f)] public float frequency;

        public Vector3 positionStrength;
        public Vector3 rotationStrength;

        public ShakeRequest(
            float amplitude,
            float duration,
            float frequency,
            Vector3 positionStrength,
            Vector3 rotationStrength)
        {
            this.amplitude = amplitude;
            this.duration = duration;
            this.frequency = frequency;
            this.positionStrength = positionStrength;
            this.rotationStrength = rotationStrength;
        }
    }

    class ActiveShake
    {
        public float startTime;
        public float duration;
        public float amplitude;
        public float frequency;
        public Vector3 positionStrength;
        public Vector3 rotationStrength;
        public Vector3 seed;
    }

    public static ScreenShake Instance { get; private set; }

    [Header("Defaults")]
    [Tooltip("Default positional shake strength in local units (meters).")]
    [SerializeField] Vector3 defaultPositionStrength = new Vector3(0.06f, 0.06f, 0.02f);

    [Tooltip("Default rotational shake strength in degrees.")]
    [SerializeField] Vector3 defaultRotationStrength = new Vector3(1.8f, 1.2f, 1.2f);

    [SerializeField, Min(0.01f)] float defaultDuration = 0.18f;
    [SerializeField, Min(0f)] float defaultFrequency = 22f;

    [Header("Behavior")]
    [Tooltip("If enabled, this ScreenShake object is preserved across scene loads.")]
    [SerializeField] bool dontDestroyOnLoad = false;

    [Tooltip("Maximum number of overlapping shakes kept at once.")]
    [SerializeField, Min(1)] int maxConcurrentShakes = 16;

    [Tooltip("If enabled, uses the camera's initial local position as the rest position each frame.")]
    [SerializeField] bool lockToInitialLocalPosition = true;

    readonly List<ActiveShake> _active = new List<ActiveShake>(16);

    Vector3 _initialLocalPosition;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        _initialLocalPosition = transform.localPosition;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnEnable()
    {
        _initialLocalPosition = transform.localPosition;
    }

    /// <summary>
    /// Global convenience call.
    /// Safe to call even if no ScreenShake exists.
    /// </summary>
    public static void Shake(float amplitude = 1f)
    {
        if (Instance == null) return;

        Instance.Play(new ShakeRequest(
            amplitude: amplitude,
            duration: Instance.defaultDuration,
            frequency: Instance.defaultFrequency,
            positionStrength: Instance.defaultPositionStrength,
            rotationStrength: Instance.defaultRotationStrength));
    }

    public static void Shake(float amplitude, float duration, float frequency)
    {
        if (Instance == null) return;

        Instance.Play(new ShakeRequest(
            amplitude: amplitude,
            duration: duration,
            frequency: frequency,
            positionStrength: Instance.defaultPositionStrength,
            rotationStrength: Instance.defaultRotationStrength));
    }

    public static void Shake(float amplitude, float duration, float frequency, Vector3 positionStrength, Vector3 rotationStrength)
    {
        if (Instance == null) return;
        Instance.Play(new ShakeRequest(amplitude, duration, frequency, positionStrength, rotationStrength));
    }

    public void Play(ShakeRequest request)
    {
        if (request.amplitude <= 0f || request.duration <= 0f)
            return;

        if (_active.Count >= maxConcurrentShakes)
            _active.RemoveAt(0);

        _active.Add(new ActiveShake
        {
            startTime = Time.time,
            duration = request.duration,
            amplitude = request.amplitude,
            frequency = request.frequency,
            positionStrength = request.positionStrength,
            rotationStrength = request.rotationStrength,
            seed = new Vector3(UnityEngine.Random.value * 10f, UnityEngine.Random.value * 10f, UnityEngine.Random.value * 10f)
        });
    }

    void LateUpdate()
    {
        if (_active.Count == 0)
        {
            if (lockToInitialLocalPosition)
                transform.localPosition = _initialLocalPosition;
            return;
        }

        float now = Time.time;

        Vector3 totalPos = Vector3.zero;
        Vector3 totalRot = Vector3.zero;

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var s = _active[i];
            float t = (now - s.startTime) / Mathf.Max(0.0001f, s.duration);
            if (t >= 1f)
            {
                _active.RemoveAt(i);
                continue;
            }

            // Simple fade-out envelope (smooth).
            float envelope = 1f - t;
            envelope = envelope * envelope * (3f - 2f * envelope); // SmoothStep
            float amp = s.amplitude * envelope;

            float timeNoise = now * s.frequency;

            float nx = NoiseSigned(s.seed.x, timeNoise);
            float ny = NoiseSigned(s.seed.y, timeNoise + 13.37f);
            float nz = NoiseSigned(s.seed.z, timeNoise + 37.91f);

            float rx = NoiseSigned(s.seed.x + 1.11f, timeNoise + 7.77f);
            float ry = NoiseSigned(s.seed.y + 2.22f, timeNoise + 19.19f);
            float rz = NoiseSigned(s.seed.z + 3.33f, timeNoise + 29.29f);

            totalPos += Vector3.Scale(new Vector3(nx, ny, nz), s.positionStrength) * amp;
            totalRot += Vector3.Scale(new Vector3(rx, ry, rz), s.rotationStrength) * amp;
        }

        // Position: keep it stable (no accumulation).
        if (lockToInitialLocalPosition)
            transform.localPosition = _initialLocalPosition + totalPos;
        else
            transform.localPosition = transform.localPosition + totalPos;

        // Rotation: additive on top of whatever FirstPersonLook/recoil already set this frame.
        // (FirstPersonLook writes localRotation in Update, we apply shake in LateUpdate.)
        Quaternion baseRotation = transform.localRotation;
        transform.localRotation = baseRotation * Quaternion.Euler(totalRot);
    }

    static float NoiseSigned(float seed, float t)
    {
        // PerlinNoise is [0..1]; remap to [-1..1]
        return Mathf.PerlinNoise(seed, t) * 2f - 1f;
    }
}
