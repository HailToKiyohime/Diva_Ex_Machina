using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum EntityState
{
    Idle,
    Patrolling,
    Retreat,
    Chasing,
    Combat,
}

[System.Serializable]
public enum TargetType
{
    Player,
    Enemy,
    Building,
    Core,
    Obstacle,
}

[System.Serializable]
public class Target
{
    public Transform targetTransform;
    public TargetType type;
    public float targetPriority;
    public float priorityDecreaseMultiplier;
    public Target(Transform targetTransform, float targetPriority, float priorityDecreaseMultiplier)
    {
        this.targetTransform = targetTransform;
        this.targetPriority = targetPriority;
        this.priorityDecreaseMultiplier = priorityDecreaseMultiplier;
    }
}

[System.Serializable]
public class TargerPreference
{
    public TargetType targetType;
    public float priorityMultiplier;
    public float priorityDecreaseMultiplier;
}

public class ModularEntityBrain : MonoBehaviour
{
    public ModularEntityMovement modularEntityMovement;

    [Header("Turrets")]
    [SerializeField] protected TurretController[] turrets;   // 0..N 座，Inspector 拖或自動抓

    protected PathFinder pathFinder;
    protected ShipPassenger selfPassenger;   // 自己的船上狀態（掛在同一個 GameObject 上）

    public EntityState currentState;
    public List<TargerPreference> targetPreferences = new List<TargerPreference>();
    public List<Target> targets = new List<Target>();

    protected Vector3[] path;
    public Vector3 nextWaypoint;
    public Vector3 destination;
    public Vector3 spawnLocation;
    [Header("Idle Setting")]
    [MinMaxSlider(0f, 15f)]
    public Vector2 idleDestinationUpdateRange = new Vector2(1f, 3f);
    public float idleActivityAreaRadius = 10f;
    [Header("Patrol Setting")]
    [MinMaxSlider(0f, 15f)]
    public Vector2 patrolDestinationUpdateRange = new Vector2(1f, 3f);
    public float patrolActivityAreaRadius = 20f;
    [Header("Retreat Setting")]
    [MinMaxSlider(0f, 15f)]
    public Vector2 retreatDestinationUpdateRange = new Vector2(1f, 3f);
    [Header("Chase Setting")]
    [MinMaxSlider(0f, 15f)]
    public Vector2 chaseDestinationUpdateRange = new Vector2(1f, 3f);
    [Header("Combat Setting")]
    [MinMaxSlider(0f, 15f)]
    public Vector2 combatDestinationUpdateRange = new Vector2(1f, 3f);
    public float combatActivityAreaRadius = 20f;
    public float reactionCoolDown = 3f;
    public float reactionChance = 0.5f;

    [Header("Docking Proximity Update")]
    [Tooltip("離最近的停靠點小於這個水平距離時，改用下面的更新間隔。所有狀態都適用。")]
    public float dockUpdateDistance = 30f;

    [Tooltip("靠近停靠點時的路徑更新間隔（秒）。不要設 0 —— NavMesh.CalculatePath 是主執行緒同步呼叫，" +
             "每個 physics step 算一次，敵人一多就會卡。0.1~0.3 通常夠了。")]
    [MinMaxSlider(0f, 15f)]
    public Vector2 dockDestinationUpdateRange = new Vector2(0.1f, 0.3f);

    protected float destinationTimer;

    private readonly Dictionary<TargetType, float> decayMultiplierByType = new Dictionary<TargetType, float>();

    [SerializeField] protected float waypointArriveRadius = 5f;
    [SerializeField] protected float slowDownRadius = 6f;

    [Tooltip("重新定位航點時，最多往前搜尋幾個線段。太大會在 U 型路徑上抄近路穿牆，太小則在路徑折返時反應遲鈍。2 通常剛好。")]
    [SerializeField] protected int waypointSearchLookAhead = 2;

    protected int currentWaypointIndex = 0;

    /// <summary>
    /// FindNextWaypoint 這一幀實際使用的路徑長度。
    ///
    /// ★ 各 Behaviour 原本用 path.Length 判斷「是不是最後一個航點」，但走的是
    ///   pathFinder.GetCurrentWorldPath() 回傳的即時投影路徑。兩者理論上等長，
    ///   但那是兩個不同的陣列，只要哪天長度對不上，抵達判定就會失效
    ///   （永遠不等於 length-1 → destinationTimer 不歸零 → 卡在原地等 timer）。
    ///   統一由這裡提供長度，兩邊必定一致。
    /// </summary>
    protected int livePathLength = 0;

