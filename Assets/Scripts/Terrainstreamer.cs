using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 依玩家（或指定目標）與各 Terrain 之間的水平距離，動態啟用 / 停用 Terrain 物件，
/// 藉此降低遠處地形的渲染與物理負擔。
/// 掛在「Terrain Group」這種父物件上；會自動蒐集其底下所有 Terrain（含一開始為 inactive 的）。
/// 注意：SetActive(false) 只停掉渲染與物理，並不會釋放 TerrainData 佔用的記憶體。
/// </summary>
[DisallowMultipleComponent]
public class TerrainStreamer : MonoBehaviour
{
    [Header("追蹤目標")]
    [Tooltip("玩家 / 母艦 的 Transform。留空時若下方選項開啟，會自動改用 Camera.main。")]
    public Transform target;

    [Tooltip("target 為空時，是否自動改用主攝影機作為追蹤目標。")]
    public bool useMainCameraIfNull = true;

    [Header("距離設定（水平 XZ，世界單位）")]
    [Tooltip("目標與 Terrain 中心的水平距離小於此值 → 啟用。")]
    public float loadRadius = 3000f;

    [Tooltip("目標與 Terrain 中心的水平距離大於此值 → 停用。\n必須大於 loadRadius，兩者之間為遲滯區間，避免在邊界反覆開關。")]
    public float unloadRadius = 4000f;

    [Header("更新")]
    [Tooltip("每隔幾秒重新計算一次。設 0 代表每幀都算。")]
    public float checkInterval = 0.5f;

    [Header("除錯")]
    [Tooltip("選取此物件時，於 Scene 視窗畫出 load / unload 範圍圈。")]
    public bool drawGizmos = true;

    struct Tile
    {
        public GameObject go;
        public Vector3 center;   // 世界座標，快取值（假設地形為靜態，不隨執行期移動）
    }

    readonly List<Tile> _tiles = new List<Tile>();
    float _timer;
    bool _warned;

    void Awake()
    {
        RefreshTiles();
    }

    void Start()
    {
        ResolveTarget();
        if (target == null)
        {
            Debug.LogWarning("[TerrainStreamer] 找不到追蹤目標，所有 Terrain 將維持啟用。請在 Inspector 指定 target。", this);
            _warned = true;
            return;
        }

        Cull(true);                 // 建立初始狀態
        _timer = checkInterval;
    }

    void Update()
    {
        if (target == null)
        {
            if (!_warned) ResolveTarget();
            return;
        }

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        _timer = Mathf.Max(0f, checkInterval);
        Cull(false);
    }

    /// <summary>重新蒐集底下所有 Terrain（例如執行期新增地形後呼叫）。</summary>
    public void RefreshTiles()
    {
        _tiles.Clear();

        Terrain[] terrains = GetComponentsInChildren<Terrain>(true);
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain t = terrains[i];
            if (t == null) continue;

            _tiles.Add(new Tile
            {
                go = t.gameObject,
                center = ComputeCenter(t),
            });
        }
    }

    /// <summary>由外部（例如切換操控單位時）改變追蹤目標，並立即重建狀態。</summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        _warned = false;
        if (target != null) Cull(true);
    }

    void ResolveTarget()
    {
        if (target != null) return;
        if (useMainCameraIfNull && Camera.main != null)
            target = Camera.main.transform;
    }

    void Cull(bool initial)
    {
        if (target == null) return;

        Vector3 p = target.position;
        float loadSq = loadRadius * loadRadius;
        float unloadSq = unloadRadius * unloadRadius;

        for (int i = 0; i < _tiles.Count; i++)
        {
            GameObject go = _tiles[i].go;
            if (go == null) continue;

            Vector3 c = _tiles[i].center;
            float dx = c.x - p.x;
            float dz = c.z - p.z;
            float distSq = dx * dx + dz * dz;   // 只算水平距離，母艦拉高時不會誤關腳下地形

            bool active = go.activeSelf;
            bool shouldBeActive;

            if (initial)
                shouldBeActive = distSq <= loadSq;      // 初始化：以 loadRadius 為準
            else if (active)
                shouldBeActive = distSq <= unloadSq;    // 已啟用：超過 unloadRadius 才關
            else
                shouldBeActive = distSq <= loadSq;      // 已停用：進入 loadRadius 才開

            if (shouldBeActive != active)
                go.SetActive(shouldBeActive);
        }
    }

    static Vector3 ComputeCenter(Terrain t)
    {
        // Unity Terrain 的 transform.position 是「左下角」，加上一半尺寸才是中心
        if (t.terrainData != null)
        {
            Vector3 size = t.terrainData.size;
            return t.transform.position + new Vector3(size.x * 0.5f, 0f, size.z * 0.5f);
        }
        return t.transform.position;
    }

    [ContextMenu("Refresh Tiles")]
    void ContextRefresh() => RefreshTiles();

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Transform t = target != null
            ? target
            : (useMainCameraIfNull && Camera.main != null ? Camera.main.transform : null);
        if (t == null) return;

        Vector3 c = t.position;
        Gizmos.color = new Color(0.3f, 1f, 0.4f, 1f);
        DrawRingXZ(c, loadRadius);
        Gizmos.color = new Color(1f, 0.4f, 0.3f, 1f);
        DrawRingXZ(c, unloadRadius);
    }

    static void DrawRingXZ(Vector3 center, float radius, int segments = 64)
    {
        if (radius <= 0f) return;

        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float a = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}