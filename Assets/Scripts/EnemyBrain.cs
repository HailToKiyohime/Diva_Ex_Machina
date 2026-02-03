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
    [Tooltip("Hysteresis buffer to prevent state flipping near combat range (0.5~2 is common).")]
    [SerializeField] private float combatRangeHysteresis = 1.0f;

    private CreatePath pathFinder;
    private EnemyMovement movement;

    private Vector3 nextMoveLocation;
    private float _nextAllowedRepathTime;

    private FirearmControlSystem firearmSystem;
    [SerializeField] private float fireConeDeg = 5f;

    // ============================
    // Attack Limiter
    // Only enemies holding a slot are allowed to shoot.
    // ============================
    private bool _hasAttackSlot;

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

        if (pathFinder.Target == null)
        {
            ReleaseAttackSlot();
            SetState(EnemyState.Idle);
            movement.HorizontalMovement(0f, 0f);
            return;
        }

        if (firearmSystem != null)
        {
            firearmSystem.target = pathFinder.Target;
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
        float dist = Vector3.Distance(transform.position, pathFinder.Target.position);
        float enterAttack = pathFinder.CombatRange;
        float exitAttack = pathFinder.CombatRange + combatRangeHysteresis;

        if (currentState != EnemyState.Attacking)
        {
            if (dist <= enterAttack)
            {
                // IMPORTANT: only enter Attacking if we can claim a slot
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
            // Already Attacking: only leave when beyond exit threshold
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
            case EnemyState.Attacking:
                // Leaving Attacking => release slot
                ReleaseAttackSlot();
                break;
        }

        currentState = newState;

        // Enter new
        switch (currentState)
        {
            case EnemyState.Idle:
                ReleaseAttackSlot();
                break;

            case EnemyState.Chasing:
                // Enter chasing: refresh path
                pathFinder.FindPath();
                _nextAllowedRepathTime = 0f;
                break;

            case EnemyState.Attacking:
                // Enter attacking: stop input this frame; actual movement/fire handled in DoAttacking
                movement.HorizontalMovement(0f, 0f);
                break;
        }
    }

    private bool TryClaimAttackSlot()
    {
        if (_hasAttackSlot) return true;

        // If there's no GameManager, just allow (fail-open)
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

        // Only shoot if we currently hold an attack slot
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
}