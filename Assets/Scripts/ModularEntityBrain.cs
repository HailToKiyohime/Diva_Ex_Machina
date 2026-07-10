using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

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

    private PathFinder pathFinder;

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
        currentState = EntityState.Idle;
        RebuildPreferenceCache();
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
                SetPath(pathFinder.FindPath(destination));
                break;
            case EntityState.Patrolling:
                destinationTimer = RollInterval(patrolDestinationUpdateRange);
                r = Random.insideUnitCircle * patrolActivityAreaRadius;
                destination = spawnLocation + new Vector3(r.x, 0f, r.y);
                SetPath(pathFinder.FindPath(destination));
                break;
            case EntityState.Retreat:
                destinationTimer = RollInterval(retreatDestinationUpdateRange);
                destination = spawnLocation;
                SetPath(pathFinder.FindPath(destination));
                break;
            case EntityState.Chasing:
                destinationTimer = RollInterval(chaseDestinationUpdateRange);
                destination = FindTarget().position;
                SetPath(pathFinder.FindPath(destination));
                break;
            case EntityState.Combat:
                destinationTimer = RollInterval(combatDestinationUpdateRange);
                r = Random.insideUnitCircle * combatActivityAreaRadius;
                destination = spawnLocation + new Vector3(r.x, 0f, r.y);
                SetPath(pathFinder.FindPath(destination));
                break;
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
        if (path == null || path.Length == 0)
            return transform.position;

        // path 可能換過、變短，夾住避免越界
        if (currentWaypointIndex > path.Length - 1)
            currentWaypointIndex = path.Length - 1;

        // 已經抵達的 corner 就往前推進；到最後一個就停在那，不再 +1（自然不會越界）
        while (currentWaypointIndex < path.Length - 1 &&
               Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(path[currentWaypointIndex].x, path[currentWaypointIndex].z)) < waypointArriveRadius)
        {
            currentWaypointIndex++;
        }

        return path[currentWaypointIndex];
    }

    private float RollInterval(Vector2 range){ 
        return Random.Range(range.x, range.y); 
    }
    protected void IdleBehaviour()
    {
        if (path == null || path.Length == 0) return; 
        Vector3 nextWaypoint = FindNextWaypoint();
        Vector3 moveDirection = nextWaypoint - transform.position;
        moveDirection.y = 0f;
        float dist = moveDirection.magnitude;

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
        if (currentWaypointIndex == path.Length - 1 && Vector3.Distance(transform.position, nextWaypoint) < waypointArriveRadius)
        {
            destinationTimer = 0f;
        }
        else
        {
            Vector3 moveDirection = nextWaypoint - transform.position;
            modularEntityMovement.HorizontalMovement(moveDirection.x, moveDirection.z);
        }
    }

    protected void RetreatBehaviour()
    {
        if (path == null || path.Length == 0) return;
        Vector3 nextWaypoint = FindNextWaypoint();
        if (currentWaypointIndex == path.Length - 1 && Vector3.Distance(transform.position, nextWaypoint) < waypointArriveRadius)
        {
            ChangeState(EntityState.Patrolling);
        }
        else
        {
            Vector3 moveDirection = nextWaypoint - transform.position;
            modularEntityMovement.HorizontalMovement(moveDirection.x, moveDirection.z);
        }
    }

    protected void ChasingBehaviour()
    {
        if (path == null || path.Length == 0) return;
        Vector3 nextWaypoint = FindNextWaypoint();
        if (Vector3.Distance(transform.position, FindTarget().position) < combatActivityAreaRadius/2)
        {
            ChangeState(EntityState.Combat);
        }
        else
        {
            Vector3 moveDirection = nextWaypoint - transform.position;
            modularEntityMovement.HorizontalMovement(moveDirection.x, moveDirection.z);
        }
    }

    protected void CombatBehaviour()
    {
        if (path == null || path.Length == 0) return;
        Vector3 nextWaypoint = FindNextWaypoint();
        if (Vector3.Distance(transform.position, nextWaypoint) < combatActivityAreaRadius / 2)
        {
            destinationTimer = 0f;
        }
        else
        {
            Vector3 moveDirection = nextWaypoint - transform.position;
            modularEntityMovement.HorizontalMovement(moveDirection.x, moveDirection.z);
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
}