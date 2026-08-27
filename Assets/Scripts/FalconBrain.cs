using NaughtyAttributes;
using UnityEngine;

// ★ 舊版第 2 行有 `using UnityEditor.Timeline.Actions;` —— 已移除。
//   那是 IDE 自動補上的，程式碼裡根本沒用到。但 UnityEditor 命名空間
//   不存在於 player build，留著會讓整個專案「在 Editor 跑得好好的、
//   一按 Build 就編譯失敗」。這是最優先要拿掉的一行。

public class FalconBrain : ModularEntityBrain
{
    private FalconMovement falconMovement;

    // ⚠ 欄位名保留 modularEntityStats，改名會讓 Inspector 上已指好的引用掉線。
    //    注意這跟 FalconMovement 從 ModularEntityMovement 繼承來的
    //    modularEntityStats 是「兩個獨立欄位」，Inspector 上要指到同一個
    //    component，否則攔截點算的 sprintSpeed 會跟實際飛行速度對不上。
    public FalconStats modularEntityStats;

    [Header("Flight Height (per state)")]
    [SerializeField] private float idleHeight = 10f;
    [SerializeField] private float patrolHeight = 10f;
    [SerializeField] private float retreatHeight = 10f;
    [SerializeField] private float chaseHeight = 10f;
    [SerializeField] private float combatHeight = 10f;

    [Header("Flight Handling")]
    [Tooltip("機首與航點方向夾角越大、油門越小。0 = 關閉（維持舊行為：不管朝哪都全速）。" +
             "建議 0.5~0.8，可以明顯減少「重算路徑後先全速衝向反方向、再慢慢繞回來」的觀感。")]
    [Range(0f, 1f)]
    [SerializeField] private float headingThrottleGate = 0f;

    [Header("Retreat Setting")]
    [MinMaxSlider(0f, 100f)]
    public Vector2 retreatAreaRadius = new Vector2(50f, 60f);
    [Tooltip("撤退到位後，在 Combat 停留多久才再次撤退。")]
    [MinMaxSlider(0f, 30f)]
    [SerializeField] private Vector2 retreatCycleDelay = new Vector2(5f, 10f);

    [Header("Missile")]
    public MissileLauncherController missileLauncherController;
    [SerializeField] private float missileLaunchDelay = 1.5f;
    [Tooltip("發射時高度不夠 → 隔多久重試一次。")]
    [SerializeField] private float missileRetryInterval = 0.5f;
    [SerializeField] private int missileMaxRetries = 10;
    private int missileRetriesLeft;

    protected override void Start()
    {
        base.Start();

        falconMovement = modularEntityMovement as FalconMovement;
        if (falconMovement == null)
            Debug.LogError($"{name}: modularEntityMovement 不是 FalconMovement，飛行邏輯會失效。");

        if (modularEntityStats == null)
            Debug.LogError($"{name}: FalconStats 未指定，Combat 的攔截點計算會噴 NRE。");
    }

    /// <summary>物件池回收 / 停用時清掉排程，避免復活後突然自己切狀態或發射導彈。</summary>
    protected virtual void OnDisable()
    {
        CancelInvoke();
    }

    // ============================================================
    // 飛行移動：先轉向航點，再沿「機首方向」前進；接近航點時降低 throttle 好轉彎。
    // 不需要 base 的角度 gate —— 隼永遠朝機首飛，不存在「方向不對先別動」的情況。
    // ============================================================
    private void FlyTowards(Vector3 moveDirection, float dist)
    {
        FaceMoveDirection(moveDirection);   // 先把 mesh 轉向航點

        // 越接近航點 → throttle 越小 → 速度越低 → 迴轉半徑越小，好轉彎
        float throttle = Mathf.Clamp01(dist / slowDownRadius);

        // ★ 機首還沒轉過來時降油門（可選，headingThrottleGate = 0 時完全不生效）。
        //
        //   隼是朝 MeshForward 飛的，所以路徑重算後如果新航點在身後，
        //   它會「全速朝舊方向飛出去，同時慢慢轉彎」—— 從畫面上看就是在回頭。
        //   這跟 waypoint index 那個 bug 是兩回事，index 修好之後這個還在。
        //   降油門讓它幾乎原地轉向，轉好再加速。
        if (headingThrottleGate > 0f && dist > 0.0001f)
        {
            float align = Vector3.Dot(modularEntityMovement.MeshForward, moveDirection / dist); // -1..1
            float aligned01 = (align + 1f) * 0.5f;                                              //  0..1
            throttle *= Mathf.Lerp(1f, aligned01, headingThrottleGate);
        }

        // ★ 用 MeshForward 而非 moveDirection：隼只會朝機首飛
        Vector3 dir = modularEntityMovement.MeshForward * throttle;
        modularEntityMovement.HorizontalMovement(dir.x, dir.z);
    }

