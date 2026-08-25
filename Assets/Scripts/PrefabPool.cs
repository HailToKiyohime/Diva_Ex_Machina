using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 池化物件可以實作這個介面，在被取出 / 歸還時收到通知。
///
/// Bullet 已經有簽章一致的 OnSpawned() / OnDespawned()，只要在類別宣告後面加上
/// ", IPooled" 就會自動接上。
///
/// 注意：Spawn 會先 SetActive(true) 再呼叫 OnSpawned()，所以 OnEnable 一定比它早跑。
/// Bullet 用 _live 旗標讓兩條路徑只會真正初始化一次。
/// </summary>
public interface IPooled
{
    void OnSpawned();
    void OnDespawned();
}

/// <summary>
/// 以 prefab 為 key 的通用物件池。
///
/// 手動放到場景中的一個 GameObject 上（放哪裡都行，通常跟 GameManager 同一個物件）。
/// 不是 DontDestroyOnLoad —— 池子跟著場景一起死，換場景不會有上一局的子彈飛過來。
///
/// 沒放進場景時，Spawn / Despawn 會退回 Instantiate / Destroy 並警告一次，
/// 所以測試場景忘了放只是失去池化，不會壞掉。
/// </summary>
[DisallowMultipleComponent]
public class PrefabPool : MonoBehaviour
{
    [System.Serializable]
    public struct PrewarmEntry
    {
        public GameObject prefab;
        [Min(0)] public int count;
    }

    [Header("Prewarm")]
    [Tooltip("進場景時預先生成。放子彈、飛彈、命中特效這類會大量生滅的 prefab。\n" +
             "數量抓「同時在場上的尖峰值」即可，不夠時池子會自己長大。")]
    [SerializeField] private List<PrewarmEntry> prewarm = new List<PrewarmEntry>();

    [Tooltip("每幀最多建立幾個，避免預熱本身變成一次卡頓。\n設成很大的數字 = 在 Start 當幀一次做完。")]
    [SerializeField] private int prewarmPerFrame = 20;

    [Header("Limits")]
    [Tooltip("每個 prefab 最多保留幾個閒置實例。超過的在 Despawn 時直接銷毀。\n" +
             "0 = 不限制（無限穿透 + 長壽命的子彈有機會讓池子無限長大，建議留著）")]
    [SerializeField] private int maxIdlePerPrefab = 256;

    [Header("Debug")]
    [SerializeField] private bool logPrewarmResult = false;

    // ──────────────────────────────────────────────────────────────

    private static PrefabPool _instance;
    private static bool _missingWarned;

    private readonly Dictionary<GameObject, Stack<GameObject>> _idle =
        new Dictionary<GameObject, Stack<GameObject>>();

    private Transform _root;

    /// <summary>場景中是否有可用的池子。</summary>
    public static bool IsAvailable => _instance != null;

