using UnityEngine;

/// <summary>
/// 跳躍 / 落地塵土特效（由 Tools > Diva Ex Machina > Jump Dust Builder 產生的 prefab 根物件）。
///
/// 圖層（各自是子物件上的 ParticleSystem，平常不發射，只在 Play() 時用 Emit 噴出）：
///   Burst   - 貼著地面向外推開的一圈塵團（主體）
///   Cloud   - 中央往上翻起的大塵團
///   Debris  - 被踢起來、受重力落下的碎石
///   Streaks - 貼地往外射的速度線（強度夠高才出現）
///   Ring    - 地面上擴散的衝擊環（強度夠高才出現）
///
/// 風格：塵團本身是動畫式的分色塊（貼圖三階明暗、邊緣俐落），
/// 運動則是寫實的 —— 先快速推開再被空氣阻力拖住、慢慢上浮散開。
///
/// 顏色由呼叫端傳入（PlayerJumpDust 會依地面取色），各圖層再乘上自己的 Tint。
/// </summary>
[DisallowMultipleComponent]
public class JumpDustEffect : MonoBehaviour
{
    [Header("Layers (assigned by JumpDustBuilder)")]
    [SerializeField] private ParticleSystem burst;
    [SerializeField] private ParticleSystem cloud;
    [SerializeField] private ParticleSystem debris;
    [SerializeField] private ParticleSystem streaks;
    [SerializeField] private ParticleSystem ring;

    [Header("Counts at Intensity 1")]
    [SerializeField, Min(0)] private int burstCount = 14;
    [SerializeField, Min(0)] private int cloudCount = 5;
    [SerializeField, Min(0)] private int debrisCount = 10;
    [SerializeField, Min(0)] private int streakCount = 12;

    [Header("Intensity Response")]
    [Tooltip("強度 0 時的粒子數量，是強度 1 的幾倍。")]
    [SerializeField, Range(0f, 1f)] private float minCountScale = 0.35f;
    [Tooltip("x = 強度 0 的速度倍率，y = 強度 1 的速度倍率。速度越快，塵土推得越遠。")]
    [SerializeField] private Vector2 speedScale = new Vector2(0.55f, 1.25f);
    [Tooltip("x = 強度 0 的大小倍率，y = 強度 1 的大小倍率。")]
    [SerializeField] private Vector2 sizeScale = new Vector2(0.7f, 1.3f);
    [Tooltip("強度高於這個值才會出現速度線。")]
    [SerializeField, Range(0f, 1f)] private float streakMinIntensity = 0.45f;
    [Tooltip("強度高於這個值才會出現地面衝擊環。")]
    [SerializeField, Range(0f, 1f)] private float ringMinIntensity = 0.3f;

    [Header("Colour (multiplied with the ground colour)")]
    [SerializeField] private Color burstTint = Color.white;
    [SerializeField] private Color cloudTint = new Color(0.94f, 0.94f, 0.94f, 0.9f);
    [SerializeField] private Color debrisTint = new Color(0.55f, 0.52f, 0.48f, 1f);
    [SerializeField] private Color streakTint = new Color(1.15f, 1.15f, 1.15f, 0.85f);
    [SerializeField] private Color ringTint = new Color(1.1f, 1.1f, 1.1f, 0.7f);

    [Tooltip("同一次噴發裡，粒子顏色在「顏色 × 這個值」到「顏色」之間隨機，讓塵團有深淺變化。")]
    [SerializeField, Range(0.5f, 1f)] private float colorVariation = 0.88f;

    private struct Baseline { public float speed, size; }

    private ParticleSystem[] _layers;
    private Baseline[] _baselines;
    private Transform _currentSpace;
    private bool _spaceInitialized;

