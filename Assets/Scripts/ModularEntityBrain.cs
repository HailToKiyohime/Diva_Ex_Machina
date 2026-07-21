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
    [SerializeField] private TurretController[] turrets;   // 0..N 座，Inspector 拖或自動抓

    private PathFinder pathFinder;
    private ShipPassenger selfPassenger;   // 自己的船上狀態（掛在同一個 GameObject 上）

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

    private float destinationTimer;

    private readonly Dictionary<TargetType, float> decayMultiplierByType = new Dictionary<TargetType, float>();

    [SerializeField] private float waypointArriveRadius = 5f;
    [SerializeField] private float slowDownRadius = 6f;
    private int currentWaypointIndex = 0;

    [SerializeField] private float facingDeadzone = 1f;   // 角度誤差小於此就不轉，避免抖動

    public void Start()
    {
        spawnLocation = transform.position;
        pathFinder = gameObject.GetComponent<PathFinder>();
        selfPassenger = gameObject.GetComponent<ShipPassenger>();
        currentState = EntityState.Idle;
        RebuildPreferenceCache();

        if (turrets == null || turrets.Length == 0)
            turrets = GetComponentsInChildren<TurretController>();
    }

    public void FixedUpdate()
    {
        PriorityUpdate();
        PathUpdate();
        StateBehaviour();
    }

    protected void PriorityUpdate()
    {
        float dt = Time.fixedDeltaTime;
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
    protected void RebuildPreferenceCache()
    {
        decayMultiplierByType.Clear();
        for (int i = 0; i < targetPreferences.Count; i++)
        {
            decayMultiplierByType[targetPreferences[i].targetType] = targetPreferences[i].priorityDecreaseMultiplier;
        }
    }

    protected void StateBehaviour()
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
    private void SyncShipFlags(Transform target)
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

    protected void PathUpdate()
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
        if (targets.Count == 0) return null;
        Target bestTarget = targets[0];
        for (int i = 1; i < targets.Count; i++)
        {
            if (targets[i].targetPriority > bestTarget.targetPriority)
                bestTarget = targets[i];
        }
        return bestTarget.targetTransform;
    }
    private void SetPath(Vector3[] newPath)
    {
        path = newPath;
        currentWaypointIndex = 0;
    }
    public Vector3 FindNextWaypoint()
    {
        // ★ 每幀跟 PathFinder 要「用當下船姿態投影出來的」即時路徑，而不是讀凍結快取
        //    → 船移動時航點跟著貼合，不 stale。地面路徑時直接回傳原路徑，行為不變。
        Vector3[] livePath = pathFinder.GetCurrentWorldPath();

        if (livePath == null || livePath.Length == 0)
            return transform.position;

        // path 可能換過、變短，夾住避免越界
        if (currentWaypointIndex > livePath.Length - 1)
            currentWaypointIndex = livePath.Length - 1;

        // 已經抵達的 corner 就往前推進；到最後一個就停在那，不再 +1（自然不會越界）
        while (currentWaypointIndex < livePath.Length - 1 &&
               Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(livePath[currentWaypointIndex].x, livePath[currentWaypointIndex].z)) < waypointArriveRadius)
        {
            currentWaypointIndex++;
        }

        return livePath[currentWaypointIndex];
    }

    private float RollInterval(Vector2 range)
    {
        return Random.Range(range.x, range.y);
    }
    protected void IdleBehaviour()
    {
        if (path == null || path.Length == 0) return;
        Vector3 nextWaypoint = FindNextWaypoint();
        Vector3 moveDirection = nextWaypoint - transform.position;
        moveDirection.y = 0f;
        float dist = moveDirection.magnitude;

        ResetTurretAiming(moveDirection); // 閒置時砲塔不瞄準，避免亂射

        if (dist < waypointArriveRadius)
        {
            modularEntityMovement.HorizontalMovement(0f, 0f);
        }
        else
        {
            float throttle = Mathf.Clamp01(dist / slowDownRadius);
            Vector3 dir = moveDirection / dist * throttle;

            float signedAngle = Vector3.SignedAngle(modularEntityMovement.MeshForward, moveDirection, Vector3.up);
            if (Mathf.Abs(signedAngle) < 30)
            {
                modularEntityMovement.HorizontalMovement(dir.x, dir.z);
            }
            FaceMoveDirection(moveDirection);
        }

    }

    protected void PatrollingBehaviour()
    {
        if (path == null || path.Length == 0) return;
        Vector3 nextWaypoint = FindNextWaypoint();
        Vector3 moveDirection = nextWaypoint - transform.position;
        moveDirection.y = 0f;
        float dist = moveDirection.magnitude;


        ResetTurretAiming(moveDirection); // 巡邏時砲塔不瞄準，避免亂射

        if (currentWaypointIndex == path.Length - 1 && Vector3.Distance(transform.position, nextWaypoint) < waypointArriveRadius)
        {
            destinationTimer = 0f;   // 到達巡邏點 → 下一幀重挑新點（留在 Patrolling）
        }
        else
        {
            float throttle = Mathf.Clamp01(dist / slowDownRadius);
            Vector3 dir = moveDirection / dist * throttle;

            float signedAngle = Vector3.SignedAngle(modularEntityMovement.MeshForward, moveDirection, Vector3.up);
            if (Mathf.Abs(signedAngle) < 60)
            {
                modularEntityMovement.HorizontalMovement(dir.x, dir.z);
            }
            FaceMoveDirection(moveDirection);
        }
    }

    protected void RetreatBehaviour()
    {
        if (path == null || path.Length == 0) return;
        Vector3 nextWaypoint = FindNextWaypoint();
        Vector3 moveDirection = nextWaypoint - transform.position;
        moveDirection.y = 0f;

        ResetTurretAiming(moveDirection); // 撤退時砲塔不瞄準，避免亂射

        if (currentWaypointIndex == path.Length - 1 && Vector3.Distance(transform.position, nextWaypoint) < waypointArriveRadius)
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

    protected void ChasingBehaviour()
    {
        if (path == null || path.Length == 0) return;

        Transform target = FindTarget();
        if (target == null) { ChangeState(EntityState.Patrolling); return; }   // 目標消失 → 回巡邏

        Vector3 nextWaypoint = FindNextWaypoint();
        Vector3 moveDirection = nextWaypoint - transform.position;
        moveDirection.y = 0f;

        ResetTurretAiming(moveDirection);   // 追擊時砲塔不瞄準，避免亂射

        if (Vector3.Distance(transform.position, target.position) < combatActivityAreaRadius / 2)
        {
            ChangeState(EntityState.Combat);   // 進入戰鬥範圍 → 真正切換狀態
        }
        else if (currentWaypointIndex == path.Length - 1 && Vector3.Distance(transform.position, nextWaypoint) < waypointArriveRadius)
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

    protected void CombatBehaviour()
    {
        if (path == null || path.Length == 0) return;
        Vector3 nextWaypoint = FindNextWaypoint();
        Vector3 moveDirection = nextWaypoint - transform.position;
        moveDirection.y = 0f;
        if (currentWaypointIndex == path.Length - 1 && Vector3.Distance(transform.position, nextWaypoint) < waypointArriveRadius)
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

        Transform target = FindTarget();
        if (target != null)
        {
            Rigidbody targetRb = target.GetComponentInParent<Rigidbody>();
            Vector3 targetVel = targetRb != null ? targetRb.linearVelocity : Vector3.zero;
            UpdateTurretAiming(target, targetVel);

        }
    }
    public void ChangeState(EntityState next)
    {
        currentState = next;
        destinationTimer = 0f;
    }
    private void FaceMoveDirection(Vector3 worldDir)
    {
        worldDir.y = 0f;
        if (worldDir.sqrMagnitude < 0.0001f) return;

        float signedAngle = Vector3.SignedAngle(modularEntityMovement.MeshForward, worldDir, Vector3.up);
        if (Mathf.Abs(signedAngle) < facingDeadzone) return;

        // 傳入剩餘角度的絕對值，RotateMesh 會夾住不過頭
        modularEntityMovement.RotateMesh(Mathf.Sign(signedAngle), Mathf.Abs(signedAngle));
    }

    // 每座砲塔用「自己的彈速」算自己的攔截點
    private void UpdateTurretAiming(Transform target, Vector3 targetVelocity)
    {
        for (int i = 0; i < turrets.Length; i++)
        {
            TurretController turret = turrets[i];
            if (turret == null) continue;

            // 讀 turret 自己的 bulletSpeed —— 資料在哪，就去哪讀
            if (ProjectileCalculation.InterceptionPoint(
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
    private void ResetTurretAiming(Vector3 moveDirection)
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

    public void AddTarget(Transform targetTransform, TargetType type, float priority, float priorityDecreaseMultiplier)
    {
        if (targetTransform == null) return;
        // 已經有這個目標了？就更新它的優先度
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i].targetTransform == targetTransform)
            {
                targets[i].targetPriority = priority;
                targets[i].priorityDecreaseMultiplier = priorityDecreaseMultiplier;
                return;
            }
        }
        // 沒有就新增
        targets.Add(new Target(targetTransform, priority, priorityDecreaseMultiplier));
    }
}