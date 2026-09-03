using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敵人生成器。
///
/// 每隔一段隨機時間，按權重抽一個 SpawnGroup，把那一組裡面的敵人全部生出來，
/// 並註冊到 GameManager.enemies。
///
/// 生成需要同時滿足三個條件：玩家在 activeRange 內、存活數未達上限、
/// 以及（canSpawnInSight = false 時）生成器不在玩家視野內。
///
/// 任何一個條件不成立時，計時器都「不會」重置 —— 它停在 0 以下，
/// 條件一恢復就在同一幀補上生成，被擋住的等待時間不會白白浪費。
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    /// <summary>
    /// 一個生成組。被抽中時，spawnEnemy 裡的敵人會「全部」生成，不是從中挑一個。
    /// </summary>
    [System.Serializable]
    public class SpawnGroup
    {
        [Tooltip("這一組被抽中的權重。是相對值 —— 權重 2 的組被抽中的機率是權重 1 的兩倍。\n" +
                 "0 或負數 = 這一組永遠不會被抽中。")]
        public float spawnWeight = 1f;

        [Tooltip("這一組被抽中時要生成的所有敵人。")]
        public GameObject[] spawnEnemy;
    }

    [Header("Spawn Groups")]
    [SerializeField] private SpawnGroup[] spawnGroups;

    [Header("Timing")]
    [Tooltip("兩次生成之間的間隔（秒）。x = 最小值，y = 最大值。")]
    [SerializeField] private Vector2 spawnTimeRange = new Vector2(5f, 10f);

    [Header("Visibility")]
    [Tooltip("false = 生成器在玩家視野內時不生成，等玩家把視線轉開才生成。\n" +
             "true  = 玩家看著也照樣生成。")]
    [SerializeField] private bool canSpawnInSight = false;

    [Tooltip("留空會自動抓 Camera.main。")]
    [SerializeField] private Camera playerCamera;

    [Header("Limits")]
    [Tooltip("這個生成器同時存活在場上的敵人數量上限。達到上限就暫停，等敵人死掉才恢復。\n" +
             "只在「生成一組之前」檢查一次，所以一組多隻時最終數量可能略高於上限。\n" +
             "0 或負數 = 不限制。")]
    [SerializeField] private int maxAliveEnemies = 10;

    [Header("Active Range")]
    [Tooltip("玩家在這個距離內才會生成敵人。用 3D 直線距離。\n" +
             "0 或負數 = 不限制距離。")]
    [SerializeField] private float activeRange = 100f;

    [Tooltip("玩家的 Transform。留空會退而使用相機位置當作玩家位置 ——\n" +
             "第三人稱下相機在玩家後方幾公尺，對 activeRange 這種尺度通常夠用。")]
    [SerializeField] private Transform playerTransform;

    private float _spawnTimer;

    // 這個生成器生出來、目前還活著的敵人。
    // 敵人死亡時是被 Destroy 的（ModularEntityStats），所以靠 Unity 的假 null
    // 就能判斷存活，不需要任何死亡通知機制。
    private readonly List<GameObject> _spawned = new List<GameObject>();

    private void OnEnable()
    {
        RollTimer();
    }

    private void Update()
    {
        if (_spawnTimer > 0f)
        {
            _spawnTimer -= Time.deltaTime;
            return;
        }

        // ★ 以下三個條件任何一個不成立都直接 return，刻意「不」重置計時器。
        //   計時器停在 0 以下，條件一恢復就在同一幀補上生成，
        //   而不是重新等一輪。被擋住的時間不會白白浪費。
        //
        //   順序由便宜到貴：IsPlayerInRange 只是一次距離平方比較，
        //   CountAlive 要走訪清單，所以放在後面。
        if (!IsPlayerInRange()) return;
        if (IsAtAliveCap()) return;
        if (!canSpawnInSight && IsVisibleToPlayer()) return;

        SpawnOnce();
        RollTimer();
    }

    private void RollTimer()
    {
        float min = Mathf.Max(0f, spawnTimeRange.x);
        float max = Mathf.Max(min, spawnTimeRange.y);
        _spawnTimer = Random.Range(min, max);
    }

    /// <summary>玩家在不在 activeRange 內。</summary>
    private bool IsPlayerInRange()
    {
        if (activeRange <= 0f) return true;   // 不限制距離

        Transform player = ResolvePlayer();

        // ★ 找不到玩家 → 回傳 true（不擋）。
        //   跟 IsVisibleToPlayer 同一個規則：評估不了的限制條件就不套用。
        //   反過來（找不到就永不生成）會變成一個沒有任何錯誤訊息的靜默失效。
        if (player == null) return true;

        return (player.position - transform.position).sqrMagnitude <= activeRange * activeRange;
    }

    private Transform ResolvePlayer()
    {
        if (playerTransform != null) return playerTransform;

        // 退路：用相機位置近似玩家位置。第三人稱下相機在玩家後方幾公尺，
        // 對 activeRange 這種數十公尺的尺度通常夠用。要精準就把 playerTransform 指好。
        Camera cam = ResolveCamera();
        return (cam != null) ? cam.transform : null;
    }

    /// <summary>這個生成器的存活敵人數是否已達上限。</summary>
    private bool IsAtAliveCap()
    {
        if (maxAliveEnemies <= 0) return false;   // 不限制
        return CountAlive() >= maxAliveEnemies;
    }

    /// <summary>
    /// 清掉已被銷毀的項目，回傳還活著的數量。
    ///
    /// 只在計時器歸零那一幀才會被呼叫，所以這個 O(n) 走訪的成本可以忽略 ——
    /// n 本身也被 maxAliveEnemies 綁住了。
    /// </summary>
    private int CountAlive()
    {
        // 倒著走，RemoveAt 才不會跳過元素
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            if (_spawned[i] == null) _spawned.RemoveAt(i);
        }
        return _spawned.Count;
    }

    /// <summary>
    /// 生成器現在在不在玩家相機的視錐內。
    ///
    /// 只做視錐判定，不做遮擋判定 —— 生成器躲在牆後面但落在畫面範圍內時，
    /// 這裡仍然算「看得到」，會擋住生成。
    /// </summary>
    private bool IsVisibleToPlayer()
    {
        Camera cam = ResolveCamera();
        if (cam == null) return false;   // 沒有相機 = 沒有人在看，不擋

        Vector3 vp = cam.WorldToViewportPoint(transform.position);

        // z <= 0 代表在相機背後。WorldToViewportPoint 對背後的點
        // 仍然會回傳看似合法的 x/y，所以這個檢查不能省。
        return vp.z > 0f
            && vp.x >= 0f && vp.x <= 1f
            && vp.y >= 0f && vp.y <= 1f;
    }

    /// <summary>
    /// 每次都確認一下相機還活著。
    /// 專案裡有四組 camera set 會被 UIManager 開開關關，
    /// 快取住的那台可能已經被 SetActive(false) 了。
    /// </summary>
    private Camera ResolveCamera()
    {
        if (playerCamera == null || !playerCamera.isActiveAndEnabled)
            playerCamera = Camera.main;

        return playerCamera;
    }

    private void SpawnOnce()
    {
        SpawnGroup group = PickGroup();
        if (group == null || group.spawnEnemy == null) return;

        for (int i = 0; i < group.spawnEnemy.Length; i++)
        {
            GameObject prefab = group.spawnEnemy[i];
            if (prefab == null) continue;

            GameObject enemy = Instantiate(prefab, transform.position, transform.rotation);

            // 敵人也可能在自己的 Awake / OnEnable 裡就呼叫過 AddEnemy
            //（Instantiate 是同步的，那些回呼在這一行之前就跑完了）。
            // GameManager.AddEnemy 內部有 Contains 檢查，重複註冊是安全的。
            if (GameManager.Instance != null)
                GameManager.Instance.AddEnemy(enemy);

            _spawned.Add(enemy);   // 存活上限用；死亡時變成假 null，CountAlive 會清掉
        }
    }

    /// <summary>按 spawnWeight 加權抽一組。權重 &lt;= 0 的組會被跳過。</summary>
    private SpawnGroup PickGroup()
    {
        if (spawnGroups == null || spawnGroups.Length == 0) return null;

        float total = 0f;
        SpawnGroup lastValid = null;

        for (int i = 0; i < spawnGroups.Length; i++)
        {
            SpawnGroup g = spawnGroups[i];
            if (g == null || g.spawnWeight <= 0f) continue;

            total += g.spawnWeight;
            lastValid = g;
        }

        if (total <= 0f) return null;   // 沒有任何一組有正權重

        float roll = Random.value * total;

        for (int i = 0; i < spawnGroups.Length; i++)
        {
            SpawnGroup g = spawnGroups[i];
            if (g == null || g.spawnWeight <= 0f) continue;

            roll -= g.spawnWeight;
            if (roll <= 0f) return g;
        }

        // 浮點誤差讓 roll 剛好沒被扣完時的保底。回傳最後一個有效組，
        // 不要回傳 null —— 那會表現成「生成器偶爾莫名其妙跳過一次」。
        return lastValid;
    }
}