    private void Awake()
    {
        _layers = new[] { burst, cloud, debris, streaks, ring };
        _baselines = new Baseline[_layers.Length];

        for (int i = 0; i < _layers.Length; i++)
        {
            ParticleSystem ps = _layers[i];
            if (ps == null) continue;

            var main = ps.main;
            _baselines[i] = new Baseline
            {
                speed = main.startSpeedMultiplier,
                size = main.startSizeMultiplier,
            };
        }
    }

    /// <summary>
    /// 在地面上噴一次塵土。
    /// </summary>
    /// <param name="position">地面接觸點。</param>
    /// <param name="normal">地面法線，塵土會沿著這個平面往外推。</param>
    /// <param name="intensity">0 = 輕輕一跳，1 = 高處重重落地。</param>
    /// <param name="color">地面顏色（各圖層會再乘上自己的 Tint）。</param>
    /// <param name="space">站在移動平台（船）上時傳入平台的 Transform，塵土會跟著平台移動；地面傳 null。</param>
    public void Play(Vector3 position, Vector3 normal, float intensity, Color color, Transform space = null)
    {
        if (_layers == null) Awake();

        if (normal.sqrMagnitude < 0.0001f) normal = Vector3.up;
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, normal) *
                         Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);   // 每次轉一下，塵團排列不會重複
        transform.SetPositionAndRotation(position + normal * 0.03f, rot);

        SetSimulationSpace(space);

        float k = Mathf.Clamp01(intensity);
        float countScale = Mathf.Lerp(minCountScale, 1f, k);
        float speedMul = Mathf.Lerp(speedScale.x, speedScale.y, k);
        float sizeMul = Mathf.Lerp(sizeScale.x, sizeScale.y, k);

        EmitLayer(0, Count(burstCount, countScale), speedMul, sizeMul, color * burstTint);
        EmitLayer(1, Count(cloudCount, countScale), speedMul, sizeMul, color * cloudTint);
        EmitLayer(2, Count(debrisCount, countScale), speedMul, sizeMul, color * debrisTint);

        if (k >= streakMinIntensity)
            EmitLayer(3, Count(streakCount, countScale), speedMul, sizeMul, color * streakTint);

        if (k >= ringMinIntensity)
            EmitLayer(4, 1, 1f, sizeMul, color * ringTint);
    }

    private static int Count(int baseCount, float scale)
    {
        return baseCount <= 0 ? 0 : Mathf.Max(1, Mathf.RoundToInt(baseCount * scale));
    }

    private void EmitLayer(int index, int count, float speedMul, float sizeMul, Color color)
    {
        ParticleSystem ps = _layers[index];
        if (ps == null || count <= 0) return;

        Baseline b = _baselines[index];
        var main = ps.main;
        main.startSpeedMultiplier = b.speed * speedMul;
        main.startSizeMultiplier = b.size * sizeMul;

        color.r = Mathf.Clamp01(color.r);
        color.g = Mathf.Clamp01(color.g);
        color.b = Mathf.Clamp01(color.b);
        color.a = Mathf.Clamp01(color.a);
        Color darker = new Color(color.r * colorVariation, color.g * colorVariation, color.b * colorVariation, color.a);
        main.startColor = new ParticleSystem.MinMaxGradient(darker, color);

        ps.Emit(count);
    }

    /// <summary>
    /// 地面：World 空間。船上：以船為參考的 Custom 空間，塵土會跟著船走，不會被甩在後面。
    /// 空間切換只在真的改變時做（切換會讓還活著的粒子跳位）。
    /// </summary>
    private void SetSimulationSpace(Transform space)
    {
        if (_spaceInitialized && space == _currentSpace) return;
        _spaceInitialized = true;
        _currentSpace = space;

        for (int i = 0; i < _layers.Length; i++)
        {
            ParticleSystem ps = _layers[i];
            if (ps == null) continue;

            var main = ps.main;
            if (space != null)
            {
                main.simulationSpace = ParticleSystemSimulationSpace.Custom;
                main.customSimulationSpace = space;
            }
            else
            {
                main.simulationSpace = ParticleSystemSimulationSpace.World;
            }
        }
    }
}
