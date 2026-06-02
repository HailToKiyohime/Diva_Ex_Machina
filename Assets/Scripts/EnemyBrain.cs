using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public enum EnemyState
{
    Idle,
    Chasing,
    Attacking,
}

[System.Serializable]
public class TargetPriority
{
    public Transform target;
    public int baseAggro;               // Fixed priority — never decays
    public bool isMainTarget;
    public float damageCauseByTarget;   // Raw damage accumulation — decays when out of range
    public int damageAggro;             // Stepped aggro derived from damageCauseByTarget — decays in sync

    public int TotalAggro => baseAggro + damageAggro;
}

public class EnemyBrain : MonoBehaviour
{
    public List<TargetPriority> targetList = new List<TargetPriority>();

    public Vector3 spwanLocation;

    public EnemyState currentState = EnemyState.Idle;

    [Header("Repath When Path Finished")]
    [SerializeField] private float repathWhenCloseToLastCorner = 1.0f;
    [SerializeField] private float repathCooldown = 0.25f;

    [Header("State Switch")]
    [Tooltip("Hysteresis buffer to prevent state flipping near combat range (0.5~2 is common).")]
    [SerializeField] private float combatRangeHysteresis = 1.0f;

    private CreatePath pathFinder;
    private EnemyMovement movement;

    private Vector3 nextMoveLocation;
    private float _nextAllowedRepathTime;

    private FirearmControlSystem firearmSystem;
    [SerializeField] private float fireConeDeg = 5f;

    public float retargetInterval = 2f;
    private float retargetTimer = 0f;

    [Tooltip("Only targets within this distance are considered when retargeting (0 = unlimited).")]
    [SerializeField] private float retargetRange = 50f;

    [Tooltip("Rate at which damageCauseByTarget decays per second when the target is outside retargetRange.")]
    [SerializeField] private float damageFalloffRate = 5f;

    [Tooltip("Damage required to gain 1 damageAggro step. Shared with EnemyStats.AddAggro via DamagePerAggroStep.")]
    [SerializeField] private float damagePerAggroStep = 50f;

    /// <summary>Exposed so EnemyStats.AddAggro can use the same threshold.</summary>
    public float DamagePerAggroStep => damagePerAggroStep;

    // ============================
    // Attack Limiter
    // Only enemies holding a slot are allowed to shoot.
    // ============================
    private bool _hasAttackSlot;

    public Transform currentTargetTransform = null;

    [SerializeField] private bool onShiped = false;

    private void Awake()
    {
        pathFinder = GetComponent<CreatePath>();
        movement = GetComponent<EnemyMovement>();
        firearmSystem = GetComponent<FirearmControlSystem>();
    }

    private void OnDisable()
    {
        ReleaseAttackSlot();
    }

    private void OnDestroy()
    {
        ReleaseAttackSlot();
    }

    private void Update()
    {
        if (pathFinder == null || movement == null) return;

        bool hasAnyValidTarget =
            (pathFinder != null && pathFinder.HasNavTarget) ||
            (targetList != null && targetList.Exists(t => t != null && t.target != null));

        if (!hasAnyValidTarget)
        {
            ReleaseAttackSlot();
            SetState(EnemyState.Idle);
            movement.HorizontalMovement(0f, 0f);
            return;
        }

        if (firearmSystem != null)
        {
            if (currentTargetTransform != null)
                firearmSystem.defaultTarget = currentTargetTransform;
        }

        // Retargeting: periodically reconsider target priorities and possibly switch targets
        DecayOutOfRangeTargets();
        retargetTimer += Time.deltaTime;
        if (retargetTimer >= retargetInterval)
        {
            retargetTimer = 0f;
            SetCurrentTarget(ChooseNewTarget());
        }

        // A) Decide state by distance (+ hysteresis)
        UpdateStateByDistance();

        // If we somehow lost our slot, force out of Attacking
        if (currentState == EnemyState.Attacking && !_hasAttackSlot)
        {
            SetState(EnemyState.Chasing);
        }

        // B) State behavior
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
        if (currentTargetTransform == null)
        {
            SetState(EnemyState.Chasing);
            return;
        }

        float dist = Vector3.Distance(transform.position, currentTargetTransform.position);
        float enterAttack = pathFinder.CombatRange;
        float exitAttack = pathFinder.CombatRange + combatRangeHysteresis;

        if (currentState != EnemyState.Attacking)
        {
            if (dist <= enterAttack)
            {
                if (TryClaimAttackSlot())
                    SetState(EnemyState.Attacking);
                else
                    SetState(EnemyState.Chasing);
            }
            else
            {
                SetState(EnemyState.Chasing);
            }
        }
        else
        {
            if (dist >= exitAttack)
                SetState(EnemyState.Chasing);
        }
    }