    [SerializeField] protected float facingDeadzone = 1f;   // 角度誤差小於此就不轉，避免抖動

    protected virtual void Start()
    {
        spawnLocation = transform.position;
        pathFinder = gameObject.GetComponent<PathFinder>();
        selfPassenger = gameObject.GetComponent<ShipPassenger>();
        currentState = EntityState.Idle;
        RebuildPreferenceCache();

        if (turrets == null || turrets.Length == 0)
            turrets = GetComponentsInChildren<TurretController>();
    }

    public virtual void FixedUpdate()
    {
        PriorityUpdate();
        PathUpdate();
        StateBehaviour();
    }

    protected void PriorityUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // ★ 第一步：無條件剔除已被銷毀的目標。
        //
        //   這一段刻意放在 targets.Count > 1 的閘門「外面」。
        //   優先度衰減只在多目標時進行是刻意的設計（最後一個目標不衰減 =
        //   敵人不會忘記自己正在戰鬥中），但那個設計有一個前提：卡住的那個目標
        //   必須是活著的。
        //
        //   目標被 Destroy 之後 targetTransform 會變成 Unity 的假 null，
        //   優先度又最高 → FindTarget() 每幀都回傳它 → 各狀態判定「目標消失」
        //   → 切回 Patrolling。實體從此變成植物人。
        //
        //   在這裡剔除，就不需要任何跨物件的死亡通知機制 ——
        //   目標死掉後最多一個 physics step 就會被清掉。
        PruneDestroyedTargets();

