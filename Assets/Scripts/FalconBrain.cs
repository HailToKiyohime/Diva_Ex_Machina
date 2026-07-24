using NaughtyAttributes;
using UnityEngine;

public class FalconBrain : ModularEntityBrain
{
    private FalconMovement falconMovement;

    [Header("Flight Height (per state)")]
    [SerializeField] private float idleHeight = 10f;
    [SerializeField] private float patrolHeight = 10f;
    [SerializeField] private float retreatHeight = 10f;
    [SerializeField] private float chaseHeight = 10f;
    [SerializeField] private float combatHeight = 10f;
    [Header("Retreat Setting")]
    [MinMaxSlider(0f, 100f)]
    public Vector2 retreatAreaRadius = new Vector2(50f, 60f);
    protected override void Start()
    {
        base.Start();

        falconMovement = modularEntityMovement as FalconMovement;
        if (falconMovement == null)
            Debug.LogError($"{name}: modularEntityMovement 不是 FalconMovement，飛行邏輯會失效。");
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

        // ★ 用 MeshForward 而非 moveDirection：隼只會朝機首飛
        Vector3 dir = modularEntityMovement.MeshForward * throttle;
        modularEntityMovement.HorizontalMovement(dir.x, dir.z);
    }

    // 停止：交給 base 的減速邏輯收到零，靠 VerticalMovement 懸停在空中
    private void Hover()
    {
        modularEntityMovement.HorizontalMovement(0f, 0f);
    }

    protected override void IdleBehaviour()
    {
        // 高度控制放在最前面：即使沒有路徑也要維持懸停，否則會掉下去
        falconMovement.VerticalMovement(idleHeight);

        if (path == null || path.Length == 0) return;

        Vector3 nextWaypoint = FindNextWaypoint();
        Vector3 moveDirection = nextWaypoint - transform.position;
        moveDirection.y = 0f;                    // 只看水平：航點在地面、隼在空中
        float dist = moveDirection.magnitude;

        ResetTurretAiming(moveDirection);

        if (dist < waypointArriveRadius)
        {
            Hover();   // 到達路徑終點 → 停下來懸停（跟一般敵人一樣會停）
        }
        else
        {
            FlyTowards(moveDirection, dist);
        }

        if (targets.Count != 0)
        {
            ChangeState(EntityState.Chasing);
        }
    }

    protected override void PatrollingBehaviour()
    {
        falconMovement.VerticalMovement(patrolHeight);

        if (path == null || path.Length == 0) return;

        Vector3 nextWaypoint = FindNextWaypoint();
        Vector3 moveDirection = nextWaypoint - transform.position;
        moveDirection.y = 0f;
        float dist = moveDirection.magnitude;

        ResetTurretAiming(moveDirection);

        if (currentWaypointIndex == path.Length - 1 && dist < waypointArriveRadius)
        {
            destinationTimer = 0f;   // 到達巡邏點 → 下一幀重挑新點（留在 Patrolling）
        }
        else
        {
            FlyTowards(moveDirection, dist);
        }
    }

    protected override void RetreatBehaviour()
    {
        falconMovement.VerticalMovement(retreatHeight);

        if (path == null || path.Length == 0) return;

        Vector3 nextWaypoint = FindNextWaypoint();
        Vector3 moveDirection = nextWaypoint - transform.position;
        moveDirection.y = 0f;
        float dist = moveDirection.magnitude;

        ResetTurretAiming(moveDirection);
        if (currentWaypointIndex == path.Length - 1 && dist < waypointArriveRadius|| Vector3.Distance(transform.position, FindTarget().position)> combatActivityAreaRadius)
        {
            ChangeState(EntityState.Combat);   // 撤退到位 → 真正切換狀態
        }
        else
        {
            FlyTowards(moveDirection, dist);
        }
    }

    protected override void ChasingBehaviour()
    {
        falconMovement.VerticalMovement(chaseHeight);

        if (path == null || path.Length == 0) return;

        Transform target = FindTarget();
        if (target == null) { ChangeState(EntityState.Patrolling); return; }   // 目標消失 → 回巡邏

        Vector3 nextWaypoint = FindNextWaypoint();
        Vector3 moveDirection = nextWaypoint - transform.position;
        moveDirection.y = 0f;
        float dist = moveDirection.magnitude;

        ResetTurretAiming(moveDirection);

        // 到目標的距離也只看水平，否則懸停高度會永遠計入，進不了 Combat
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude < combatActivityAreaRadius / 2)
        {
            ChangeState(EntityState.Combat);   // 進入戰鬥範圍 → 真正切換狀態
        }
        else if (currentWaypointIndex == path.Length - 1 && dist < waypointArriveRadius)
        {
            destinationTimer = 0f;   // 走完舊路徑但還沒近 → 下一幀用目標當下位置重算
        }
        else
        {
            FlyTowards(moveDirection, dist);
        }
    }

    protected override void CombatBehaviour()
    {
        falconMovement.VerticalMovement(combatHeight);

        if (path == null || path.Length == 0) return;

        Vector3 nextWaypoint = FindNextWaypoint();
        Vector3 moveDirection = nextWaypoint - transform.position;
        moveDirection.y = 0f;
        float dist = moveDirection.magnitude;

        if (currentWaypointIndex == path.Length - 1 && dist < waypointArriveRadius)
        {
            ChangeState(EntityState.Retreat);
        }
        else
        {
            FlyTowards(moveDirection, dist);
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
                    destination = MathToolKit.GetRandomPointInRing(chaseTarget.position, retreatAreaRadius.y,retreatAreaRadius.x);
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
                    destination = MathToolKit.GetPointAtTargetBack(transform, combatTarget,5);
                    SyncShipFlags(combatTarget);
                    SetPath(pathFinder.FindPath(destination));
                    break;
                }
        }
    }
}