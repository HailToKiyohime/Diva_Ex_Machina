using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour, IPooled
{
    public GameObject attacker;

    public float physicalDamage = 0;
    public float explosionDamage = 0;
    public float energyDamage = 0;
    public float coldDamage = 0;

    public float criticalChance = 0.05f;
    public float criticalMultiplier = 1.5f;

    public float lifespan = 5f;
    public Rigidbody rb;

    public LayerMask ignoreLayer;
    public LayerMask enemyLayer;
    public bool ignoreObstacles = false;

    // ─────────────────────────── Homing ───────────────────────────
    [Header("Homing")]
    [Tooltip("轉向角速度。0 = 不追蹤，360 = 每秒可轉 360 度")]
    public float homingDegreePerSecond = 0f;

    [Tooltip("搜尋目標的半徑")]
    public float homingRange = 30f;

    [Range(0f, 180f)]
    [Tooltip("搜尋錐半角。以「子彈當前機頭方向」為軸，只鎖正前方的敵人")]
    public float homingSearchAngle = 40f;

    [Range(0f, 180f)]
    [Tooltip("脫鎖角。已鎖定的目標偏離當前機頭方向超過這個角度就放棄，改找新的")]
    public float homingLoseAngle = 90f;

    [Tooltip("整個生命週期內可累積的總轉向角度（額度）。轉多少扣多少，扣完就永久直線飛。\n負數 = 無限額度，0 = 完全不轉")]
    public float maxHomingAngle = 120f;

    [Tooltip("重新搜尋目標的最短間隔（秒），避免每個 physics step 都掃一次")]
    public float retargetInterval = 0.15f;

    [Tooltip("鎖定前是否要求視線通暢")]
    public bool homingRequireLineOfSight = false;

    [Tooltip("會擋住視線的層（只在 homingRequireLineOfSight 開啟時使用）")]
    public LayerMask homingObstacleLayer;

    [Tooltip("讓子彈模型朝向飛行方向")]
    public bool alignToVelocity = true;

    [Header("Homing / 外部指定目標（VLS）")]
    [Tooltip("發射後幾秒才開始轉向。垂直發射的爬升段用這個，避免一出管就折返")]
    public float homingStartDelay = 0f;

    [Tooltip("由發射器透過 SetHomingTarget() 指定的目標，是否無視搜尋錐、脫鎖角與搜尋半徑。\nVLS 飛彈請開啟：出管時機頭朝上、目標在側下方，不豁免的話會立刻脫鎖")]
    public bool lockAssignedTarget = true;
    // ──────────────────────────────────────────────────────────────

    // Your semantics:
    // 0  = destroy after 1st enemy impact
    // 1  = pass 1 enemy, destroy after 2nd enemy impact
    // 2  = pass 2 enemies, destroy after 3rd enemy impact
    // -1 = infinite
    //
    // ★ 注意：這個欄位是「就地遞減」的 —— 打穿一個目標就 --。
    //   所以它必須進 prefab 快照，回收重用時還原，否則穿透彈只有第一輪有穿透。
    public int penetration = 0;
    [SerializeField] private PlayerAnimation meleeImpactOwnerAnim;

    // _live：這顆子彈是否「在場上活著」。
    // 取代原本的 _destroyed —— 池化之後子彈不會真的被銷毀，只會在 live / 待命之間切換。
    private bool _live;
    private Collider _selfCol;

    // Prevent multi-collider enemies from taking damage multiple times per bullet
    private readonly HashSet<int> _hitEnemyIds = new HashSet<int>();

    // Homing runtime state
    private Transform _homingTarget;        // 目標本體（IDamageable 所在的 Transform）
    private Collider _homingTargetCol;      // 用來取瞄準點（bounds.center），比 pivot 準
    private Rigidbody _homingTargetRb;
    private Vector3 _homingTargetVel;
    private Vector3 _homingTargetLastPos;
    private bool _hasTargetLastPos;
    private bool _targetAssigned;           // true = 發射器指定的，false = 子彈自己找的
    private float _nextRetargetTime;
    private float _homingReadyTime;         // 爬升段結束時間

    private float _turnUsed;                // 已經用掉的累積轉向角度（度）
    private bool _homingExhausted;          // 額度用完 → 之後永遠直線

    // 壽命：原本是 Destroy(gameObject, lifespan)，那個排程綁在 GameObject 上、
    // 回收重用時取消不掉（重生後的子彈可能被上一世的計時器殺掉）。改成自己算。
    private float _despawnTime;

    // Predict 的待結算命中。原本用 coroutine + yield return null 延一幀，
    // 那等於每個 physics step、每顆子彈都配置一個 iterator。改成旗標。
    private Collider _pendingHit;
    private bool _hasPendingHit;

    // 視覺元件快取（回收時要清乾淨，否則會看到殘留的拖尾與粒子）
    private TrailRenderer[] _trails;
    private bool[] _trailEmitDefaults;
    private ParticleSystem[] _particles;

    // 共用暫存 buffer（同一 frame 內同步使用，不會互相干擾）
    private static readonly Collider[] _overlapBuffer = new Collider[64];

    /// <summary>剩餘轉向額度（度）。maxHomingAngle 為負代表無限。</summary>
    public float RemainingTurnAngle =>
        (maxHomingAngle < 0f) ? float.PositiveInfinity : Mathf.Max(0f, maxHomingAngle - _turnUsed);

    /// <summary>目前鎖定的目標（唯讀）。</summary>
    public Transform HomingTarget => _homingTarget;

    /// <summary>這顆子彈是否在場上飛行中。</summary>
    public bool IsLive => _live;

    // ═══════════════════ 外部指定目標 API ═══════════════════

    /// <summary>
    /// 由發射器在生成子彈後指定追蹤目標。lockAssignedTarget 開啟時，
    /// 這個目標不受搜尋錐 / 脫鎖角 / 搜尋半徑限制，直到目標消失為止。
    /// 目標消失後會自動退回「自己搜尋」模式。
    /// </summary>
    public void SetHomingTarget(Transform target)
    {
        if (target == null)
        {
            ClearHomingTarget();
            return;
        }

        _homingTarget = target;
        _homingTargetCol = target.GetComponentInChildren<Collider>();
        _homingTargetRb = target.GetComponentInParent<Rigidbody>();
        _homingTargetVel = (_homingTargetRb != null && !_homingTargetRb.isKinematic)
            ? _homingTargetRb.linearVelocity
            : Vector3.zero;
        _homingTargetLastPos = GetTargetAimPoint();
        _hasTargetLastPos = true;
        _targetAssigned = true;
    }

    /// <summary>方便發射器直接丟 IDamageable 進來。</summary>
    public void SetHomingTarget(IDamageable target)
    {
        var comp = target as Component;
        SetHomingTarget(comp != null ? comp.transform : null);
    }

    /// <summary>解除指定，退回自己搜尋。</summary>
    public void ClearHomingTarget() => ClearTarget();

    // ═══════════════ Prefab 預設值快照 ═══════════════
    //
    // 為什麼要整組快照，而不是只重設「看起來像 runtime state」的欄位：
    //
    // 兩個 spawn 點寫入的欄位是不重疊的 ——
    //   RangeAttackController：寫 damage + crit，不寫 enemyLayer / ignoreLayer
    //   TurretController     ：寫 damage + layer，不寫 crit
    // 用 Instantiate 時沒寫到的欄位一定是 prefab 值，所以沒事；
    // 一旦回收重用，沒寫到的欄位會留著上一手的值 ——
    // 玩家子彈被砲塔用過之後 enemyLayer 會變成玩家層，玩家就會被自己的子彈打到。
    //
    // Awake() 在 Instantiate 期間就跑完，早於任何外部寫入，
    // 所以這裡拍到的必然是 prefab 上的值。

    private struct Defaults
    {
        public float physicalDamage, explosionDamage, energyDamage, coldDamage;
        public float criticalChance, criticalMultiplier;
        public float lifespan;
        public LayerMask ignoreLayer, enemyLayer;
        public bool ignoreObstacles;

        public float homingDegreePerSecond, homingRange, homingSearchAngle, homingLoseAngle;
        public float maxHomingAngle, retargetInterval;
        public bool homingRequireLineOfSight;
        public LayerMask homingObstacleLayer;
        public bool alignToVelocity;
        public float homingStartDelay;
        public bool lockAssignedTarget;

        public int penetration;
        public CollisionDetectionMode collisionDetectionMode;
    }

    private Defaults _defaults;

    private void CaptureDefaults()
    {
        _defaults.physicalDamage = physicalDamage;
        _defaults.explosionDamage = explosionDamage;
        _defaults.energyDamage = energyDamage;
        _defaults.coldDamage = coldDamage;

        _defaults.criticalChance = criticalChance;
        _defaults.criticalMultiplier = criticalMultiplier;

        _defaults.lifespan = lifespan;
        _defaults.ignoreLayer = ignoreLayer;
        _defaults.enemyLayer = enemyLayer;
        _defaults.ignoreObstacles = ignoreObstacles;

        _defaults.homingDegreePerSecond = homingDegreePerSecond;
        _defaults.homingRange = homingRange;
        _defaults.homingSearchAngle = homingSearchAngle;
        _defaults.homingLoseAngle = homingLoseAngle;
        _defaults.maxHomingAngle = maxHomingAngle;
        _defaults.retargetInterval = retargetInterval;
        _defaults.homingRequireLineOfSight = homingRequireLineOfSight;
        _defaults.homingObstacleLayer = homingObstacleLayer;
        _defaults.alignToVelocity = alignToVelocity;
        _defaults.homingStartDelay = homingStartDelay;
        _defaults.lockAssignedTarget = lockAssignedTarget;

        _defaults.penetration = penetration;

        // Predict() 命中時會把 CCD 模式改成 ContinuousSpeculative，改了就回不去
        _defaults.collisionDetectionMode = (rb != null)
            ? rb.collisionDetectionMode
            : CollisionDetectionMode.Discrete;
    }

    private void RestoreDefaults()
    {
        attacker = null;   // prefab 上必然是 null；由 spawn 端每次重新指定

        physicalDamage = _defaults.physicalDamage;
        explosionDamage = _defaults.explosionDamage;
        energyDamage = _defaults.energyDamage;
        coldDamage = _defaults.coldDamage;

        criticalChance = _defaults.criticalChance;
        criticalMultiplier = _defaults.criticalMultiplier;

        lifespan = _defaults.lifespan;
        ignoreLayer = _defaults.ignoreLayer;
        enemyLayer = _defaults.enemyLayer;
        ignoreObstacles = _defaults.ignoreObstacles;

        homingDegreePerSecond = _defaults.homingDegreePerSecond;
        homingRange = _defaults.homingRange;
        homingSearchAngle = _defaults.homingSearchAngle;
        homingLoseAngle = _defaults.homingLoseAngle;
        maxHomingAngle = _defaults.maxHomingAngle;
        retargetInterval = _defaults.retargetInterval;
        homingRequireLineOfSight = _defaults.homingRequireLineOfSight;
        homingObstacleLayer = _defaults.homingObstacleLayer;
        alignToVelocity = _defaults.alignToVelocity;
        homingStartDelay = _defaults.homingStartDelay;
        lockAssignedTarget = _defaults.lockAssignedTarget;

        penetration = _defaults.penetration;

        if (rb != null)
        {
            rb.collisionDetectionMode = _defaults.collisionDetectionMode;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // ═══════════════════════ 生命週期 ═══════════════════════

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (_selfCol == null) _selfCol = GetComponent<Collider>();

        _trails = GetComponentsInChildren<TrailRenderer>(true);
        _trailEmitDefaults = new bool[_trails.Length];
        for (int i = 0; i < _trails.Length; i++)
            _trailEmitDefaults[i] = _trails[i].emitting;

        _particles = GetComponentsInChildren<ParticleSystem>(true);

        CaptureDefaults();
    }

    // Instantiate 時、以及日後物件池 SetActive(true) 時都會走這裡。
    private void OnEnable() => HandleSpawn();

    // 物件池在 SetActive(true) 之後可以再呼叫這個（OnEnable 已經做過就會被擋掉）。
    // 接上 PrefabPool 之後，把 IPooled 介面掛到這個類別上即可，簽章已經對上。
    public void OnSpawned() => HandleSpawn();

    private void HandleSpawn()
    {
        if (_live) return;   // 同一次上場只初始化一次，避免蓋掉 spawn 端剛寫入的欄位
        _live = true;

        // 1) 先把所有設定欄位還原成 prefab 值
        RestoreDefaults();

        // 2) 再清 runtime state
        _hitEnemyIds.Clear();          // ★ 不清的話，這顆子彈永遠打不到它前世打過的目標
        _pendingHit = null;
        _hasPendingHit = false;

        ClearTarget();
        _turnUsed = 0f;
        _homingExhausted = false;
        _nextRetargetTime = 0f;
        _homingReadyTime = Time.time + homingStartDelay;

        _despawnTime = Time.time + Mathf.Max(0f, lifespan);

        // Physics.IgnoreCollision 的配對狀態會在 collider disable/enable 時被清掉。
        // 這裡明確地 enable 一次，確保上一世 IgnoreCollision(self, other, true) 的殘留
        // 不會讓這顆子彈穿過某個敵人。
        if (_selfCol != null) _selfCol.enabled = true;

        ResetVisuals();
    }

    /// <summary>
    /// 這顆子彈退場。目前直接 Destroy；接上物件池後只要換掉最後那一行。
    /// </summary>
    public void Despawn()
    {
        if (!_live) return;

        OnDespawned();

        PrefabPool.Despawn(gameObject);
    }

    // 冪等：物件池直接 SetActive(false) 時 OnDisable 也會呼叫到，重複呼叫無害。
    public void OnDespawned()
    {
        _live = false;

        _pendingHit = null;
        _hasPendingHit = false;
        ClearTarget();

        if (_selfCol != null) _selfCol.enabled = false;

        StopVisuals();
    }

    private void OnDisable() => OnDespawned();

    private void ResetVisuals()
    {
        // 順序很重要：transform 已經由 Instantiate / 物件池擺到位了，這時才 Clear，
        // 否則會看到一條從「上次死亡的位置」拉到槍口的拖尾。
        if (_trails != null)
        {
            for (int i = 0; i < _trails.Length; i++)
            {
                if (_trails[i] == null) continue;
                _trails[i].Clear();
                _trails[i].emitting = _trailEmitDefaults[i];
            }
        }

        if (_particles != null)
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                if (_particles[i] == null) continue;
                _particles[i].Clear(true);
                if (_particles[i].main.playOnAwake) _particles[i].Play(true);
            }
        }
    }

    private void StopVisuals()
    {
        if (_trails != null)
        {
            for (int i = 0; i < _trails.Length; i++)
            {
                if (_trails[i] == null) continue;
                _trails[i].emitting = false;
                _trails[i].Clear();
            }
        }

        if (_particles != null)
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                if (_particles[i] == null) continue;
                _particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    protected virtual void FixedUpdate()
    {
        if (!_live) return;

        // 1) 上一個 physics step 由 Predict() 預測到的命中，延一步後才結算。
        //    對應原本 Predict() 裡的 yield return null。
        if (_hasPendingHit)
        {
            _hasPendingHit = false;
            Collider pending = _pendingHit;
            _pendingHit = null;

            // 目標可能在這一步之間被銷毀了；原本的 coroutine 版在這裡會丟例外
            if (pending != null)
            {
                OnTriggerEnterFixed(pending);
                if (!_live) return;
            }
        }

        // 2) 壽命
        if (Time.time >= _despawnTime)
        {
            Despawn();
            return;
        }

        // 3) 追蹤
        UpdateHoming(Time.fixedDeltaTime);
        if (!_live) return;

        // 4) 下一步的碰撞預測
        Predict();
    }

    private void OnTriggerEnter(Collider collider)
    {
        OnTriggerEnterFixed(collider);
    }

    private bool IsInEnemyLayer(Collider col)
    {
        if (col == null) return false;
        int bit = 1 << col.gameObject.layer;
        return (enemyLayer.value & bit) != 0;
    }

    // ═══════════════ 命中過濾：單一真相來源 ═══════════════
    //
    // 舊版有兩套獨立的過濾，必須手動保持一致：
    //
    //   Predict()             → GetPredictMask() & ~ignoreLayer.value，餵給 Physics.Linecast
    //   OnTriggerEnterFixed() → IsInIgnoreLayer / ignoreObstacles 兩個早期 return
    //
    // 而且它們的底層機制根本不同：
    //   Physics.Linecast 是 query，只吃 layerMask，完全不看 Layer Collision Matrix。
    //   OnTriggerEnter   則是由矩陣決定會不會被呼叫。
    //
    // 結果是「矩陣裡關掉的組合，Predict 照樣打得到」—— 子彈會在預測階段被一個
    // OnTriggerEnter 永遠不會回報的東西殺掉，表現成子彈莫名其妙消失。
    //
    // 現在兩邊都從 BuildReactMask() 導出，不可能再不同步。

    /// <summary>
    /// 這顆子彈「應該有反應」的層遮罩。
    ///
    /// Predict 直接拿它當 Linecast 的 layerMask，
    /// OnTriggerEnterFixed 透過 ShouldReactTo 拿它做早期 return。
    ///
    /// 注意這回答的是「要不要理會這個碰撞體」，不是「這是不是可以傷害的目標」。
    /// 後者仍然由 IsInEnemyLayer + IDamageable 判斷。
    /// </summary>
    protected int BuildReactMask()
    {
        int notBullet = ~LayerMask.GetMask("Bullet");

        // ignoreObstacles = true → 只對 enemyLayer 有反應，其餘一律穿過
        int mask = ignoreObstacles ? enemyLayer.value : notBullet;

        return mask & notBullet & ~ignoreLayer.value;
    }

    /// <summary>單一 collider 版本，語意跟 BuildReactMask 完全一致。</summary>
    protected bool ShouldReactTo(Collider col)
    {
        if (col == null) return false;
        return (BuildReactMask() & (1 << col.gameObject.layer)) != 0;
    }

    /// <summary>
    /// 往前掃一個 physics step 的距離，補上高速子彈的漏判。
    ///
    /// 原本是 coroutine，而且每個 FixedUpdate 都 StartCoroutine 一次 ——
    /// 大部分情況下它根本不會 yield（沒命中就直接結束），等於白白配置一個
    /// iterator + Coroutine 物件。100 顆子彈 × 50 steps/s 就是每秒五千次配置。
    /// </summary>
    protected virtual void Predict()
    {
        if (rb == null) return;
        if (_hasPendingHit) return;   // 已經有待結算的命中，不要再往前掃、也不要重複貼位置

        Vector3 from = transform.position;
        Vector3 to = from + rb.linearVelocity * Time.fixedDeltaTime;

        int layerMask = BuildReactMask();
        if (Physics.Linecast(from, to, out RaycastHit hit, layerMask))
        {
            transform.position = hit.point;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            _pendingHit = hit.collider;
            _hasPendingHit = true;
        }
    }

    // ═══════════════════════════ Homing ═══════════════════════════

    private void UpdateHoming(float dt)
    {
        if (!_live || _homingExhausted) return;
        if (homingDegreePerSecond <= 0f) return;
        if (rb == null) return;

        // 爬升段：完全不轉向，也不消耗額度
        if (Time.time < _homingReadyTime) return;

        // 額度用完 → 收攤，之後每幀直接跳出
        if (RemainingTurnAngle <= 0f)
        {
            _homingExhausted = true;
            ClearTarget();
            return;
        }

        Vector3 vel = rb.linearVelocity;
        float speed = vel.magnitude;
        if (speed < 0.0001f) return;

        Vector3 currentDir = vel / speed;   // 子彈當前機頭方向，所有搜尋與判定都以這個為準

        // 目標失效 → 找新的；找不到就什麼都不做，維持原速度直線飛
        if (!IsTargetValid(currentDir))
        {
            ClearTarget();
            if (Time.time >= _nextRetargetTime)
            {
                _nextRetargetTime = Time.time + retargetInterval;
                AcquireTarget(currentDir);
            }
        }
        if (_homingTarget == null) return;

        UpdateTargetVelocity(dt);

        Vector3 desiredDir = SolveHomingDirection(GetTargetAimPoint(), speed);
        if (desiredDir.sqrMagnitude < 0.0001f) return;

        // 這一步能轉幾度 = min(角速度上限, 剩餘額度)
        float stepDeg = Mathf.Min(homingDegreePerSecond * dt, RemainingTurnAngle);
        if (stepDeg <= 0f) return;

        Vector3 newDir = Vector3.RotateTowards(currentDir, desiredDir, stepDeg * Mathf.Deg2Rad, 0f);
        if (newDir.sqrMagnitude < 0.0001f) return;
        newDir.Normalize();

        // 實際轉了多少就扣多少（已經對準時每幀只會轉一點點，額度消耗自然變慢）
        _turnUsed += Vector3.Angle(currentDir, newDir);

        rb.linearVelocity = newDir * speed;   // 保留速率，只改方向
        if (alignToVelocity)
            transform.rotation = Quaternion.LookRotation(newDir, Vector3.up);
    }

    /// <summary>
    /// 用 MathToolKit 的攔截解算求方向。解不出來（或解不合法）就退回直接瞄準。
    /// </summary>
    private Vector3 SolveHomingDirection(Vector3 aimPoint, float bulletSpeed)
    {
        Vector3 origin = transform.position;
        Vector3 fallback = aimPoint - origin;

        // MathToolKit 參數：a = 目標位置, b = 攔截者位置, vA = 目標速度, sB = 攔截者速率
        if (MathToolKit.InterceptionPoint(aimPoint, origin, _homingTargetVel, bulletSpeed, out Vector3 interceptPoint))
        {
            Vector3 toIntercept = interceptPoint - origin;

            // 目標速率 ≈ 子彈速率時 quadratic 的 a 會趨近 0（Infinity / NaN），
            // 而且兩根同為負時 Mathf.Max 會取到負的解 → 這裡一律過濾掉。
            float sanityRange = Mathf.Max(homingRange, fallback.magnitude) * 4f;
            bool valid =
                !float.IsNaN(toIntercept.x) && !float.IsInfinity(toIntercept.x) &&
                !float.IsNaN(toIntercept.y) && !float.IsInfinity(toIntercept.y) &&
                !float.IsNaN(toIntercept.z) && !float.IsInfinity(toIntercept.z) &&
                toIntercept.sqrMagnitude > 0.0001f &&
                toIntercept.sqrMagnitude < sanityRange * sanityRange &&
                Vector3.Dot(toIntercept, fallback) > 0f;   // 攔截點不能在目標的反方向

            if (valid) return toIntercept.normalized;
        }

        return (fallback.sqrMagnitude > 0.0001f) ? fallback.normalized : Vector3.zero;
    }

    private bool IsTargetValid(Vector3 currentDir)
    {
        if (_homingTarget == null) return false;
        if (!_homingTarget.gameObject.activeInHierarchy) return false;

        // 發射器指定的目標：豁免搜尋錐 / 脫鎖角 / 半徑，咬到目標消失為止
        if (_targetAssigned && lockAssignedTarget) return true;

        Vector3 to = GetTargetAimPoint() - transform.position;

        // 超出搜尋半徑 → 放棄
        if (to.sqrMagnitude > homingRange * homingRange) return false;

        // 已經跑到子彈的側後方 → 放棄，改找前方新的目標
        if (Vector3.Angle(currentDir, to) > homingLoseAngle) return false;

        return true;
    }

    private void ClearTarget()
    {
        _homingTarget = null;
        _homingTargetCol = null;
        _homingTargetRb = null;
        _homingTargetVel = Vector3.zero;
        _hasTargetLastPos = false;
        _targetAssigned = false;   // 指定的目標沒了 → 退回自己搜尋
    }

    private Vector3 GetTargetAimPoint()
    {
        if (_homingTargetCol != null && _homingTargetCol.enabled)
            return _homingTargetCol.bounds.center;   // 打身體中心，不是腳底 pivot
        return (_homingTarget != null) ? _homingTarget.position : transform.position;
    }

    /// <summary>
    /// 估目標速度。非 kinematic 的 Rigidbody 直接讀，
    /// 其餘（CharacterController / NavMeshAgent / kinematic）用位置差分。
    /// </summary>
    private void UpdateTargetVelocity(float dt)
    {
        Vector3 pos = GetTargetAimPoint();

        if (_homingTargetRb != null && !_homingTargetRb.isKinematic)
        {
            _homingTargetVel = _homingTargetRb.linearVelocity;
        }
        else if (_hasTargetLastPos && dt > 0f)
        {
            Vector3 sampled = (pos - _homingTargetLastPos) / dt;
            _homingTargetVel = Vector3.Lerp(_homingTargetVel, sampled, 0.5f);  // 抹掉抖動
        }

        _homingTargetLastPos = pos;
        _hasTargetLastPos = true;
    }

    /// <summary>
    /// 在「子彈當前機頭方向」的前方錐形內找最近的可傷害目標。
    /// 跟發射位置、初始方向完全無關。
    /// </summary>
    private void AcquireTarget(Vector3 currentDir)
    {
        int mask = enemyLayer.value & ~ignoreLayer.value;
        if (mask == 0) return;

        int count = Physics.OverlapSphereNonAlloc(
            transform.position, homingRange, _overlapBuffer, mask, QueryTriggerInteraction.Ignore);

        Transform bestTf = null;
        Collider bestCol = null;
        Rigidbody bestRb = null;
        float bestSqr = float.MaxValue;

        float rangeSqr = homingRange * homingRange;

        for (int i = 0; i < count; i++)
        {
            Collider col = _overlapBuffer[i];
            if (col == null) continue;

            var damageable = col.GetComponentInParent<IDamageable>();
            if (damageable == null) continue;

            var comp = damageable as Component;
            if (comp == null) continue;

            // 穿透彈不要回頭鎖已經打過的目標
            if (_hitEnemyIds.Contains(comp.GetInstanceID())) continue;

            // 不要鎖自己人（開槍者自己）
            if (attacker != null && comp.transform.IsChildOf(attacker.transform)) continue;

            Vector3 aim = col.bounds.center;
            Vector3 to = aim - transform.position;
            float sqr = to.sqrMagnitude;
            if (sqr < 0.0001f || sqr > rangeSqr) continue;
            if (sqr >= bestSqr) continue;

            // 唯一的方向條件：在機頭前方的搜尋錐內
            if (Vector3.Angle(currentDir, to) > homingSearchAngle) continue;

            if (homingRequireLineOfSight && IsLineOfSightBlocked(aim, comp.transform)) continue;

            bestSqr = sqr;
            bestTf = comp.transform;
            bestCol = col;
            bestRb = comp.GetComponentInParent<Rigidbody>();
        }

        if (bestTf == null) return;

        _homingTarget = bestTf;
        _homingTargetCol = bestCol;
        _homingTargetRb = bestRb;
        _homingTargetVel = (bestRb != null && !bestRb.isKinematic) ? bestRb.linearVelocity : Vector3.zero;
        _homingTargetLastPos = bestCol.bounds.center;
        _hasTargetLastPos = true;
        _targetAssigned = false;
    }

    private bool IsLineOfSightBlocked(Vector3 aim, Transform targetRoot)
    {
        if (Physics.Linecast(transform.position, aim, out RaycastHit hit,
                             homingObstacleLayer, QueryTriggerInteraction.Ignore))
        {
            // 打到目標自己不算被擋住
            return !hit.transform.IsChildOf(targetRoot);
        }
        return false;
    }

    // ══════════════════════════════════════════════════════════════

    protected virtual void OnTriggerEnterFixed(Collider other)
    {
        Debug.Log($"Bullet hit: {other.name} / layer={LayerMask.LayerToName(other.gameObject.layer)}");
        if (!_live) return;
        if (other == null) return;

        // ★ 共用過濾：跟 Predict 的 Linecast layerMask 是同一個來源。
        //   舊版這裡是兩個獨立的早期 return（IsInIgnoreLayer + ignoreObstacles），
        //   跟 Predict 的遮罩各算各的。
        if (!ShouldReactTo(other))
            return;

        // 可被傷害的目標？（不再綁死 EnemyStats，改認 IDamageable 介面）
        var target = other.GetComponentInParent<IDamageable>();
        // enemyLayer 現在代表「這顆子彈允許打到的層」：
        // 玩家開的子彈設成敵人層，敵人開的子彈設成玩家層，以此分敵我、避免友軍誤傷。
        bool isTarget = (target != null) && IsInEnemyLayer(other);

        if (isTarget)
        {
            // 用被打物件的 instance id 去重（避免同一目標多 collider 重複觸發）
            var targetObj = target as Component;
            int id = (targetObj != null) ? targetObj.GetInstanceID() : other.GetInstanceID();
            if (_hitEnemyIds.Contains(id)) return;
            _hitEnemyIds.Add(id);

            // 打中的就是現在鎖定的目標 → 放掉鎖定，讓穿透彈去找下一個
            if (_homingTarget != null && targetObj != null && _homingTarget == targetObj.transform)
                ClearTarget();

            // 暴擊在「攻擊方」這邊結算（暴擊是攻擊者的屬性）
            float critMul = 1f;
            if (Random.value < Mathf.Clamp01(criticalChance))
                critMul = Mathf.Max(1f, criticalMultiplier);

            // 只交出「原始四種傷害」，防禦由被打的目標自己套用
            DamageInfo dmg = new DamageInfo(
                physicalDamage * critMul,
                explosionDamage * critMul,
                energyDamage * critMul,
                coldDamage * critMul
            );

            target.TakeDamage(dmg, attacker);

            // 避免穿過同一 collider 時重複觸發
            // （這個配對狀態會在子彈退場時由 _selfCol.enabled = false 清掉）
            if (_selfCol != null && other != null)
                Physics.IgnoreCollision(_selfCol, other, true);

            // 接著處理穿透（語意與原本相同）
            if (penetration == -1)
            {
                return; // 無限穿透
            }
            else if (penetration > 0)
            {
                penetration--; // 消耗一次穿透
                return;        // 繼續飛
            }
            else // penetration == 0
            {
                Despawn(); // 命中後退場
                return;
            }
        }

        // Not enemy:
        // - if ignoreObstacles was true, we already returned above
        // - otherwise hit obstacle => despawn
        Despawn();
    }

    // IsInIgnoreLayer 已移除 —— 它是舊的第二套過濾，唯一的呼叫點已改用
    // ShouldReactTo。留著它只會讓人日後不小心又寫出跟 Predict 不同步的判斷。

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (homingDegreePerSecond <= 0f) return;

        Vector3 pos = transform.position;
        Vector3 dir = (rb != null && rb.linearVelocity.sqrMagnitude > 0.0001f)
            ? rb.linearVelocity.normalized
            : transform.forward;

        Gizmos.color = new Color(1f, 0.6f, 0f, 0.5f);
        Gizmos.DrawWireSphere(pos, homingRange);

        // 以機頭方向為軸的搜尋錐
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(pos, dir * homingRange);
        Vector3 up = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
        Vector3 right = Vector3.Cross(up, dir).normalized;
        Gizmos.DrawRay(pos, Quaternion.AngleAxis(homingSearchAngle, up) * dir * homingRange);
        Gizmos.DrawRay(pos, Quaternion.AngleAxis(-homingSearchAngle, up) * dir * homingRange);
        Gizmos.DrawRay(pos, Quaternion.AngleAxis(homingSearchAngle, right) * dir * homingRange);
        Gizmos.DrawRay(pos, Quaternion.AngleAxis(-homingSearchAngle, right) * dir * homingRange);

        if (_homingTarget != null)
        {
            Gizmos.color = _targetAssigned ? Color.magenta : Color.red;
            Gizmos.DrawLine(pos, GetTargetAimPoint());
        }
    }
#endif
}