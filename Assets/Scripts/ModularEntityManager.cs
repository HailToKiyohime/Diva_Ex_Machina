using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

/// <summary>
/// 集中更新所有 ModularEntity（敵人）的 Brain 與 Movement。
///
/// 為什麼要集中：
///   Unity 對每一個有 FixedUpdate 的 MonoBehaviour 都要各自從引擎呼叫一次。
///   400 多隻敵人 × Brain + Movement = 每個物理步 800 多次跨引擎呼叫，
///   光是呼叫本身的開銷就很可觀；改成這裡一個 FixedUpdate 用迴圈跑完，這部分開銷就消失了。
///
/// Brain 降頻：
///   Brain 不需要每個物理步都思考。預設每 0.1 秒思考一次，而且每隻敵人的思考時間點隨機錯開，
///   不會全部擠在同一步。思考之間的物理步，Movement 照常每步執行，
///   沿用 Brain 上次決定的移動方向、朝向、懸停高度。
///
/// 寫新的敵人時：
///   什麼都不用註冊。繼承 ModularEntityBrain / ModularEntityMovement，
///   基底類別的 OnEnable / OnDisable 會自動向這裡登記與移除。
///   要覆寫 OnEnable / OnDisable 的話，記得呼叫 base。
///   Brain 的每次思考寫在 Think()，Movement 的每步邏輯寫在 ManagedFixedUpdate(dt)。
///
/// 場景裡不用放這個元件：第一隻敵人登記時會自動建立。
/// 想在 Inspector 調整思考間隔，就自己在場景裡放一個。
/// </summary>
[DefaultExecutionOrder(-50)]
public class ModularEntityManager : MonoBehaviour
{
    [Tooltip("Brain 每隔多久思考一次（秒）。\n" +
             "0 = 每個物理步都思考（等於改動前的行為）。\n" +
             "0.1 通常看不出差別；敵人移動很快、需要更靈敏的反應時可以調低。")]
    [SerializeField, Min(0f)] private float brainThinkInterval = 0.1f;

    public static ModularEntityManager Instance { get; private set; }
    private static bool _quitting;

    private readonly ManagedList<ModularEntityBrain> _brains = new ManagedList<ModularEntityBrain>();
    private readonly ManagedList<ModularEntityMovement> _movements = new ManagedList<ModularEntityMovement>();

    // 在 Profiler 裡會看到這兩個名字，取代原本的 ModularEntityBrain.FixedUpdate() / ModularEntityMovement.FixedUpdate()
    private static readonly ProfilerMarker s_ThinkMarker = new ProfilerMarker("ModularEntity.BrainThink");
    private static readonly ProfilerMarker s_MoveMarker = new ProfilerMarker("ModularEntity.MovementTick");

    public float BrainThinkInterval => brainThinkInterval;
    public int BrainCount => _brains.Count;
    public int MovementCount => _movements.Count;

