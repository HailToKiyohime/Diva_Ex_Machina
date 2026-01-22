using UnityEngine;
using UnityEngine.AI;

public enum EnemyState
{
    Idle,
    Chasing,
    Attacking,
}

public class EnemyBrain : MonoBehaviour
{
    public EnemyState currentState = EnemyState.Idle;

    [Header("Repath When Path Finished")]
    [SerializeField] private float repathWhenCloseToLastCorner = 1.0f;
    [SerializeField] private float repathCooldown = 0.25f;

    [Header("State Switch")]
    [Tooltip("避免距離在 combatRange 邊界抖動時狀態瘋狂切換（>0 建議 0.5~2）")]
    [SerializeField] private float combatRangeHysteresis = 1.0f;

    private CreatePath pathFinder;
    private EnemyMovement movement;

    private Vector3 nextMoveLocation;
    private float _nextAllowedRepathTime;

    private FirearmControlSystem firearmSystem;
    [SerializeField] private float fireConeDeg = 10f;

    private void Awake()
    {
        pathFinder = GetComponent<CreatePath>();
        movement = GetComponent<EnemyMovement>();
        firearmSystem = GetComponent<FirearmControlSystem>();
    }

    private void Update()
    {
        if (pathFinder == null || movement == null) return;
        if (pathFinder.Target == null)
        {
            SetState(EnemyState.Idle);
            movement.HorizontalMovement(0f, 0f);
            return;
        }
        if (firearmSystem != null)
        {
            firearmSystem.target = pathFinder.Target;
        }
        // A) 先根據距離切狀態（含 hysteresis）
        UpdateStateByDistance();

        // B) 再跑狀態行為
        switch (currentState)
        {
            case EnemyState.Idle:
                movement.HorizontalMovement(0f, 0f);
                break;

            case EnemyState.Chasing:
                DoChasing();
                break;

            case EnemyState.Attacking:
                DoAttacking();
                break;
        }
    }

    private void UpdateStateByDistance()
    {
        float dist = Vector3.Distance(transform.position, pathFinder.Target.position);
        float enterAttack = pathFinder.CombatRange;                     // 進攻判斷線
        float exitAttack = pathFinder.CombatRange + combatRangeHysteresis; // 離開進攻（回到追擊）判斷線

        if (currentState != EnemyState.Attacking)
        {
            if (dist <= enterAttack)
                SetState(EnemyState.Attacking);
            else
                SetState(EnemyState.Chasing);
        }
        else
        {
            // 已在 Attacking：拉開到 exitAttack 才切回 Chasing，避免抖動
            if (dist >= exitAttack)
                SetState(EnemyState.Chasing);
        }
    }

    private void SetState(EnemyState newState)
    {
        if (currentState == newState) return;

        // Exit old
        switch (currentState)
        {
            case EnemyState.Chasing:
                // 需要的話可在這裡做追擊結束清理
                break;
        }

        currentState = newState;

        // Enter new
        switch (currentState)
        {
            case EnemyState.Chasing:
                // 進入追擊：立刻算一次路，避免剛切回來還沿用舊 corner
                pathFinder.FindPath();
                _nextAllowedRepathTime = 0f;
                break;

            case EnemyState.Attacking:
                // 進入攻擊：先停一下（真正攻擊行為你之後再接 AttackManager/動畫）
                movement.HorizontalMovement(0f, 0f);
                break;
        }
    }

    private void DoChasing()
    {
        // 1) 若已到路徑終點附近，就立即重算路徑（沿用你原本邏輯）
        TryRepathIfPathFinished();

        // 2) 正常沿路徑移動（沿用你原本邏輯）
        nextMoveLocation = pathFinder.FindNextMoveLocation(transform);

        Vector3 dir = nextMoveLocation - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f)
        {
            movement.HorizontalMovement(0f, 0f);
            return;
        }

        dir.Normalize();

        Transform basis = transform;
        Vector3 fwd = basis.forward; fwd.y = 0f;
        Vector3 right = basis.right; right.y = 0f;

        if (fwd.sqrMagnitude > 0.0001f) fwd.Normalize();
        if (right.sqrMagnitude > 0.0001f) right.Normalize();

        float moveZ = Vector3.Dot(fwd, dir);
        float moveX = Vector3.Dot(right, dir);

        movement.HorizontalMovement(moveX, moveZ);
    }

    private void DoAttacking()
    {
        // 最小版本：攻擊狀態先停住（或你想用 circularPathFinding 在近距離繞圈也行）
        // 1) 若已到路徑終點附近，就立即重算路徑（沿用你原本邏輯）
        TryRepathIfPathFinished();

        // 2) 正常沿路徑移動（沿用你原本邏輯）
        nextMoveLocation = pathFinder.FindNextMoveLocation(transform);

        Vector3 dir = nextMoveLocation - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f)
        {
            movement.HorizontalMovement(0f, 0f);
            return;
        }

        dir.Normalize();

        Transform basis = transform;
        Vector3 fwd = basis.forward; fwd.y = 0f;
        Vector3 right = basis.right; right.y = 0f;

        if (fwd.sqrMagnitude > 0.0001f) fwd.Normalize();
        if (right.sqrMagnitude > 0.0001f) right.Normalize();

        float moveZ = Vector3.Dot(fwd, dir);
        float moveX = Vector3.Dot(right, dir);

        movement.HorizontalMovement(moveX, moveZ);

        // 之後要接：面向目標、播放攻擊動畫、觸發 AttackManager 等
        // 只有 Attacking 狀態才觸發開火（主人需求 #3）
        if (firearmSystem != null && currentState == EnemyState.Attacking)
        {
            firearmSystem.RequestFireAll(fireConeDeg); // 主人需求 #1 + #2
        }
    }

    private void TryRepathIfPathFinished()
    {
        if (Time.time < _nextAllowedRepathTime) return;

        NavMeshPath p = pathFinder.GetPath();
        if (p == null || p.corners == null) return;

        int len = p.corners.Length;

        if (len < 2)
        {
            pathFinder.FindPath();
            _nextAllowedRepathTime = Time.time + repathCooldown;
            return;
        }

        Vector3 pos = transform.position;
        pos.y = 0f;

        int closestIndex = -1;
        float bestSqr = float.PositiveInfinity;

        for (int i = 0; i < len; i++)
        {
            Vector3 c = p.corners[i];
            c.y = 0f;

            float sqr = (c - pos).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                closestIndex = i;
            }
        }

        float thresholdSqr = repathWhenCloseToLastCorner * repathWhenCloseToLastCorner;
        if (closestIndex == len - 1 && bestSqr <= thresholdSqr)
        {
            pathFinder.FindPath();
            _nextAllowedRepathTime = Time.time + repathCooldown;
        }
    }
}
