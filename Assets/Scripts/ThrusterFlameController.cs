using UnityEngine;

/// <summary>
/// Drives a layered thruster flame built by ThrusterFlameBuilder
/// (Tools > Diva Ex Machina > Thruster Flame Builder).
///
/// Layers (each is a ParticleSystem child assigned by the builder):
///   Core        - short white-hot jet            (local space)
///   Flame       - main tongue                    (local space)
///   Outer       - soft wide flame, secondary colour (local space)
///   NozzleGlow  - flickering banded disc at the nozzle
///   Sparks      - thin fast streaks              (world space)
///   Rings       - shock rings, high throttle and Boost() (local space)
///   MotionTrail - world-space ribbon: the continuous streak left behind in flight
///
/// This script never touches startColor — colours belong to EffectColorController
/// on the same object, so the existing ColorPicker can recolour the flame
/// exactly like it recolours weapon coating effects.
///
/// Hooking it to gameplay later (no UI / movement wiring yet):
///   SetThrottle(0..1)  - idle hover (0) to full boost (1); smoothed internally
///   Boost()            - one-shot burst: shock rings + sparks + brief overdrive
///   SetEngineOn(bool)  - ignite / cut the engine (particles die out naturally)
/// </summary>
[DisallowMultipleComponent]
public class ThrusterFlameController : MonoBehaviour
{
    [Header("Layers (assigned by ThrusterFlameBuilder)")]
    [SerializeField] private ParticleSystem core;
    [SerializeField] private ParticleSystem flame;
    [SerializeField] private ParticleSystem outer;
    [SerializeField] private ParticleSystem nozzleGlow;
    [SerializeField] private ParticleSystem sparks;
    [SerializeField] private ParticleSystem rings;
    [SerializeField] private ParticleSystem trail;

    [Header("Throttle")]
    [Range(0f, 1f)]
    [SerializeField] private float throttle = 0.35f;

    [Tooltip("How fast the visual throttle follows SetThrottle (per second).")]
    [SerializeField] private float throttleResponse = 10f;

    [Tooltip("Flame length / emission at throttle 0, as a fraction of full boost.")]
    [Range(0f, 1f)]
    [SerializeField] private float idleScale = 0.4f;

    [Header("Shock Rings")]
    [Tooltip("Rings start appearing above this throttle.")]
    [Range(0f, 1f)]
    [SerializeField] private float ringThrottleThreshold = 0.75f;
    [SerializeField] private float ringRateAtFullThrottle = 9f;

    [Header("Boost()")]
    [SerializeField] private int boostRingCount = 3;
    [SerializeField] private float boostRingInterval = 0.05f;
    [SerializeField] private int boostSparkCount = 14;
    [Tooltip("Temporary throttle overshoot right after Boost(), e.g. 1.3 = 30% longer flame.")]
    [SerializeField] private float boostOvershoot = 1.3f;
    [SerializeField] private float boostOvershootDuration = 0.25f;