    // 停止：交給 base 的減速邏輯收到零，靠 VerticalMovement 懸停在空中
    private void Hover()
    {
        modularEntityMovement.HorizontalMovement(0f, 0f);
    }

    /// <summary>falconMovement 為 null 時不要每幀噴 NRE。</summary>
    private void HoldAltitude(float height)
    {
        if (falconMovement == null) return;
        falconMovement.VerticalMovement(height);
    }

    protected override void IdleBehaviour()
    {
        // 高度控制放在最前面：即使沒有路徑也要維持懸停，否則會掉下去
        HoldAltitude(idleHeight);

        // ★ 舊版：if (path == null || path.Length == 0) return;
        //   base class 已經把這個寫法標成 bug 並改掉了，但 Falcon override 之後
        //   又把它複製了一份回來。後果跟 base 註解寫的一模一樣：
        //     1. HorizontalMovement 沒被呼叫 → moveDirection 留著上一次的值
        //        → 隼以 sprintSpeed 朝舊方向一直飛走，而且它在空中，
        //          不會像地面單位那樣撞牆停下來，會直接飛出地圖。
        //     2. 下面的 targets.Count 判定被跳過 → 算不出路徑就永遠不會進 Chasing。
        //     3. destinationTimer 不會縮短 → 不會盡快重試。
        //   HasUsablePath() 三件事一次處理掉。
        if (HasUsablePath())
        {
            Vector3 nextWaypoint = FindNextWaypoint();
            Vector3 moveDirection = nextWaypoint - transform.position;
            moveDirection.y = 0f;                    // 只看水平：航點在地面、隼在空中
            float dist = moveDirection.magnitude;

            ResetTurretAiming(moveDirection);

            // ★ 一定要用吃 float 的多載，傳入「水平」距離。
            //   吃 Vector3 的版本是 3D 距離，隼在 10 公尺高空永遠 ≥ 10 > 5，判定永遠 false。
            if (IsAtFinalWaypoint(dist))
                Hover();   // 到達 idle 點 → 原地懸停，等 destinationTimer 自然滾出下一個點
            else
                FlyTowards(moveDirection, dist);
        }
        else
        {
            // HasUsablePath() 失敗時已經幫忙把水平輸入歸零（等同 Hover）
            ResetTurretAiming(Vector3.zero);
        }

        // ★ 移到 path 檢查外面：算不出路徑不代表不該切狀態
        if (targets.Count != 0)
            ChangeState(EntityState.Chasing);
    }

    protected override void PatrollingBehaviour()
    {
        HoldAltitude(patrolHeight);

        if (!HasUsablePath())
        {
            ResetTurretAiming(Vector3.zero);
            return;
        }

        Vector3 nextWaypoint = FindNextWaypoint();
        Vector3 moveDirection = nextWaypoint - transform.position;
        moveDirection.y = 0f;
        float dist = moveDirection.magnitude;

        ResetTurretAiming(moveDirection);

        if (IsAtFinalWaypoint(dist))
            destinationTimer = 0f;   // 到達巡邏點 → 下一幀重挑新點（留在 Patrolling）
        else
            FlyTowards(moveDirection, dist);
    }