    // ═══════════════════════ 生命週期 ═══════════════════════

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"[PrefabPool] 場景裡已經有一個 PrefabPool 了，移除這個重複的。", this);
            Destroy(this);   // 只移除元件，不動 GameObject（它可能還掛著別的東西）
            return;
        }

        _instance = this;

        // 容器獨立成 root 物件，不掛在自己底下 ——
        // PrefabPool 這個元件如果被放在一個會移動 / 縮放的物件上（例如掛在船上），
        // 躺在池子裡的物件會跟著被拖著跑。
        var rootGo = new GameObject("[PrefabPool] Idle");
        rootGo.SetActive(false);   // ★ 容器保持停用，見 CreateInstance 的說明
        _root = rootGo.transform;
        _root.SetParent(null);
        _root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        _root.localScale = Vector3.one;
    }

    private void Start()
    {
        if (_instance != this) return;
        StartCoroutine(PrewarmRoutine());
    }

    private void OnDestroy()
    {
        if (_instance != this) return;

        _instance = null;
        _missingWarned = false;   // 換場景後允許再警告一次

        if (_root != null) Destroy(_root.gameObject);
    }

    // ═══════════════════════ 對外 API ═══════════════════════

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        => Spawn(prefab, position, rotation, null);

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (prefab == null) return null;

        if (_instance == null)
        {
            WarnMissing();
            return Instantiate(prefab, position, rotation, parent);
        }

        return _instance.SpawnInternal(prefab, position, rotation, parent);
    }

    /// <summary>
    /// 歸還一個實例。回傳 true = 真的回到池子；false = 被直接銷毀
    /// （沒有池子、或這個物件不是池子生的）。
    /// </summary>
    public static bool Despawn(GameObject instance)
    {
        if (instance == null) return false;

        PooledInstance pooled = instance.GetComponent<PooledInstance>();

        // 不是池子生的（例如池子缺席時用 Instantiate 生的退路物件），或池子已經沒了
        if (pooled == null || pooled.sourcePrefab == null || _instance == null)
        {
            Destroy(instance);
            return false;
        }

        return _instance.DespawnInternal(instance, pooled);
    }

    /// <summary>手動預熱。可以在載入畫面裡針對這一關會用到的 prefab 呼叫。</summary>
    public static void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;

        if (_instance == null) { WarnMissing(); return; }

        Stack<GameObject> stack = _instance.GetOrCreateStack(prefab);
        for (int i = 0; i < count; i++)
        {
            if (_instance.maxIdlePerPrefab > 0 && stack.Count >= _instance.maxIdlePerPrefab) break;

            GameObject go = _instance.CreateInstance(prefab);
            if (go == null) break;
            stack.Push(go);
        }
    }

    /// <summary>回收場上所有還活著的池化物件。切換關卡 / 重開一局時可以用。</summary>
    public static void DespawnAll()
    {
        if (_instance == null) return;

        // 不是熱路徑，用 FindObjectsByType 沒關係
        PooledInstance[] all = FindObjectsByType<PooledInstance>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null || all[i].isIdle) continue;
            Despawn(all[i].gameObject);
        }
    }

    /// <summary>某個 prefab 目前有幾個閒置實例（除錯用）。</summary>
    public static int IdleCount(GameObject prefab)
    {
        if (_instance == null || prefab == null) return 0;
        return _instance._idle.TryGetValue(prefab, out Stack<GameObject> s) ? s.Count : 0;
    }

    // ═══════════════════════ 內部實作 ═══════════════════════

    private GameObject SpawnInternal(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        GameObject go = null;

        if (_idle.TryGetValue(prefab, out Stack<GameObject> stack))
        {
            // 池子裡可能有被外部 Destroy 掉的殘骸（某段程式碼忘了改成 Despawn），
            // Unity 的假 null 會讓它們看起來還在。一路 pop 到找出活的為止。
            while (stack.Count > 0 && go == null)
                go = stack.Pop();
        }

        if (go == null)
        {
            go = CreateInstance(prefab);
            if (go == null) return null;
        }

        // ★ 順序：先擺好 transform，最後才啟用。
        //   反過來的話物件會在「上次死亡的位置」活過來一瞬間 ——
        //   拖尾會拉一條線橫跨地圖，OnTriggerEnter 也可能在錯的地方觸發。
        Transform t = go.transform;
        t.SetParent(parent, false);
        t.SetPositionAndRotation(position, rotation);

        PooledInstance pooled = go.GetComponent<PooledInstance>();
        if (pooled != null) pooled.isIdle = false;

        go.SetActive(true);   // → Awake（第一次）→ OnEnable

        if (pooled != null) Notify(pooled.hooks, true);

        return go;
    }

    private bool DespawnInternal(GameObject go, PooledInstance pooled)
    {
        if (pooled.isIdle) return true;   // 同一幀被 Despawn 第二次，安靜跳過
        pooled.isIdle = true;

        Notify(pooled.hooks, false);

        go.SetActive(false);              // → OnDisable

        // 停用之後再搬，transform 階層的更新比較便宜
        go.transform.SetParent(_root, false);

        Stack<GameObject> stack = GetOrCreateStack(pooled.sourcePrefab);

        // 池子已經夠大 → 多的直接丟掉，不要讓它無限長
        if (maxIdlePerPrefab > 0 && stack.Count >= maxIdlePerPrefab)
        {
            Destroy(go);
            return false;
        }

        stack.Push(go);
        return true;
    }

    private GameObject CreateInstance(GameObject prefab)
    {
        // 生在「停用的容器」底下 → 這個實例從頭到尾沒有 active 過：
        // Awake / OnEnable 不會跑、playOnAwake 的粒子不會在原點噴一下、
        // 也不會有一幀的物理接觸。預熱 200 顆子彈不會在畫面中央閃出一片火花。
        GameObject go = Instantiate(prefab, _root);

        // 明確關掉自己的 activeSelf。少了這行，之後 SetParent 脫離停用容器的瞬間
        // 它就會自己活過來 —— 而且是在還沒擺好位置的時候。
        go.SetActive(false);

        PooledInstance pooled = go.GetComponent<PooledInstance>();
        if (pooled == null) pooled = go.AddComponent<PooledInstance>();

        pooled.sourcePrefab = prefab;
        pooled.isIdle = true;
        pooled.hooks = go.GetComponentsInChildren<IPooled>(true);

        return go;
    }

    private Stack<GameObject> GetOrCreateStack(GameObject prefab)
    {
        if (!_idle.TryGetValue(prefab, out Stack<GameObject> stack))
        {
            stack = new Stack<GameObject>();
            _idle[prefab] = stack;
        }
        return stack;
    }

    private static void Notify(IPooled[] hooks, bool spawned)
    {
        if (hooks == null) return;

        for (int i = 0; i < hooks.Length; i++)
        {
            // ★ 不能只寫 hooks[i] != null。
            //   Unity 的「假 null」是 UnityEngine.Object 的 == 多載做出來的；
            //   透過 interface 型別比較會走 C# 原生的參考比較，
            //   已經被 Destroy 的 MonoBehaviour 在這裡看起來仍然「不是 null」。
            //   先轉回 UnityEngine.Object 才會走到那個多載。
            var obj = hooks[i] as UnityEngine.Object;
            if (obj == null) continue;

            if (spawned) hooks[i].OnSpawned();
            else hooks[i].OnDespawned();
        }
    }

    private IEnumerator PrewarmRoutine()
    {
        int perFrame = Mathf.Max(1, prewarmPerFrame);
        int madeThisFrame = 0;

        for (int e = 0; e < prewarm.Count; e++)
        {
            PrewarmEntry entry = prewarm[e];
            if (entry.prefab == null || entry.count <= 0) continue;

            Stack<GameObject> stack = GetOrCreateStack(entry.prefab);

            for (int i = 0; i < entry.count; i++)
            {
                if (maxIdlePerPrefab > 0 && stack.Count >= maxIdlePerPrefab) break;

                GameObject go = CreateInstance(entry.prefab);
                if (go == null) break;
                stack.Push(go);

                if (++madeThisFrame >= perFrame)
                {
                    madeThisFrame = 0;
                    yield return null;
                }
            }
        }

        if (logPrewarmResult) LogStats();
    }

    private static void WarnMissing()
    {
        if (_missingWarned) return;
        _missingWarned = true;

        Debug.LogWarning(
            "[PrefabPool] 場景裡找不到 PrefabPool，這次退回 Instantiate / Destroy。\n" +
            "把 PrefabPool 元件掛到場景中任一個 GameObject 上即可啟用池化。（本訊息每個場景只出現一次）");
    }

    [ContextMenu("Log Pool Stats")]
    private void LogStats()
    {
        var sb = new System.Text.StringBuilder("[PrefabPool] 閒置實例：\n");
        foreach (KeyValuePair<GameObject, Stack<GameObject>> kv in _idle)
        {
            string prefabName = (kv.Key != null) ? kv.Key.name : "<missing prefab>";
            sb.Append("  ").Append(prefabName).Append(" : ").Append(kv.Value.Count).Append('\n');
        }
        Debug.Log(sb.ToString(), this);
    }
}