    // ── 靜態狀態重置（關閉 Domain Reload 時，靜態欄位會跨越 Play 保留下來） ──
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        _quitting = false;
        Application.quitting -= OnQuitting;
        Application.quitting += OnQuitting;
    }

    private static void OnQuitting() => _quitting = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"場景裡有多個 {nameof(ModularEntityManager)}，多出來的會被移除。", this);
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private static ModularEntityManager GetOrCreate()
    {
        if (Instance != null) return Instance;
        if (_quitting) return null;   // 關閉遊戲時不要再生新物件

        Instance = FindAnyObjectByType<ModularEntityManager>();
        if (Instance == null)
        {
            var go = new GameObject(nameof(ModularEntityManager));
            Instance = go.AddComponent<ModularEntityManager>();
        }
        return Instance;
    }

    // ═══════════════════════ 登記 / 移除 ═══════════════════════

    public static void Register(ModularEntityBrain brain)
    {
        ModularEntityManager m = GetOrCreate();
        if (m == null) return;

        if (m._brains.Add(brain))
        {
            // 第一次思考的時間隨機錯開，讓敵人平均分散在不同的物理步
            brain.ScheduleFirstThink(Time.fixedTime + UnityEngine.Random.Range(0f, m.brainThinkInterval));
        }
    }

    public static void Unregister(ModularEntityBrain brain)
    {
        if (Instance != null) Instance._brains.Remove(brain);
    }

    public static void Register(ModularEntityMovement movement)
    {
        ModularEntityManager m = GetOrCreate();
        if (m != null) m._movements.Add(movement);
    }

    public static void Unregister(ModularEntityMovement movement)
    {
        if (Instance != null) Instance._movements.Remove(movement);
    }

    // ═══════════════════════ 每個物理步 ═══════════════════════

    private void FixedUpdate()
    {
        float now = Time.fixedTime;
        float dt = Time.fixedDeltaTime;

        // 1. Brain：只有到時間的才思考。先跑 Brain，Movement 才會拿到這一步最新的決定。
        using (s_ThinkMarker.Auto())
        {
            _brains.BeginIteration();
            int count = _brains.Count;
            for (int i = 0; i < count; i++)
            {
                ModularEntityBrain brain = _brains[i];
                if (ReferenceEquals(brain, null)) continue;       // 這一步中途被移除的空位
                if (now < brain.NextThinkTime) continue;         // 還沒輪到它

                // 每隻敵人各自接住例外：原本一隻敵人出錯只會停掉它自己的 FixedUpdate，
                // 集中之後不接的話，一隻出錯就會讓後面所有敵人這一步都停擺。
                try { brain.RunThink(now, brainThinkInterval); }
                catch (Exception e) { Debug.LogException(e, brain); }
            }
            _brains.EndIteration();
        }

        // 2. Movement：每一步都跑（地面偵測、速度、轉向都需要連續）。
        using (s_MoveMarker.Auto())
        {
            _movements.BeginIteration();
            int count = _movements.Count;
            for (int i = 0; i < count; i++)
            {
                ModularEntityMovement movement = _movements[i];
                if (ReferenceEquals(movement, null)) continue;

                try { movement.ManagedFixedUpdate(dt); }
                catch (Exception e) { Debug.LogException(e, movement); }
            }
            _movements.EndIteration();
        }
    }

    // ═══════════════════════ 清單 ═══════════════════════

    /// <summary>
    /// 可以在走訪途中安全增刪的清單。
    ///
    /// 敵人會在別的敵人思考時死掉、被停用（例如被砲塔打死）。
    /// 走訪中移除只會把那一格設成空位，走完再一次壓縮；
    /// 走訪中新增會接在尾端，這一步不處理，下一步才開始跑。
    /// 每個元素記住自己的索引（IManagedEntity.ManagerIndex），移除是 O(1)。
    /// </summary>
    private class ManagedList<T> where T : class, IManagedEntity
    {
        private readonly List<T> _items = new List<T>(512);
        private bool _iterating;
        private bool _hasHoles;

        public int Count => _items.Count;
        public T this[int i] => _items[i];

        public bool Add(T item)
        {
            if (item == null || item.ManagerIndex >= 0) return false;   // 已經登記過
            item.ManagerIndex = _items.Count;
            _items.Add(item);
            return true;
        }

        public void Remove(T item)
        {
            if (item == null) return;
            int idx = item.ManagerIndex;
            if (idx < 0 || idx >= _items.Count || !ReferenceEquals(_items[idx], item)) return;

            item.ManagerIndex = -1;

            if (_iterating)
            {
                _items[idx] = null;     // 走訪中：先留空位，走完再壓縮
                _hasHoles = true;
                return;
            }

            // 不在走訪中：把最後一個搬過來補位
            int last = _items.Count - 1;
            if (idx != last)
            {
                T moved = _items[last];
                _items[idx] = moved;
                moved.ManagerIndex = idx;
            }
            _items.RemoveAt(last);
        }

        public void BeginIteration() => _iterating = true;

        public void EndIteration()
        {
            _iterating = false;
            if (!_hasHoles) return;
            _hasHoles = false;

            int write = 0;
            for (int read = 0; read < _items.Count; read++)
            {
                T item = _items[read];
                if (ReferenceEquals(item, null)) continue;
                _items[write] = item;
                item.ManagerIndex = write;
                write++;
            }
            _items.RemoveRange(write, _items.Count - write);
        }
    }
}

/// <summary>由 ModularEntityManager 集中更新的元件。ManagerIndex 只給管理器用，-1 = 沒登記。</summary>
public interface IManagedEntity
{
    int ManagerIndex { get; set; }
}