    protected override void RetreatBehaviour()
    {
        HoldAltitude(retreatHeight);

        // ★ 舊版：... || Vector3.Distance(transform.position, FindTarget().position) > ...
        //
        //   兩個問題疊在一起：
        //   (1) FindTarget() 可能回傳 null。PruneDestroyedTargets 會在目標死掉的
        //       下一個 physics step 把它清掉，而 Retreat 正好是「剛打完一輪」的狀態
        //       —— 目標在這時候死掉的機率一點都不低。一旦是 null 就直接 NRE，
        //       整個 FixedUpdate 中斷，AI 當場僵住。
        //   (2) C# 的 && 優先於 ||，所以 `a && b || c` 實際是 `(a && b) || c`。
        //       語意上多半是對的，但完全靠讀者記住優先級，很容易改壞。
        //
        //   目標判定拉到最前面，跟 base 的 ChasingBehaviour 一致。
        Transform target = FindTarget();
        if (target == null)
        {
            ChangeState(EntityState.Patrolling);   // 沒有目標就沒有撤退的意義
            return;
        }

        // ★ 只看水平距離。舊版用 3D 距離 → 飛行高度永遠被計入
        //   → 門檻等於被墊高了 retreatHeight，撤退提早結束。
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        bool farEnough = toTarget.magnitude > combatActivityAreaRadius;

        bool hasPath = HasUsablePath();
        bool arrived = false;
        Vector3 moveDirection = Vector3.zero;
        float dist = 0f;

        if (hasPath)
        {
            Vector3 nextWaypoint = FindNextWaypoint();
            moveDirection = nextWaypoint - transform.position;
            moveDirection.y = 0f;
            dist = moveDirection.magnitude;

            ResetTurretAiming(moveDirection);
            arrived = IsAtFinalWaypoint(dist);
        }
        else
        {
            ResetTurretAiming(Vector3.zero);
        }

        if (arrived || farEnough)
        {
            ChangeState(EntityState.Combat);   // 撤退到位 → 真正切換狀態

            // ★ 加上 IsInvoking 檢查。沒有的話，只要這個分支在狀態真正切換前
            //   被走到兩次，就會疊出兩個 pending invoke，之後互相打架。
            if (!IsInvoking(nameof(ChangeStateToRetreat)))
                Invoke(nameof(ChangeStateToRetreat), Random.Range(retreatCycleDelay.x, retreatCycleDelay.y));
        }
        else if (hasPath)
        {
            FlyTowards(moveDirection, dist);
        }
    }

    private void ChangeStateToRetreat()
    {
        // 舊版在這裡呼叫 CancelInvoke(nameof(ChangeStateToRetreat)) —— 沒有意義。
        // Invoke 觸發的當下排程就已經消耗掉了，沒有東西可以取消。
        ChangeState(EntityState.Retreat);
    }

    protected override void ChasingBehaviour()
    {
        HoldAltitude(chaseHeight);

        // ★ 目標判定與 Combat 轉換不需要路徑，所以放在 path 檢查前面。
        //   舊版放在後面 → 一次算不出路就永遠卡在 Chasing，不會進 Combat 也不會回 Patrolling。
        Transform target = FindTarget();
        if (target == null) { ChangeState(EntityState.Patrolling); return; }   // 目標消失 → 回巡邏

        // 到目標的距離也只看水平，否則懸停高度會永遠計入，進不了 Combat
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.magnitude < combatActivityAreaRadius / 2)
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
        float dist = moveDirection.magnitude;

        ResetTurretAiming(moveDirection);