    [Header("Flicker")]
    [Tooltip("Random shimmer on length / glow size. Small values keep it anime-clean.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float flickerAmount = 0.1f;
    [SerializeField] private float flickerSpeed = 18f;

    private struct Baseline
    {
        public float lifetime, speed, size, rate, rateDistance, trailWidth;
    }

    private ParticleSystem[] layers;
    private Baseline[] baselines;
    private float visualThrottle;
    private float overshootTimer;
    private int pendingRings;
    private float ringTimer;
    private float noiseSeed;
    private bool engineOn = true;

    public float Throttle => throttle;
    public bool EngineOn => engineOn;

    private void Awake()
    {
        layers = new[] { core, flame, outer, nozzleGlow, sparks, rings, trail };
        baselines = new Baseline[layers.Length];

        for (int i = 0; i < layers.Length; i++)
        {
            var ps = layers[i];
            if (ps == null) continue;

            var main = ps.main;
            var emission = ps.emission;
            baselines[i] = new Baseline
            {
                lifetime = main.startLifetimeMultiplier,
                speed = main.startSpeedMultiplier,
                size = main.startSizeMultiplier,
                rate = emission.rateOverTimeMultiplier,
                rateDistance = emission.rateOverDistanceMultiplier,
                trailWidth = ps.trails.widthOverTrailMultiplier
            };
        }

        visualThrottle = throttle;
        noiseSeed = Random.value * 100f;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        visualThrottle = Mathf.MoveTowards(visualThrottle, throttle, throttleResponse * dt);

        float t = visualThrottle;
        if (overshootTimer > 0f)
        {
            overshootTimer -= dt;
            float k = Mathf.Clamp01(overshootTimer / Mathf.Max(0.0001f, boostOvershootDuration));
            t = Mathf.Lerp(t, boostOvershoot, k);
        }

        float scale = Mathf.Lerp(idleScale, 1f, Mathf.Clamp01(t)) * Mathf.Max(1f, t);
        float flicker = 1f + (Mathf.PerlinNoise(noiseSeed, Time.time * flickerSpeed) - 0.5f) * 2f * flickerAmount;

        // Length reads from lifetime * speed; emission density follows throttle.
        ApplyLayer(0, lengthMul: scale * flicker, rateMul: scale, sizeMul: 1f);                 // core
        ApplyLayer(1, lengthMul: scale * flicker, rateMul: scale, sizeMul: 1f);                 // flame
        ApplyLayer(2, lengthMul: scale, rateMul: scale, sizeMul: Mathf.Lerp(0.8f, 1f, t));      // outer
        ApplyLayer(3, lengthMul: 1f, rateMul: 1f, sizeMul: Mathf.Lerp(0.6f, 1.1f, t) * flicker); // nozzle glow
        ApplyLayer(4, lengthMul: scale, rateMul: t * t, sizeMul: 1f);                          // sparks

        UpdateRings(t, dt);
        UpdateTrail(t);
    }

    private void UpdateTrail(float t)
    {
        if (trail == null) return;

        var b = baselines[6];
        var emission = trail.emission;
        emission.rateOverTimeMultiplier = engineOn ? b.rate : 0f;
        emission.rateOverDistanceMultiplier = engineOn ? b.rateDistance : 0f;

        var trails = trail.trails;
        trails.widthOverTrailMultiplier = b.trailWidth * Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(t));
    }

    private void ApplyLayer(int index, float lengthMul, float rateMul, float sizeMul)
    {
        var ps = layers[index];
        if (ps == null) return;

        var b = baselines[index];
        var main = ps.main;
        var emission = ps.emission;

        // Split the length change between lifetime and speed so particles
        // don't get too sparse at full boost or too dense at idle.
        float half = Mathf.Sqrt(Mathf.Max(0f, lengthMul));
        main.startLifetimeMultiplier = b.lifetime * half;
        main.startSpeedMultiplier = b.speed * half;
        main.startSizeMultiplier = b.size * sizeMul;
        emission.rateOverTimeMultiplier = engineOn ? b.rate * rateMul : 0f;
    }

    private void UpdateRings(float t, float dt)
    {
        if (rings == null) return;

        var emission = rings.emission;
        float over = Mathf.InverseLerp(ringThrottleThreshold, 1f, t);
        emission.rateOverTimeMultiplier = (engineOn && t > ringThrottleThreshold) ? ringRateAtFullThrottle * over : 0f;

        if (pendingRings > 0)
        {
            ringTimer -= dt;
            if (ringTimer <= 0f)
            {
                rings.Emit(1);
                pendingRings--;
                ringTimer = boostRingInterval;
            }
        }
    }

    // -----------------------------
    // Public API
    // -----------------------------

    /// <summary>0 = idle hover, 1 = full boost.</summary>
    public void SetThrottle(float value)
    {
        throttle = Mathf.Clamp01(value);
    }

    /// <summary>One-shot dash burst: shock rings, extra sparks and a short overdrive.</summary>
    public void Boost()
    {
        if (!engineOn) return;

        overshootTimer = boostOvershootDuration;
        pendingRings = Mathf.Max(pendingRings, boostRingCount);
        ringTimer = 0f;

        if (sparks != null && boostSparkCount > 0)
            sparks.Emit(boostSparkCount);
    }

    /// <summary>Ignite or cut the engine. Cutting lets live particles finish instead of popping.</summary>
    public void SetEngineOn(bool on)
    {
        engineOn = on;

        foreach (var ps in layers)
        {
            if (ps == null) continue;
            if (on && !ps.isPlaying) ps.Play(false);
        }

        if (!on) pendingRings = 0;
    }
}