    private void SetState(EnemyState newState)
    {
        if (currentState == newState) return;

        // Exit old state
        switch (currentState)
        {
            case EnemyState.Attacking:
                ReleaseAttackSlot();
                break;
        }

        currentState = newState;

        // Enter new state
        switch (currentState)
        {
            case EnemyState.Idle:
                ReleaseAttackSlot();
                break;

            case EnemyState.Chasing:
                pathFinder.FindPath();
                _nextAllowedRepathTime = 0f;
                break;

            case EnemyState.Attacking:
                movement.HorizontalMovement(0f, 0f);
                break;
        }
    }

    private bool TryClaimAttackSlot()
    {
        if (_hasAttackSlot) return true;

        if (GameManager.Instance == null)
        {
            _hasAttackSlot = true;
            return true;
        }

        _hasAttackSlot = GameManager.Instance.TryClaimAttackSlot(this);
        return _hasAttackSlot;
    }

    private void ReleaseAttackSlot()
    {
        if (!_hasAttackSlot) return;
        _hasAttackSlot = false;

        if (GameManager.Instance != null)
            GameManager.Instance.ReleaseAttackSlot(this);
    }

    private void DoChasing()
    {
        TryRepathIfPathFinished();

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
        TryRepathIfPathFinished();

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

        if (!_hasAttackSlot) return;

        if (firearmSystem != null && currentState == EnemyState.Attacking)
        {
            firearmSystem.RequestFireAll(fireConeDeg);
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

    public void SetOnShiped(bool onShip)
    {
        onShiped = onShip;
    }

    /// <summary>
    /// Called every frame. For targets outside retargetRange, gradually reduce
    /// damageCauseByTarget and sync damageAggro. baseAggro is never affected. isMainTarget entries are exempt.
    /// </summary>
    private void DecayOutOfRangeTargets()
    {
        if (targetList == null || targetList.Count == 0) return;

        float rangeSqr = retargetRange > 0f ? retargetRange * retargetRange : float.PositiveInfinity;

        foreach (TargetPriority target in targetList)
        {
            if (target == null || target.target == null) continue;

            // Main targets are immune to decay
            if (target.isMainTarget) continue;

            float distSqr = (target.target.position - transform.position).sqrMagnitude;
            if (distSqr <= rangeSqr) continue;

            // Decay raw damage value
            target.damageCauseByTarget = Mathf.Max(0f, target.damageCauseByTarget - damageFalloffRate * Time.deltaTime);

            // Sync stepped aggro to match decayed damage — baseAggro untouched
            target.damageAggro = Mathf.FloorToInt(target.damageCauseByTarget / damagePerAggroStep);
        }
    }

    public Transform ChooseNewTarget()
    {
        if (targetList == null || targetList.Count == 0)
            return null;

        float rangeSqr = retargetRange > 0f ? retargetRange * retargetRange : float.PositiveInfinity;

        int totalAggro = 0;

        foreach (TargetPriority target in targetList)
        {
            if (target == null || target.target == null)
                continue;

            if (!target.isMainTarget)
            {
                float distSqr = (target.target.position - transform.position).sqrMagnitude;
                if (distSqr > rangeSqr)
                    continue;
            }

            totalAggro += Mathf.Max(0, target.TotalAggro);
        }

        if (totalAggro <= 0)
            return null;

        int roll = Random.Range(0, totalAggro);
        int current = 0;

        foreach (TargetPriority target in targetList)
        {
            if (target == null || target.target == null)
                continue;

            if (!target.isMainTarget)
            {
                float distSqr = (target.target.position - transform.position).sqrMagnitude;
                if (distSqr > rangeSqr)
                    continue;
            }

            current += Mathf.Max(0, target.TotalAggro);

            if (roll < current)
                return target.target;
        }

        return null;
    }

    private void SetCurrentTarget(Transform newTarget)
    {
        if (newTarget == null) return;

        currentTargetTransform = newTarget;

        if (firearmSystem != null)
            firearmSystem.defaultTarget = currentTargetTransform;

        if (pathFinder != null)
        {
            pathFinder.FindPath();
            _nextAllowedRepathTime = 0f;
        }
    }

    public void ForceSetTarget(Transform newTarget)
    {
        if (newTarget == null) return;

        var existing = targetList.Find(t => t != null && t.target == newTarget);
        if (existing == null)
        {
            targetList.Add(new TargetPriority
            {
                target = newTarget,
                baseAggro = 1,
                isMainTarget = false,
                damageCauseByTarget = 0f,
                damageAggro = 0
            });
        }

        currentTargetTransform = newTarget;

        if (firearmSystem != null)
            firearmSystem.defaultTarget = currentTargetTransform;

        if (pathFinder != null)
        {
            pathFinder.FindPath();
            _nextAllowedRepathTime = 0f;
        }
    }
}