        if (IsAtFinalWaypoint(dist))
            destinationTimer = 0f;   // 走完舊路徑但還沒近 → 下一幀用目標當下位置重算
        else
            FlyTowards(moveDirection, dist);
    }

    protected override void CombatBehaviour()
    {
        HoldAltitude(combatHeight);

        // ★ 移動段可以沒有路徑，但瞄準開火段一定要照跑。
        //   舊版整段 return → 隼一旦算不出徘徊路徑就連射擊都停了，
        //   而且會維持上一幀的 moveDirection 一路飛走。
        if (HasUsablePath())
        {
            Vector3 nextWaypoint = FindNextWaypoint();
            Vector3 moveDirection = nextWaypoint - transform.position;
            moveDirection.y = 0f;
            float dist = moveDirection.magnitude;

            if (IsAtFinalWaypoint(dist))
            {
                if (Random.Range(0, 2) == 0)
                {
                    ScheduleMissile();

                    if (turrets != null)
                    {
                        for (int i = 0; i < turrets.Length; i++)
                        {
                            TurretController turret = turrets[i];
                            if (turret == null) continue;
                            turret.Reload();   // 重新裝填子彈
                        }
                    }

                    ChangeState(EntityState.Retreat);
                }
                else
                {
                    destinationTimer = 0f;   // 走完舊路徑但還沒近 → 下一幀用目標當下位置重算
                }
            }
            else
            {
                FlyTowards(moveDirection, dist);
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

    protected override void PathUpdate()
    {
        destinationTimer -= Time.fixedDeltaTime;
        if (destinationTimer > 0f) return;

        Vector2 r;
        switch (currentState)
        {
            case EntityState.Idle:
                {
                    destinationTimer = RollInterval(idleDestinationUpdateRange);
                    r = Random.insideUnitCircle * idleActivityAreaRadius;
                    destination = spawnLocation + new Vector3(r.x, 0f, r.y);
                    SyncShipFlags(null);
                    SetPath(pathFinder.FindPath(destination));
                    break;
                }
            case EntityState.Patrolling:
                {
                    destinationTimer = RollInterval(patrolDestinationUpdateRange);
                    r = Random.insideUnitCircle * patrolActivityAreaRadius;
                    destination = spawnLocation + new Vector3(r.x, 0f, r.y);
                    SyncShipFlags(null);
                    SetPath(pathFinder.FindPath(destination));
                    break;
                }
            case EntityState.Retreat:
                {
                    destinationTimer = RollInterval(retreatDestinationUpdateRange);
                    Transform chaseTarget = FindTarget();
                    if (chaseTarget == null) { ChangeState(EntityState.Patrolling); break; }   // 沒目標 → 回巡邏
                    destination = MathToolKit.GetRandomPointInRing(chaseTarget.position, retreatAreaRadius.y, retreatAreaRadius.x);
                    SyncShipFlags(chaseTarget);
                    SetPath(pathFinder.FindPath(destination));
                    break;
                }
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

                    // ★ 舊版直接 combatTarget.GetComponentInParent<Rigidbody>().linearVelocity
                    //   → 目標是靜態建築 / Core（TargetType.Building、TargetType.Core，
                    //     那些東西沒有 Rigidbody）時整段噴 NRE，PathUpdate 中斷，
                    //     連帶 StateBehaviour 也不會跑。
                    //   base 的 CombatBehaviour 早就有這個防呆，這裡漏了。
                    Rigidbody targetRb = combatTarget.GetComponentInParent<Rigidbody>();
                    Vector3 targetVel = (targetRb != null) ? targetRb.linearVelocity * 0.5f : Vector3.zero;

                    if (MathToolKit.InterceptionPoint(combatTarget.position, transform.position, targetVel,
                                                      modularEntityStats.sprintSpeed, out Vector3 interceptPoint))
                        destination = MathToolKit.GetPointAtTargetBack(transform.position, interceptPoint, 10);
                    else
                        destination = MathToolKit.GetPointAtTargetBack(transform, combatTarget, 10);

                    SyncShipFlags(combatTarget);
                    SetPath(pathFinder.FindPath(destination));
                    break;
                }
        }
    }

    private void ScheduleMissile()
    {
        missileRetriesLeft = missileMaxRetries;
        if (!IsInvoking(nameof(LaunchMissile)))
            Invoke(nameof(LaunchMissile), missileLaunchDelay);
    }

    private void LaunchMissile()
    {
        if (missileLauncherController == null) return;

        // ★ 高度不夠時，舊版就這樣默默放棄了（那個 CancelInvoke 也沒有作用，
        //   Invoke 已經觸發，沒有排程可以取消）。
        //   而 1.5 秒後正好是隼剛切進 Retreat、還在爬升的時候 —— 也就是說
        //   「高度不足」不是例外情況，是常態。導彈常常一發都沒射出去。
        //   改成隔一小段時間重試，有次數上限避免無限排程。
        if (falconMovement == null || falconMovement.FlyHeight() <= retreatHeight * 0.5f)
        {
            if (missileRetriesLeft-- > 0)
                Invoke(nameof(LaunchMissile), missileRetryInterval);
            return;
        }

        Transform target = FindTarget();
        if (target == null) return;

        StartCoroutine(missileLauncherController.Launch(target));
    }
}