        if (targets.Count > 1)
        {
            for (int x = targets.Count - 1; x >= 0; x--)
            {
                Target t = targets[x];

                float typeMultiplier = 1f;
                if (decayMultiplierByType.TryGetValue(t.type, out float found))
                    typeMultiplier = found;

                t.targetPriority -= t.priorityDecreaseMultiplier * typeMultiplier * dt;

                if (t.targetPriority <= 0f)
                    targets.RemoveAt(x);
            }
        }
    }

    /// <summary>剔除 targetTransform 已被銷毀的項目。倒著走，RemoveAt 才不會跳過元素。</summary>
    protected void PruneDestroyedTargets()
    {
        for (int x = targets.Count - 1; x >= 0; x--)
        {
            Target t = targets[x];
            if (t == null || t.targetTransform == null)
                targets.RemoveAt(x);
        }
    }
    protected virtual void RebuildPreferenceCache()
    {
        decayMultiplierByType.Clear();
        for (int i = 0; i < targetPreferences.Count; i++)
        {
            decayMultiplierByType[targetPreferences[i].targetType] = targetPreferences[i].priorityDecreaseMultiplier;
        }
    }

    protected virtual void StateBehaviour()
    {
        switch (currentState)
        {
            case EntityState.Idle:
                IdleBehaviour();
                break;
            case EntityState.Patrolling:
                PatrollingBehaviour();
                break;
            case EntityState.Retreat:
                RetreatBehaviour();
                break;
            case EntityState.Chasing:
                ChasingBehaviour();
                break;
            case EntityState.Combat:
                CombatBehaviour();
                break;
        }
    }

    // ★ 算路徑前，把「自己」與「目標」的船上狀態餵給 PathFinder。
    //   target == null 代表目的地是自己算出來的點（Idle/Patrol 隨機點、Retreat 的 spawnLocation），
    //   那種點跟自己在同一個空間，所以 isTargetOnShip 跟著 isOnShip 走。
    protected virtual void SyncShipFlags(Transform target)
    {
        pathFinder.isOnShip = selfPassenger != null && selfPassenger.isOnShip;

        if (target != null)
        {
            // 目標身上的 ShipPassenger；沒掛就當作不在船上（例如靜態建築、Core）
            ShipPassenger tp = target.GetComponent<ShipPassenger>();
            pathFinder.isTargetOnShip = tp != null && tp.isOnShip;
            pathFinder.targetLocation = target.position;   // GetTargetClosestDockingLocation 需要它
        }
        else
        {
            pathFinder.isTargetOnShip = pathFinder.isOnShip;
            pathFinder.targetLocation = destination;
        }
    }

    protected virtual void PathUpdate()
    {
        destinationTimer -= Time.fixedDeltaTime;
        if (destinationTimer > 0f) return;

        Vector2 r;
        switch (currentState)
        {
            case EntityState.Idle:
                destinationTimer = RollInterval(idleDestinationUpdateRange);
                r = Random.insideUnitCircle * idleActivityAreaRadius;
                destination = spawnLocation + new Vector3(r.x, 0f, r.y);
                SyncShipFlags(null);
                SetPath(pathFinder.FindPath(destination));
                break;
            case EntityState.Patrolling:
                destinationTimer = RollInterval(patrolDestinationUpdateRange);
                r = Random.insideUnitCircle * patrolActivityAreaRadius;
                destination = spawnLocation + new Vector3(r.x, 0f, r.y);
                SyncShipFlags(null);
                SetPath(pathFinder.FindPath(destination));
                break;
            case EntityState.Retreat:
                destinationTimer = RollInterval(retreatDestinationUpdateRange);
                destination = spawnLocation;
                SyncShipFlags(null);
                SetPath(pathFinder.FindPath(destination));
                break;
            case EntityState.Chasing:
                {
                    destinationTimer = RollInterval(chaseDestinationUpdateRange);
                    Transform chaseTarget = FindTarget();
                    if (chaseTarget == null) { ChangeState(EntityState.Patrolling); break; }   // 沒目標 → 回巡邏
                    destination = chaseTarget.position;
                    SyncShipFlags(chaseTarget);
                    SetPath(pathFinder.FindPath(destination));
                    break;
                }
            case EntityState.Combat:
                {
                    destinationTimer = RollInterval(combatDestinationUpdateRange);
                    Transform combatTarget = FindTarget();
                    if (combatTarget == null) { ChangeState(EntityState.Patrolling); break; }   // 沒目標 → 回巡邏
                    r = Random.insideUnitCircle * combatActivityAreaRadius;
                    destination = combatTarget.position + new Vector3(r.x, 0f, r.y);
                    SyncShipFlags(combatTarget);
                    SetPath(pathFinder.FindPath(destination));
                    break;
                }
        }
    }

    public Transform FindTarget()
    {
        // 原本直接拿 targets[0] 當起始最佳解，不檢查有效性 ——
        // 只要它是假 null，整個迴圈比出來的贏家就可能是一個不存在的東西。
        Target bestTarget = null;

        for (int i = 0; i < targets.Count; i++)
        {
            Target t = targets[i];
            if (!IsTargetUsable(t)) continue;

            if (bestTarget == null || t.targetPriority > bestTarget.targetPriority)
                bestTarget = t;
        }

        return (bestTarget != null) ? bestTarget.targetTransform : null;
    }

    /// <summary>
    /// 這個目標現在能不能當作交戰對象。
    ///
    /// 已銷毀 → 不能用（而且 PruneDestroyedTargets 會在下一個 physics step 清掉它）。
    /// 停用中 → 不能用，但「保留在清單裡」——
    ///   物件被 SetActive(false) 通常是暫時的（物件池、隱藏、過場），
    ///   直接移除會讓敵人在目標回來時失憶。銷毀才是永久的。
    /// </summary>
    protected virtual bool IsTargetUsable(Target t)
    {
        if (t == null) return false;
        if (t.targetTransform == null) return false;
        if (!t.targetTransform.gameObject.activeInHierarchy) return false;
        return true;
    }
    protected void SetPath(Vector3[] newPath)
    {
        path = newPath;
        livePathLength = (newPath != null) ? newPath.Length : 0;

        // ★ 這裡就是「敵人重算路徑後會回頭走第一個點」的根源。
        //
        //   舊版：currentWaypointIndex = 0;
        //
        //   問題在於 corner[0] 並不是實體的位置，而是
        //   NavMesh.SamplePosition(transform.position) 吸附之後的起點。
        //   實體是 Rigidbody 驅動的（不是 NavMeshAgent），隨時可能：
        //     · 走出 NavMesh 邊緣
        //     · 站在甲板 / 箱子 / 任何沒烘焙的東西上
        //     · 被撞飛而暫時離地
        //     · ghost ↔ real 投影有一點誤差
        //   這時 sample 會往「最近的合法位置」吸附 —— 而最近的合法位置，
        //   通常就在它剛剛走過來的那個方向。navSampleMaxDistance 是 100，
        //   最遠可以吸到 100 公尺外。
        //
        //   歸零之後，唯一能跳過這個過期 corner 的機制是 FindNextWaypoint 的
        //   「距離小於 waypointArriveRadius(5) 就 ++」。corner[0] 在身後 30 公尺
        //   時這條規則不會成立 → 實體轉頭走回去 → 走進 5 公尺內才前進
        //   → 1~3 秒後 timer 到期又重算又歸零 → 不斷回頭。
        //
        //   路徑短時 corner 間距本來就跟 5 公尺同量級，回頭幅度小看不出來；
        //   路徑長時 corner 拉開，一次回頭就是一個幾十公尺的大轉彎。
        //
        //   改成用投影定位：找出實體現在落在折線的哪一段，直接瞄準那一段的終點。
        //   corner[0] 在身後時，實體位於 [corner0, corner1] 這一段上
        //   → 瞄準 corner1 → 結構上不可能再走回頭路。
        currentWaypointIndex = ResolveWaypointIndex(newPath, 0);
    }

    /// <summary>
    /// 把實體投影到路徑折線上，回傳「應該瞄準的 corner index」。
    ///
    /// 做法：在 [fromSegment, fromSegment + waypointSearchLookAhead] 這個窗口內
    /// 找出離實體最近的線段，瞄準該線段的終點；接著再套用抵達半徑推進。
    ///
    /// 為什麼要限制搜尋窗口而不是掃整條路徑：
    /// U 型繞路（例如繞過一面牆）時，歐氏距離上最近的線段可能在牆的另一側。
    /// 直接跳過去等於叫實體穿牆。限制成只能往前看幾段，就不會抄到那種捷徑。
    /// </summary>
    protected int ResolveWaypointIndex(Vector3[] p, int fromSegment)
    {
        if (p == null || p.Length == 0) return 0;
        if (p.Length == 1) return 0;   // 保底路徑（只有目的地一個點）

        Vector3 pos = transform.position;

        int lastSegment = p.Length - 2;                                   // 最後一段的起點 index
        int start = Mathf.Clamp(fromSegment, 0, lastSegment);
        int end = Mathf.Min(lastSegment, start + Mathf.Max(0, waypointSearchLookAhead));

        int bestSegment = start;
        float bestSqr = float.PositiveInfinity;

        for (int i = start; i <= end; i++)
        {
            float sqr = SqrDistanceToSegmentXZ(pos, p[i], p[i + 1]);
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                bestSegment = i;
            }
        }

        int index = bestSegment + 1;   // 瞄準所在線段的「終點」，不是起點

        // 已經站在該 corner 的抵達半徑內 → 繼續往前推進（原本的行為，保留）
        while (index < p.Length - 1 && DistanceXZ(pos, p[index]) < waypointArriveRadius)
            index++;

        return index;
    }

    /// <summary>點到線段的平方距離（只看水平面，Y 忽略）。開平方很貴，比大小不需要開。</summary>
    protected static float SqrDistanceToSegmentXZ(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector2 p = new Vector2(point.x, point.z);
        Vector2 s = new Vector2(a.x, a.z);
        Vector2 e = new Vector2(b.x, b.z);

        Vector2 se = e - s;
        float lenSqr = se.sqrMagnitude;
        if (lenSqr < 0.000001f) return (p - s).sqrMagnitude;   // 重複 corner

        float t = Mathf.Clamp01(Vector2.Dot(p - s, se) / lenSqr);
        return (p - (s + se * t)).sqrMagnitude;
    }

    protected static float DistanceXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    /// <summary>
    /// 現在瞄準的是不是最後一個航點，而且已經到了。
    /// 用 livePathLength 而不是 path.Length —— 跟 FindNextWaypoint 走的是同一條路徑。
    /// </summary>
    protected bool IsAtFinalWaypoint(Vector3 waypoint)
    {
        return IsAtFinalWaypoint(DistanceToPoint(waypoint));
    }

    /// <summary>
    /// 同上，但由呼叫端自己決定「距離」怎麼量。
    ///
    /// ★ 飛行單位（FalconBrain）一定要用這個多載。
    ///   隼懸停在地面航點上方 10 公尺，3D 距離永遠 ≥ 10，
    ///   而 waypointArriveRadius 預設是 5 —— 吃 Vector3 的版本對隼永遠回傳 false，
    ///   抵達判定完全失效（destinationTimer 不會歸零、Retreat 不會進 Combat）。
    ///   隼要傳入把 Y 歸零之後的水平距離。
    /// </summary>
    protected bool IsAtFinalWaypoint(float distanceToWaypoint)
    {
        if (livePathLength <= 0) return false;
        return currentWaypointIndex >= livePathLength - 1
            && distanceToWaypoint < waypointArriveRadius;
    }

    [Header("Path Failure")]
    [Tooltip("算不出路徑時多久重試一次（秒）。不要設 0，否則每個 physics step 都會呼叫 NavMesh.CalculatePath。")]
    [SerializeField] protected float pathRetryInterval = 0.25f;

    /// <summary>
    /// 現在有沒有可以走的路徑。沒有的話：把移動輸入歸零，並縮短下一次重算的等待時間。
    ///
    /// ★ 這是這次修的 bug 的另一半。
    ///   舊版每個 Behaviour 開頭都是 `if (path == null || path.Length == 0) return;`，
    ///   一次算不出路徑就把整段行為跳掉，連帶造成三個後果：
    ///
    ///   1. HorizontalMovement 沒被呼叫 → ModularEntityMovement.moveDirection 留著上一次的值
    ///      → 實體以 sprintSpeed 朝著舊方向一直滑，撞牆或滑出 NavMesh。
    ///   2. 狀態轉換全部被跳過 → Idle 不會轉 Chasing、Chasing 不會轉 Combat、
    ///      Combat 不會瞄準開火。從外面看就是「AI 整個死掉」。
    ///   3. 實體因此停在（或滑到）一個 SamplePosition 會失敗的位置，
    ///      下一次 FindPath 的起點還是那裡 → 永遠算不出路 → 死鎖。
    ///
    ///   現在「沒有路徑」是一個明確處理的狀態：停下來、砲塔歸位、盡快重試，
    ///   而狀態轉換與砲塔邏輯照常執行。
    /// </summary>
    protected bool HasUsablePath()
    {
        if (path != null && path.Length > 0) return true;

        livePathLength = 0;
        modularEntityMovement.HorizontalMovement(0f, 0f);   // 清掉殘留的移動輸入，避免無限滑行
        if (destinationTimer > pathRetryInterval)
            destinationTimer = pathRetryInterval;
        return false;
    }
    public Vector3 FindNextWaypoint()
    {
        // ★ 每幀跟 PathFinder 要「用當下船姿態投影出來的」即時路徑，而不是讀凍結快取
        //    → 船移動時航點跟著貼合，不 stale。地面路徑時直接回傳原路徑，行為不變。
        Vector3[] livePath = pathFinder.GetCurrentWorldPath();

        if (livePath == null || livePath.Length == 0)
        {
            livePathLength = 0;
            return transform.position;
        }

        livePathLength = livePath.Length;

        // path 可能換過、變短，夾住避免越界
        if (currentWaypointIndex > livePath.Length - 1)
            currentWaypointIndex = livePath.Length - 1;

        // ★ 舊版只有「距離夠近就 ++」這一條規則，代表實體只能跳過它剛好站在
        //   5 公尺內的 corner。走太快直接掠過、或 corner 落在身後（重算路徑時的
        //   sample 吸附），這條規則都不會成立 → 實體轉頭回去撿那個點。
        //
        //   改成投影定位：找出自己落在折線的哪一段，瞄準那一段的終點。
        //   從 currentWaypointIndex - 1（也就是目前所在線段）開始往前找。
        int resolved = ResolveWaypointIndex(livePath, currentWaypointIndex - 1);

        // 只前進不後退。同一條路徑內索引單調遞增 ——
        // 折返型路徑（例如繞出去又繞回來）上，前後兩段可能貼得很近，
        // 沒有這道閘門就會在兩段之間來回跳，看起來又是在原地打轉。
        // 路徑換掉時由 SetPath 重新定位，不受這道閘門影響。
        if (resolved > currentWaypointIndex)
            currentWaypointIndex = resolved;

        return livePath[currentWaypointIndex];
    }

    /// <summary>
    /// 滾出下一次路徑更新的間隔。
    ///
    /// ★ 靠近停靠點時改用 dockDestinationUpdateRange（更短）。
    ///
    ///   為什麼要這樣：地面路徑（!isOnShip && isTargetOnShip）走的是
    ///   ComputeGroundPath，lastPathOnShip 是 false，所以 GetCurrentWorldPath()
    ///   直接回傳凍結的世界座標 —— 整條路徑在下一次重算之前完全不會跟著船更新。
    ///   終點那個 docking point 也一樣是凍結的。
    ///
    ///   離得遠的時候這無所謂（船的位移相對於路徑長度可以忽略）；
    ///   但快登船時，1~3 秒的凍結誤差就等於整個登船口的寬度，
    ///   敵人會一直撲到船「幾秒前」的位置。靠近時提高更新頻率就能收斂。
    ///
    ///   刻意寫在 RollInterval 而不是各個 state 裡：所有狀態自動適用，
    ///   FalconBrain 覆寫的 PathUpdate 也是呼叫這個方法，一併生效。
    /// </summary>
    protected float RollInterval(Vector2 range)
    {
        if (IsNearDockingPoint())
            range = dockDestinationUpdateRange;

        return Random.Range(range.x, range.y);
    }

    /// <summary>
    /// 離任何一個停靠點夠近嗎。
    /// 用水平距離 —— 飛行單位（Falcon）懸停在 10 公尺高空，
    /// 3D 距離會永遠把飛行高度計進去，等於門檻被墊高。
    /// </summary>
    protected virtual bool IsNearDockingPoint()
    {
        if (dockUpdateDistance <= 0f) return false;

        LandshipNavigation nav = LandshipNavigation.Instance;
        if (nav == null || nav.dockingPoints == null) return false;

        float sqrThreshold = dockUpdateDistance * dockUpdateDistance;
        Vector3 pos = transform.position;

        for (int i = 0; i < nav.dockingPoints.Length; i++)
        {
            Transform dock = nav.dockingPoints[i];
            if (dock == null) continue;

            Vector3 delta = dock.position - pos;
            delta.y = 0f;

            if (delta.sqrMagnitude < sqrThreshold) return true;
        }

        return false;
    }
    protected virtual void IdleBehaviour()
    {
        if (HasUsablePath())
        {
            Vector3 nextWaypoint = FindNextWaypoint();
            Vector3 moveDirection = nextWaypoint - transform.position;
            moveDirection.y = 0f;
            float dist = moveDirection.magnitude;


            ResetTurretAiming(moveDirection); // 巡邏時砲塔不瞄準，避免亂射

            if (IsAtFinalWaypoint(nextWaypoint))
            {
                destinationTimer = 0f;   // 到達巡邏點 → 下一幀重挑新點（留在 Patrolling）
            }
            else if (dist > 0.0001f)   // dist 為 0 時 moveDirection / dist 會變成 NaN，直接污染 Rigidbody
            {
                float throttle = Mathf.Clamp01(dist / slowDownRadius);
                Vector3 dir = moveDirection / dist * throttle;

                float signedAngle = Vector3.SignedAngle(modularEntityMovement.MeshForward, moveDirection, Vector3.up);
                if (Mathf.Abs(signedAngle) < 180)
                {
                    modularEntityMovement.HorizontalMovement(dir.x, dir.z);
                }
                FaceMoveDirection(moveDirection);
            }
        }
        else
        {
            ResetTurretAiming(Vector3.zero);   // 沒路可走 → 砲塔回中立，不要維持上一幀的朝向
        }

        //if there is target in the list, change state to chasing
        // ★ 移到 path 檢查外面：算不出路徑不代表不該切狀態
        if (targets.Count > 0)
        {
            ChangeState(EntityState.Chasing);
        }
    }

    protected virtual void PatrollingBehaviour()
    {
        if (!HasUsablePath())
        {
            ResetTurretAiming(Vector3.zero);
            return;
        }

        Vector3 nextWaypoint = FindNextWaypoint();
        Vector3 moveDirection = nextWaypoint - transform.position;
        moveDirection.y = 0f;
        float dist = moveDirection.magnitude;


        ResetTurretAiming(moveDirection); // 巡邏時砲塔不瞄準，避免亂射

        if (IsAtFinalWaypoint(nextWaypoint))
        {
            destinationTimer = 0f;   // 到達巡邏點 → 下一幀重挑新點（留在 Patrolling）
        }
        else if (dist > 0.0001f)   // 防除以 0 → NaN
        {
            float throttle = Mathf.Clamp01(dist / slowDownRadius);
            Vector3 dir = moveDirection / dist * throttle;

            float signedAngle = Vector3.SignedAngle(modularEntityMovement.MeshForward, moveDirection, Vector3.up);
            if (Mathf.Abs(signedAngle) < 180)
            {
                modularEntityMovement.HorizontalMovement(dir.x, dir.z);
            }
            FaceMoveDirection(moveDirection);
        }
    }

    protected virtual void RetreatBehaviour()
    {
        if (!HasUsablePath())
        {
            ResetTurretAiming(Vector3.zero);
            return;
        }

        Vector3 nextWaypoint = FindNextWaypoint();
        Vector3 moveDirection = nextWaypoint - transform.position;
        moveDirection.y = 0f;

        ResetTurretAiming(moveDirection); // 撤退時砲塔不瞄準，避免亂射

        if (IsAtFinalWaypoint(nextWaypoint))
        {
            ChangeState(EntityState.Patrolling);   // 撤退到位 → 真正切換狀態
        }
        else
        {
            float signedAngle = Vector3.SignedAngle(modularEntityMovement.MeshForward, moveDirection, Vector3.up);
            if (Mathf.Abs(signedAngle) < 120)
            {
                modularEntityMovement.HorizontalMovement(moveDirection.x, moveDirection.z);
            }
            FaceMoveDirection(moveDirection);
        }
    }

    protected virtual void ChasingBehaviour()
    {
        // ★ 目標判定與 Combat 轉換不需要路徑，所以放在 path 檢查前面。
        //   舊版放在後面 → 一次算不出路就永遠卡在 Chasing，不會進 Combat 也不會回 Patrolling。
        Transform target = FindTarget();
        if (target == null) { ChangeState(EntityState.Patrolling); return; }   // 目標消失 → 回巡邏

        if (DistanceToPoint(target.position) < combatActivityAreaRadius / 2)
        {
            ChangeState(EntityState.Combat);   // 進入戰鬥範圍 → 真正切換狀態
            return;
        }

        if (!HasUsablePath())
        {
            ResetTurretAiming(Vector3.zero);
            return;
        }

        Vector3 nextWaypoint = FindNextWaypoint();
        Vector3 moveDirection = nextWaypoint - transform.position;
        moveDirection.y = 0f;

        ResetTurretAiming(moveDirection);   // 追擊時砲塔不瞄準，避免亂射

        if (IsAtFinalWaypoint(nextWaypoint))
        {
            destinationTimer = 0f;   // 走完舊路徑但還沒近 → 下一幀用目標當下位置重算（留在 Chasing）
        }
        else
        {
            float signedAngle = Vector3.SignedAngle(modularEntityMovement.MeshForward, moveDirection, Vector3.up);
            if (Mathf.Abs(signedAngle) < 120)
            {
                modularEntityMovement.HorizontalMovement(moveDirection.x, moveDirection.z);
            }
            FaceMoveDirection(moveDirection);
        }
    }

    protected virtual void CombatBehaviour()
    {
        // ★ 移動段可以沒有路徑，但瞄準開火段一定要照跑。
        //   舊版整段 return → 敵人一旦算不出徘徊路徑就連射擊都停了。
        if (HasUsablePath())
        {
            Vector3 nextWaypoint = FindNextWaypoint();
            Vector3 moveDirection = nextWaypoint - transform.position;
            moveDirection.y = 0f;
            if (IsAtFinalWaypoint(nextWaypoint))
            {
                destinationTimer = 0f;   // 到達徘徊點 → 下一幀重挑新徘徊點（留在 Combat）
            }
            else
            {
                float signedAngle = Vector3.SignedAngle(modularEntityMovement.MeshForward, moveDirection, Vector3.up);
                if (Mathf.Abs(signedAngle) < 160)
                {
                    modularEntityMovement.HorizontalMovement(moveDirection.x, moveDirection.z);
                }
                FaceMoveDirection(moveDirection);
            }
        }

        Transform target = FindTarget();
        if (target != null)
        {
            Rigidbody targetRb = target.GetComponentInParent<Rigidbody>();
            Vector3 targetVel = targetRb != null ? targetRb.linearVelocity : Vector3.zero;
            UpdateTurretAiming(target, targetVel);

        }
    }
    protected virtual void ChangeState(EntityState next)
    {
        currentState = next;
        destinationTimer = 0f;
    }
    protected virtual void FaceMoveDirection(Vector3 worldDir)
    {
        worldDir.y = 0f;
        if (worldDir.sqrMagnitude < 0.0001f) return;

        float signedAngle = Vector3.SignedAngle(modularEntityMovement.MeshForward, worldDir, Vector3.up);
        if (Mathf.Abs(signedAngle) < facingDeadzone) return;

        // 傳入剩餘角度的絕對值，RotateMesh 會夾住不過頭
        modularEntityMovement.RotateMesh(Mathf.Sign(signedAngle), Mathf.Abs(signedAngle));
    }

    // 每座砲塔用「自己的彈速」算自己的攔截點
    protected virtual void UpdateTurretAiming(Transform target, Vector3 targetVelocity)
    {
        for (int i = 0; i < turrets.Length; i++)
        {
            TurretController turret = turrets[i];
            if (turret == null) continue;

            // 讀 turret 自己的 bulletSpeed —— 資料在哪，就去哪讀
            if (MathToolKit.InterceptionPoint(
                    target.position,                  // a: 目標現在位置
                    turret.MuzzlePosition,            // b: 這座砲塔的砲口
                    targetVelocity,                   // vA: 目標速度
                    turret.bulletSpeed,               // sB: 這座砲塔的彈速
                    out Vector3 interceptPoint))
            {
                turret.targetLocation = interceptPoint;
                if (turret.HasLineOfSightTo(target))
                    turret.Shoot();
            }
            else
            {
                // 解不出攔截（目標太快/彈太慢）→ 退回直接瞄準現在位置
                turret.targetLocation = target.position;
                if (turret.HasLineOfSightTo(target))
                    turret.Shoot();   // 直瞄退路也一樣:看得到就打
            }
        }
    }

    //將每座砲塔轉向實體的移動方向（砲管放平）；沒有移動方向時退回中立朝向
    protected virtual void ResetTurretAiming(Vector3 moveDirection)
    {
        if (turrets == null) return;

        moveDirection.y = 0f;
        bool hasDirection = moveDirection.sqrMagnitude > 0.0001f;
        if (hasDirection) moveDirection.Normalize();

        for (int i = 0; i < turrets.Length; i++)
        {
            TurretController turret = turrets[i];
            if (turret == null) continue;

            if (hasDirection)
            {
                // 沿移動方向、與 pitch 軸同高的遠處虛擬點 → yaw 對準移動方向、pitch 歸零
                turret.targetLocation = turret.PitchPivotPosition + moveDirection * 20f;
            }
            else
            {
                // 靜止（沒有移動方向）→ 回中立朝向
                turret.targetLocation = turret.RestAimPoint;
            }
        }
    }

    public virtual void AddTarget(Transform targetTransform, TargetType type, float priority, float priorityDecreaseMultiplier)
    {
        if (targetTransform == null) return;

        // 去重移到這裡。EnemyDetection 呼叫前本來就有檢查，但那個檢查在呼叫端 ——
        // 之後把 TakeDamage 的仇恨接上來時（那是每次中彈都會觸發的高頻呼叫），
        // 沒有這道防線就會瞬間產生大量重複項目。
        //
        // 已存在時取較高的優先度，而不是再加一筆：
        // 重複偵測到同一個目標的語意是「重新確認威脅」，不是「多了一個威脅」。
        for (int i = 0; i < targets.Count; i++)
        {
            Target existing = targets[i];
            if (existing == null || existing.targetTransform != targetTransform) continue;

            existing.type = type;
            existing.priorityDecreaseMultiplier = priorityDecreaseMultiplier;
            existing.targetPriority = Mathf.Max(existing.targetPriority, priority);
            return;
        }

        // 原本沒有設定 type —— Target 的建構子不收它，所以每一筆的 type 都停在
        // 預設值 Player。decayMultiplierByType 是用 type 查表的，等於
        // targetPreferences 對非玩家目標從來沒生效過。
        targets.Add(new Target(targetTransform, priority, priorityDecreaseMultiplier) { type = type });
    }

    protected virtual float DistanceToPoint(Vector3 point)
    {
        return Vector3.Distance(transform.position, point);
    }
}